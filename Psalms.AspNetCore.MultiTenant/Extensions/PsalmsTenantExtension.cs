using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Services;

namespace Psalms.AspNetCore.MultiTenant.Extensions;

public static class PsalmsTenantExtension
{
    public static IServiceCollection AddPsalmsMultiTenant(this IServiceCollection service, Action<DbContextOptionsBuilder> options)
    {
        service.AddDbContext<PsalmsTenantDbContext>(options);
        service.AddScoped<IPsalmsTenantService, PsalmsTenantService>();
        service.AddMemoryCache();
        return service;
    }
}