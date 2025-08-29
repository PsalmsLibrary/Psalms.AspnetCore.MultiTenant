using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Models;
using Psalms.AspNetCore.MultiTenant.Services;

namespace Psalms.AspNetCore.MultiTenant.Extensions;

public static class PsalmsTenantExtension
{
    public static IServiceCollection AddPsalmsMultiTenant<TenantModel, TenantContext, AppContext>(this IServiceCollection service, 
        Action<DbContextOptionsBuilder> tenantOptions, Action<DbContextOptionsBuilder> appContextOptions)
        where TenantModel : class, ITenantModelBase
        where AppContext : MultiTenantConfigureDbContext
        where TenantContext : DbContext, IPsalmsTenantDbContext<TenantModel>
    {
        service.AddDbContext<IPsalmsTenantDbContext<TenantModel>, TenantContext>(tenantOptions);
        service.AddDbContext<MultiTenantConfigureDbContext, AppContext>(appContextOptions);

        service.AddScoped<PsalmsTenantService<TenantModel>>();

        service.AddMemoryCache();
        return service; 
    }
}