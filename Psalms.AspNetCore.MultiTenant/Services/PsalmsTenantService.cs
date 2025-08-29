using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Models;
using System.Linq.Expressions;

namespace Psalms.AspNetCore.MultiTenant.Services;

public class PsalmsTenantService<Tenant>(IPsalmsTenantDbContext<Tenant> tenantContext, IConfiguration config, MultiTenantConfigureDbContext Appcontext)
    where Tenant : class, ITenantModelBase 
{   
    #region IPsalmsTenantService Methods
    public async Task<Tenant?> GetTenantByAsync(Expression<Func<Tenant, bool>> predicate)
        => await tenantContext.Tenants.FirstOrDefaultAsync(predicate);

    public async Task CreateTenantAsync(Tenant tenant)
    {
        await tenantContext.Tenants.AddAsync(tenant);
        await tenantContext.ApplyChangesAsync();

        await SetConnectionStringAsync(tenant);
       
        await Appcontext.Database.MigrateAsync();
    }
    public async Task DeleteTenantByAsync(Expression<Func<Tenant, bool>> predicate)
    {
        var tenant = await GetTenantByAsync(predicate)
            ?? throw new Exception("Unable to find tenant to delete");

        tenantContext.Tenants.Remove(tenant);
        await tenantContext.ApplyChangesAsync();

        await SetConnectionStringAsync(tenant);

        await Appcontext.Database.EnsureDeletedAsync();
    }
    private Task SetConnectionStringAsync(Tenant tenant)
    {
        var connectionString = PsalmsDatabase.GetDbConnectionString(config, tenant.DatabaseName);

        Appcontext.Database.SetConnectionString(connectionString);

        return Task.CompletedTask;
    }
    #endregion
}