using Microsoft.EntityFrameworkCore;
using Psalms.AspNetCore.MultiTenant.Models;

namespace Psalms.AspNetCore.MultiTenant.Context;

/// <summary>
/// A default Tenant Context to a easily configuration.
/// </summary>
/// <param name="options"></param>
public class PsalmsTenantContext(DbContextOptions<PsalmsTenantContext> options) : DbContext(options), IPsalmsTenantDbContext<PsalmsTenantModel>
{
    public DbSet<PsalmsTenantModel> Tenants { get; set; }

    public Task ApplyChangesAsync() => SaveChangesAsync();
}
