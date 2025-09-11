# 

# Psalms.AspNetCore.MultiTenant

A lightweight library to simplify **multi-tenant** implementation in ASP.NET Core applications using Entity Framework Core.

It provides dynamic database connection handling, automatic tenant migrations, and centralized configuration for tenant-aware applications.

---

## 🚀 Purpose

The goal of this library is to make **database-isolated multi-tenancy** simple to implement by providing:

- Centralized configuration of `DbContext`s.
- Middleware to resolve tenants from user claims.
- Services to create and delete tenants with automatic migrations.
- Support for custom tenant models implementing `ITenantModelBase`.

---

## 📦 Installation

Add the package to your project:

```bash
dotnet add package Psalms.AspNetCore.MultiTenant
```

---

## ⚙️ Usage

### 1. Create a Tenant Model

Implement the `ITenantModelBase` interface or use the built-in `PsalmsTenantModel`:

```csharp
public class Tenant : ITenantModelBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
```

### 2. Configure the Tenant Context

Use the default `PsalmsTenantContext` or create your own by implementing `IPsalmsTenantDbContext<Tenant>`:

```csharp
public class TenantContext : DbContext, PsalmsTenantContext<Tenant>
{
    public TenantContext(DbContextOptions<TenantContext> options) : base(options) { }
    
    public DbSet<Tenant> Tenants {get; set;}
    
	    public Task ApplyChangesAsync() => SaveChangesAsync();
}
```

### 3. Implement Context Configuration

Implement the `IPsalmsContextConfiguration` interface to define how both the **tenant** and **application** contexts are configured:

```csharp
public class ContextConfiguration : IPsalmsContextConfiguration
{
    public Action<DbContextOptionsBuilder> TenantContextConfig(IConfiguration config) =>
        options => options.UseSqlServer(PsalmsDatabase.GetConnectionDb(config, "Tenants"));

    public Action<DbContextOptionsBuilder> AppContextConfig() =>
        options => options.UseSqlServer();
}
// Use your migrations assembly

// We will soon make provider settings available, so it will no longer be necessary to configure this part from scratch.
```

### 4. Register in `Program.cs`

```csharp
builder.Services.AddPsalmsMultiTenant<Tenant, TenantContext, AppDbContext>(
    new ContextConfiguration(),
    builder.Configuration
);
```

### 5. Add the Middleware

```csharp
app.UseMiddleware<PsalmsTenantMiddleware<Tenant, AppDbContext>>();
```

---

## 🏗️ Key Components

### 🔹 `ITenantModelBase`

Defines the required properties for any tenant model:

- `Id`
- `Name`
- `Subdomain`
- `DatabaseName`

### 🔹 `PsalmsTenantModel`

Default implementation of `ITenantModelBase`.

### 🔹 `IPsalmsTenantDbContext<TTenant>`

Abstraction for tenant-aware database contexts.

### 🔹 `PsalmsTenantContext`

Default implementation of `IPsalmsTenantDbContext` with `DbSet<PsalmsTenantModel>`.

### 🔹 `IPsalmsContextConfiguration`

Defines how to configure both tenant and application contexts.

### 🔹 `PsalmsTenantService<TTenant, AppDbContext>`

Service responsible for:

- Fetching tenants (`GetTenantByAsync`)
- Creating tenants (`CreateTenantAsync`)
- Deleting tenants (`DeleteTenantByAsync`)
- Dynamically setting connection strings

### 🔹 `PsalmsTenantMiddleware<TenantModel, AppContext>`

Middleware that:

- Resolves the tenant from user claims
- Sets the tenant’s connection string dynamically
- Returns **403 Forbidden** if the tenant is not found

### 🔹 `PsalmsDatabase`

Helper class to build connection strings based on a `DbConnectionBase` entry in `appsettings.json`.

---

## 📖 Example `appsettings.json`

```json
{
  "DbConnectionBase": "Server=localhost;Database={0};User Id=sa;Password=Your_password123;TrustServerCertificate=True"
}

```

# 📂 Example: `TenantsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Psalms.AspNetCore.MultiTenant.Services;
using System.Threading.Tasks;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly PsalmsTenantService<Tenant, AppDbContext> _tenantService;

        public TenantsController(PsalmsTenantService<Tenant, AppDbContext> tenantService)
        {
            _tenantService = tenantService;
        }

        /// <summary>
        /// List all tenants (basic example, use paging in real scenarios).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // In a real app, you'd query the TenantContext directly
            var tenants = await _tenantService
                .GetTenantByAsync(t => true); // just an example, better use IQueryable
            return Ok(tenants);
        }

        /// <summary>
        /// Get a specific tenant by id.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tenant = await _tenantService.GetTenantByAsync(t => t.Id == id);

            if (tenant == null)
                return NotFound(new { message = $"Tenant with id {id} not found." });

            return Ok(tenant);
        }

        /// <summary>
        /// Create a new tenant (runs migrations automatically).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Tenant tenant)
        {
            await _tenantService.CreateTenantAsync(tenant);
            return Ok(new { message = $"Tenant '{tenant.Name}' created successfully!" });
        }

        /// <summary>
        /// Delete a tenant (removes record and drops its database).
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _tenantService.DeleteTenantByAsync(t => t.Id == id);
            return Ok(new { message = $"Tenant with id {id} deleted successfully!" });
        }
    }
}

```

---

# 📖 How this works

### 1. **Create Tenant**

- Saves the tenant in the **TenantContext**.
- Configures connection string dynamically.
- Runs `Database.MigrateAsync()` to create schema for the tenant.

### 2. **Get Tenant**

- Uses `GetTenantByAsync` to query a tenant by expression (`Id`, `Name`, `Subdomain`, etc.).

### 3. **Delete Tenant**

- Removes the tenant from **TenantContext**.
- Drops the corresponding database using `EnsureDeletedAsync()`.

---

⚠️ Note:

For **listing tenants**, you might want to inject `IPsalmsTenantDbContext<Tenant>` directly, because `PsalmsTenantService` is focused on **single tenant operations** (`Get`, `Creat`
