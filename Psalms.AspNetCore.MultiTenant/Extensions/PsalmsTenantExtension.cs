using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Models;
using Psalms.AspNetCore.MultiTenant.Services;

namespace Psalms.AspNetCore.MultiTenant.Extensions;

public static class PsalmsTenantExtension
{
    public static IServiceCollection AddPsalmsMultiTenant<TenantModel, TenantContext, AppContext>(this IServiceCollection service,
        IPsalmsContextConfiguration contextConfiguration, IConfiguration configuration)
        where TenantModel : class, ITenantModelBase
        where AppContext : MultiTenantConfigureDbContext
        where TenantContext : DbContext, IPsalmsTenantDbContext<TenantModel>
    {
        service.AddDbContext<IPsalmsTenantDbContext<TenantModel>, TenantContext>(contextConfiguration.TenantContextConfig(configuration));
        service.AddDbContext<MultiTenantConfigureDbContext, AppContext>(contextConfiguration.AppContextConfig());

        service.AddScoped<PsalmsTenantService<TenantModel>>();

        return service; 
    }
}