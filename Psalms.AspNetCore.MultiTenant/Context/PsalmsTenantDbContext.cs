using Microsoft.EntityFrameworkCore;
using Psalms.AspNetCore.MultiTenant.Models;

namespace Psalms.AspNetCore.MultiTenant.Context;

public class PsalmsTenantDbContext(DbContextOptions<PsalmsTenantDbContext> options) : DbContext(options)
{
    public DbSet<TenantModel> Tenants { get; set; }
}