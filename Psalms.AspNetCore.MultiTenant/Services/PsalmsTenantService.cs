using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.DTOs;
using Psalms.AspNetCore.MultiTenant.Models;
using Psalms.EFCore.DataManager.UnitOfWork;
using System.Linq.Expressions;

namespace Psalms.AspNetCore.MultiTenant.Services;

public class PsalmsTenantService(PsalmsTenantDbContext tenantContext, IConfiguration config) : IPsalmsTenantService
{
    #region UnitOfWork
    private readonly Lazy<IPsalmsUnitOfWork<PsalmsTenantDbContext>> _psalmsUnitOfWork 
        = new(() => new PsalmsUnitOfWork<PsalmsTenantDbContext>(tenantContext));
    #endregion

    #region IPsalmsTenantService Methods
    public async Task<TenantModel?> GetTenantByAsync(Expression<Func<TenantModel, bool>> predicate)
        => await _psalmsUnitOfWork.Value.Repository.GetByAsync(predicate);

    public async Task CreateTenantAsync(TenantDto tenant, DbContext context)
    {
        await _psalmsUnitOfWork.Value.Repository.CreateAsync(tenant.Map());
        await _psalmsUnitOfWork.Value.SaveChangesAsync();

        var connectionString = GetDbConnectionString(config, tenant.DatabaseName);
        
        context.Database.SetConnectionString(connectionString);
        await context.Database.MigrateAsync();
    }
    public static string GetDbConnectionString(IConfiguration config, string databaseName) => string.Format(config["DbConnectionBase"] ??
            throw new Exception("Multi Tenant connection not found"), databaseName);
    #endregion
}