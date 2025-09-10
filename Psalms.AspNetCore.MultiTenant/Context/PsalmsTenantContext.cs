using Microsoft.EntityFrameworkCore;
using Psalms.AspNetCore.MultiTenant.Models;

namespace Psalms.AspNetCore.MultiTenant.Context;

public class PsalmsTenantContext(DbContextOptions<PsalmsTenantContext> options) : DbContext(options), IPsalmsTenantDbContext<PsalmsTenantModel>
{
    public DbSet<PsalmsTenantModel> Tenants { get; set; }

    public Task ApplyChangesAsync() => SaveChangesAsync();
}
