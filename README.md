# Psalms.AspNetCore.MultiTenant

A simple abstraction for **database-per-tenant multi-tenancy** (one database per client) in ASP.NET Core applications with EF Core.

The library solves three central points:

1. **Tenant discovery** through a `Claim` on the authenticated user.
2. **Dynamic switching** of your application `DbContext` connection string based on the current tenant.
3. **Tenant database lifecycle** (creation with `Migrate`, removal with `EnsureDeleted`) through a dedicated service.

> TL;DR: A middleware reads TenantId from claims, stores DatabaseName in cache, and your DbContext (which inherits from MultiTenantConfigureDbContext) switches the connection to the tenant’s database automatically.
> 

---

## Architecture overview

```
[Request] → [Auth] → [PsalmsTenantMiddleware]
                         ↓
                    IMemoryCache
                         ↓
           MultiTenantConfigureDbContext (AppDbContext)
                         ↓
                  Tenant Database (dynamic)

```

- **Tenant model**: minimal (`Id`, `Name`, `Subdomain`, `DatabaseName`).
- **Tenants context**: a `DbContext` just for the tenants table (the “catalog”).
- **Application context**: your real `DbContext`, derived from `MultiTenantConfigureDbContext`, whose `ConnectionString` is defined at runtime based on the current tenant’s `DatabaseName`.
- **Tenant service**: creates, fetches, and deletes tenants, running `Migrate`/`EnsureDeleted` on the application `DbContext`.
- **Middleware**: resolves tenant from claims and fills the cache for the `DbContext` to use.

---

## Components (what each class does)

### `ITenantModelBase`

Minimal tenant contract.

```csharp
public interface ITenantModelBase
{
    int Id { get; set; }
    string Name { get; set; }
    string Subdomain { get; set; }
    string DatabaseName { get; set; }
}

```

### `PsalmsTenantModel`

Basic ready-to-use implementation of the contract above.

```csharp
public class PsalmsTenantModel : ITenantModelBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

```

### `IPsalmsTenantDbContext<TTenant>`

Contract your **tenant catalog context** must implement.

```csharp
public interface IPsalmsTenantDbContext<TTenant> where TTenant : class, ITenantModelBase
{
    DbSet<TTenant> Tenants { get; set; }
    Task ApplyChangesAsync(); // normally: await SaveChangesAsync();
}

```

### `MultiTenantConfigureDbContext`

Base `DbContext` that reads the current tenant’s `DatabaseName` from cache and adjusts the `ConnectionString` dynamically.

Your `AppDbContext` **must inherit** this class.

```csharp
public class MultiTenantConfigureDbContext : DbContext
{
    public MultiTenantConfigureDbContext(
        DbContextOptions options,
        IMemoryCache cache,
        IConfiguration config) : base(options)
    {
        var dbName = cache.Get<string>(TenantInfo.DatabaseName)!;
        var conn   = PsalmsDatabase.GetDbConnectionString(config, dbName);
        Database.SetConnectionString(conn);
    }
}

```

> Important: this context depends on PsalmsTenantMiddleware having populated the cache within the same request before the context is instantiated/first used.
> 

### `PsalmsTenantMiddleware<TTenant>`

Middleware that:

1. Reads **`TenantId`** from the authenticated user’s claims.
2. Loads the tenant into the **catalog context** (`IPsalmsTenantDbContext<TTenant>`).
3. Stores the tenant and its `DatabaseName` in `IMemoryCache`.

If no tenant claim is present, the request continues, but the application `DbContext` won’t be able to define its connection (see *Pipeline order*).

### `PsalmsTenantService<TTenant>`

Utility service for the tenant database lifecycle:

- `GetTenantByAsync(...)`: query by expression.
- `CreateTenantAsync(tenant)`: saves to catalog, sets `AppDbContext` connection, and executes `Database.MigrateAsync()`.
- `DeleteTenantByAsync(predicate)`: removes from catalog, sets connection, and executes `Database.EnsureDeletedAsync()`.

> On creation, your AppDbContext migrations run on the tenant’s database; on deletion, the tenant database is dropped.
> 

### `PsalmsTenantExtension.AddPsalmsMultiTenant(...)`

Extension method to register **everything** in DI: the two contexts (catalog and application), `PsalmsTenantService`, and `IMemoryCache`.

---

## Requirements

- ASP.NET Core + EF Core.
- Compatible DB provider (e.g., SQLite, SQL Server). Examples below use **SQLite**.

---

## Step by step (Quick setup)

### 1) Define the **tenant model** (optional)

You can use `PsalmsTenantModel` directly or create your own model implementing `ITenantModelBase`.

```csharp
public class TenantModel : PsalmsTenantModel {}

```

### 2) Create the **Tenants catalog context**

Implement `IPsalmsTenantDbContext<TenantModel>`.

```csharp
public class TenantContext : DbContext, IPsalmsTenantDbContext<TenantModel>
{
    public TenantContext(DbContextOptions<TenantContext> options) : base(options) {}

    public DbSet<TenantModel> Tenants { get; set; } = null!;

    public Task ApplyChangesAsync() => SaveChangesAsync();
}

```

### 3) Create your **application DbContext**, inheriting from `MultiTenantConfigureDbContext`

```csharp
public class AppDbContext : MultiTenantConfigureDbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IMemoryCache cache,
        IConfiguration config) : base(options, cache, config) { }

    // Your entities
    public DbSet<Product> Products => Set<Product>();
}

```

