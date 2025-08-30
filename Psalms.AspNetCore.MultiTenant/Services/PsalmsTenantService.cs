using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Models;
using System.Linq.Expressions;

namespace Psalms.AspNetCore.MultiTenant.Services;

/// <summary>
/// Provides tenant management operations for multi-tenant applications,
/// including retrieval, creation, and deletion of tenants, as well as dynamic
/// database connection string configuration and migration handling.
/// </summary>
/// <typeparam name="Tenant">The tenant entity type, which must implement <see cref="ITenantModelBase"/>.</typeparam>
public class PsalmsTenantService<Tenant>(IPsalmsTenantDbContext<Tenant> tenantContext, IConfiguration config, MultiTenantConfigureDbContext Appcontext)
    where Tenant : class, ITenantModelBase
{
    #region IPsalmsTenantService Methods

    /// <summary>
    /// Retrieves a tenant entity that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">Expression used to filter the desired tenant.</param>
    /// <returns>The tenant entity matching the filter, or null if not found.</returns>
    public async Task<Tenant?> GetTenantByAsync(Expression<Func<Tenant, bool>> predicate)
        => await tenantContext.Tenants.FirstOrDefaultAsync(predicate);

    /// <summary>
    /// Creates a new tenant entity, applies changes to the context,
    /// updates the connection string, and runs database migrations.
    /// </summary>
    /// <param name="tenant">The tenant entity to be created.</param>
    public async Task CreateTenantAsync(Tenant tenant)
    {
        await tenantContext.Tenants.AddAsync(tenant);
        await tenantContext.ApplyChangesAsync();

        await SetConnectionStringAsync(tenant);

        await Appcontext.Database.MigrateAsync();
    }

    /// <summary>
    /// Deletes a tenant entity matching the specified predicate, applies changes,
    /// updates the connection string, and ensures the tenant database is deleted.
    /// </summary>
    /// <param name="predicate">Expression used to filter the tenant to delete.</param>
    public async Task DeleteTenantByAsync(Expression<Func<Tenant, bool>> predicate)
    {
        var tenant = await GetTenantByAsync(predicate)
            ?? throw new Exception("Unable to find tenant to delete");

        tenantContext.Tenants.Remove(tenant);
        await tenantContext.ApplyChangesAsync();

        await SetConnectionStringAsync(tenant);

        await Appcontext.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Sets the database connection string for the current tenant.
    /// </summary>
    /// <param name="tenant">The tenant entity whose database connection string will be set.</param>
    private Task SetConnectionStringAsync(Tenant tenant)
    {
        var connectionString = PsalmsDatabase.GetDbConnectionStringBase(config, tenant.DatabaseName);

        Appcontext.Database.SetConnectionString(connectionString);

        return Task.CompletedTask;
    }
    #endregion
}