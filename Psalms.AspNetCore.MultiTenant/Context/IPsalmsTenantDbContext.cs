using Microsoft.EntityFrameworkCore;
using Psalms.AspNetCore.MultiTenant.Models;

namespace Psalms.AspNetCore.MultiTenant.Context;

public interface IPsalmsTenantDbContext<TTenant> where TTenant : class, ITenantModelBase
{
    public DbSet<TTenant> Tenants { get; set; }

    Task ApplyChangesAsync();
}