> Migrations: Point MigrationsAssembly to your API project (or wherever migrations live), as that’s what will be applied to each tenant.
> 

### 4) Register services in `Program.cs`

```csharp
builder.Services.AddPsalmsMultiTenant<TenantModel, TenantContext, AppDbContext>(
    tenantOptions: x => x.UseSqlite(
        PsalmsDatabase.GetDbConnectionString(builder.Configuration, "Tenants"),
        x => x.MigrationsAssembly("YourProject")
    ),
    appContextOptions: x => x.UseSqlite(
        x => x.MigrationsAssembly("YourProject")
    )
);

```

### 5) Configure the **pipeline** (order matters!)

```csharp
app.UseAuthentication();
app.UseMiddleware<PsalmsTenantMiddleware<TenantModel>>(); // before Authorization and before using AppDbContext
app.UseAuthorization();

```

> Ensure any code using AppDbContext runs after the tenant middleware, so DatabaseName is in cache when the context is instantiated/first used.
> 

### 6) Configure base **connection string** in `appsettings.json`

Example with SQLite:

```json
{
  "DbConnectionBase": "DataSource={0}.db"

  // PsalmsDatabase.GetDbConnectionString(config, "MyDb")
  // result -> "DataSource=MyDb.db"
}

```

> The helper PsalmsDatabase.GetDbConnectionString(config, databaseNameOrKey) will look up your config to build the connection string (e.g., using a {db} template). Adjust to your implementation. This helps avoid exposing sensitive info (passwords, DB location). You only need the DB name. 😊
> 

### 7) Authentication and **Tenant Claim**

Middleware looks for the claim `TenantId` (name from `TenantInfo.TenantId.ToString()`). Ensure you **include this claim** in your user’s token (e.g., at login):

```csharp
var claims = new List<Claim>
{
    new Claim(TenantInfo.TenantId.ToString(), tenantId.ToString()),
    // ... other claims
};

```

> Without this claim, the request continues, but AppDbContext cannot set its connection. If an endpoint uses the context without tenant resolved, you’ll get a connection error.
> 

---

## Using `PsalmsTenantService`

Create/fetch/delete tenants **and** manage tenant databases automatically.

```csharp
[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly PsalmsTenantService<TenantModel> _service;

    public TenantsController(PsalmsTenantService<TenantModel> service)
        => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantDto dto)
    {
        var tenant = new TenantModel
        {
            Name = dto.Name,
            Subdomain = dto.Subdomain,
            DatabaseName = dto.DatabaseName
        };

        await _service.CreateTenantAsync(tenant);
        return CreatedAtAction(nameof(Get), new { id = tenant.Id }, tenant);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _service.GetTenantByAsync(x => x.Id == id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteTenantByAsync(x => x.Id == id);
        return NoContent();
    }
}

```

- **CreateTenantAsync**: adds tenant to catalog → sets AppDbContext connection to tenant DB → runs `MigrateAsync()`.
- **DeleteTenantByAsync**: removes tenant from catalog → sets connection → runs `EnsureDeletedAsync()` (drops tenant DB).

> Tip: you can call CreateTenantAsync during customer onboarding to guarantee their DB starts migrated.
> 

---

## Best practices, tips, and notes

- **Pipeline order**: tenant middleware must run **before** any AppDbContext usage.
- **Concurrency**: strategy uses `IMemoryCache` to share `DatabaseName` within request. For heavy multi-tenant concurrency, consider `IHttpContextAccessor + HttpContext.Items`, `AsyncLocal<T>`, or another request-scoped storage.
- **Migrations**: keep AppDbContext migrations in the correct assembly and configure `MigrationsAssembly(...)`.
- **Providers**: swap `UseSqlite(...)` for `UseSqlServer(...)` (or others) as needed.
- **Validation**: handle missing/blocked `TenantId` with `403`/`404` in middleware or global filters.
- **Per-tenant seeding**: after `CreateTenantAsync`, run tenant-specific seed data if needed.

---

## Full example (minimal)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddPsalmsMultiTenant<TenantModel, TenantContext, AppDbContext>(
    x => x.UseSqlite(
        PsalmsDatabase.GetDbConnectionString(builder.Configuration, "Tenants"),
        x => x.MigrationsAssembly("MyApi")
    ),
    x => x.UseSqlite(
        x => x.MigrationsAssembly("MyApi")
    )
);

var app = builder.Build();

app.UseAuthentication();
app.UseMiddleware<PsalmsTenantMiddleware<TenantModel>>();
app.UseAuthorization();

app.MapControllers();
app.Run();

```

---

## FAQ

**Q. Can I have multiple tenants in the same request?**

No, that’s not the library’s goal. Middleware expects **one** TenantId claim.

**Q. Where do tenant connection strings live?**

The helper `PsalmsDatabase.GetDbConnectionString` builds connections from config. You can use a fixed key (e.g., `"Tenants"`) for the catalog and a template for tenant DBs (e.g., `./data/{0}.db`).

**Q. What happens if there’s no TenantId claim?**

Request continues, but AppDbContext won’t have a defined connection. Handle with `401/403` where appropriate.

**Q. How do I add TenantId to claims?**

During login/refresh, include the tenant’s integer ID in the token claim. BTW, we also provide a library for JWT tokens 👀 https://github.com/PsalmsLibrary/Psalms.AspNetCore.Auth.Jwt
