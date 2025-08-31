# Psalms.AspNetCore.MultiTenant

A lightweight library that adds **database-per-tenant multi-tenancy** to ASP.NET Core applications with Entity Framework Core.

It handles three main concerns:

1. **Tenant resolution** – finds which tenant is making the request (based on claims).
2. **Dynamic database switching** – your `DbContext` uses the right database per request.
3. **Tenant lifecycle management** – create, migrate, and delete tenant databases easily.

---

## How it works (high-level)

```
[HTTP Request] → [Authentication] → [PsalmsTenantMiddleware]
                                    ↓
                              HttpContext.Items
                                    ↓
                   MultiTenantConfigureDbContext (your AppDbContext)
                                    ↓
                       Database of the current tenant

```

- **Tenant model**: defines Id, Name, Subdomain, DatabaseName.
- **Tenant catalog context**: keeps the registry of tenants in a shared database.
- **Application context**: your actual app `DbContext`, derived from `MultiTenantConfigureDbContext`, that switches its connection string at runtime.
- **Middleware**: extracts tenant info from the authenticated user and places it in `HttpContext.Items`.
- **Service**: creates/removes tenants and manages their databases via migrations.

---

## Core components

### `ITenantModelBase`

Defines the contract every tenant model must follow:

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

A ready-to-use default tenant implementation.

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

Contract for the **tenant catalog context**:

```csharp
public interface IPsalmsTenantDbContext<TTenant> where TTenant : class, ITenantModelBase
{
    DbSet<TTenant> Tenants { get; set; }
    Task ApplyChangesAsync();
}

```

### `MultiTenantConfigureDbContext`

Base `DbContext` that automatically switches connection strings per request.

```csharp
public class MultiTenantConfigureDbContext : DbContext
{
    public MultiTenantConfigureDbContext(DbContextOptions options, IHttpContextAccessor accessor, IConfiguration config)
        : base(options)
    {
        var dbName = accessor.HttpContext.Items[TenantInfo.DatabaseName];
        if (dbName != null)
            Database.SetConnectionString(PsalmsDatabase.GetDbConnectionStringBase(config, dbName.ToString()!));
    }
}

```

Your **AppDbContext** should inherit from this.

### `PsalmsTenantMiddleware<TTenant>`

Middleware that:

1. Reads the `TenantId` claim from the user.
2. Resolves the tenant from the catalog (`IPsalmsTenantDbContext`).
3. Places `DatabaseName` in `HttpContext.Items` for the DbContext to pick up.

### `PsalmsTenantService<TTenant>`

Handles tenant management:

- `GetTenantByAsync` → retrieve tenants by condition.
- `CreateTenantAsync` → saves tenant, sets connection string, runs migrations.
- `DeleteTenantByAsync` → removes tenant, deletes its database.

### `PsalmsDatabase`

Helper that builds the tenant-specific connection string:

```csharp
public static string GetDbConnectionStringBase(IConfiguration config, string databaseName)
    => string.Format(config["DbConnectionBase"] ?? throw new Exception("Missing base connection"), databaseName);

```

### `PsalmsTenantExtension.AddPsalmsMultiTenant`

Extension method to register everything in DI:

```csharp
builder.Services.AddPsalmsMultiTenant<TenantModel, TenantContext, AppDbContext>(
    contextConfiguration, builder.Configuration);

```

---

## Setup guide

### 1) Define a tenant model

Use the provided `PsalmsTenantModel` or create your own:

```csharp
public class Tenant : PsalmsTenantModel {}

```

### 2) Create the tenant catalog context

```csharp
public class TenantCatalog : DbContext, IPsalmsTenantDbContext<Tenant>
{
    public TenantCatalog(DbContextOptions<TenantCatalog> options) : base(options) {}

    public DbSet<Tenant> Tenants { get; set; } = null!;

    public Task ApplyChangesAsync() => SaveChangesAsync();
}

```

### 3) Create your application DbContext

Inherit from `MultiTenantConfigureDbContext`:

```csharp
public class AppDbContext : MultiTenantConfigureDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor accessor, IConfiguration config)
        : base(options, accessor, config) { }

    public DbSet<Product> Products => Set<Product>();
}

```

### 4) Implement context configuration

You must provide a configuration class that implements `IPsalmsContextConfiguration`:

```csharp
public class DbContextConfig : IPsalmsContextConfiguration
{
    public Action<DbContextOptionsBuilder> TenantContextConfig(IConfiguration config)
        => options => options.UseSqlite
        (
	        PsalmsDatabase.GetDbConnectionString(config, "dbName"),
	        x => MigrationsAssembly("YourAssembly")
        );

    public Action<DbContextOptionsBuilder> AppContextConfig()
        => options => options.UseSqlite(x => MigrationsAssembly("YourAssembly"));
}

```

### 5) Register services in Program.cs

```csharp
builder.Services.AddPsalmsMultiTenant<Tenant, TenantCatalog, AppDbContext>(
    new DbContextConfig(), builder.Configuration);

```

### 6) Add middleware to the pipeline

```csharp
app.UseAuthentication();
app.UseMiddleware<PsalmsTenantMiddleware<Tenant>>();
app.UseAuthorization();

```

Make sure the middleware runs **before** you resolve `AppDbContext`.

### 7) Configure base **connection string** in `appsettings.json`

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

### 8) Authentication and **Tenant Claim**

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

---

## Example controller

```csharp
[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly PsalmsTenantService<Tenant> _service;

    public TenantsController(PsalmsTenantService<Tenant> service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantDto dto)
    {
        var tenant = new Tenant
        {
            Name = dto.Name,
            Subdomain = dto.Subdomain,
            DatabaseName = dto.DatabaseName
        };

        await _service.CreateTenantAsync(tenant);
        return Ok(tenant);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteTenantByAsync(x => x.Id == id);
        return NoContent();
    }
}

```

---

## Key notes & best practices

- **DbContext pooling**: avoid `AddDbContextPool` for the app context; it may leak tenant connections.
- **Tenant resolution**: requires a `TenantId` claim in the authenticated user.
- **Migrations**: keep migrations in your app project; they’ll run automatically per tenant.
- **Scaling**: designed for small to medium number of tenants; thousands of databases may require different approaches.
- **Seeding**: after `CreateTenantAsync`, you can seed tenant-specific data.

---

## TL;DR

This library:

- Resolves tenant from claims.
- Switches EF Core database connection per request.
- Provides a service to create/delete tenant databases.

> In short: one API, many clients, each with its own isolated database – no extra boilerplate.
> 

## FAQ

**Q. Where do tenant connection strings live?**

The helper `PsalmsDatabase.GetDbConnectionString` builds connections from config. You can use a fixed key (e.g., `"Tenants"`) for the catalog and a template for tenant DBs (e.g., `./data/{0}.db`).

**Q. What happens if there’s no TenantId claim?**

Request continues, but AppDbContext won’t have a defined connection. Handle with `401/403` where appropriate.

**Q. How do I add TenantId to claims?**

During login/refresh, include the tenant’s integer ID in the token claim. BTW, we also provide a library for JWT tokens 👀 https://github.com/PsalmsLibrary/Psalms.AspNetCore.Auth.Jwt
