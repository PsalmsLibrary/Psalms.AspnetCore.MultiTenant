using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Enums;
using Psalms.AspNetCore.MultiTenant.Models;

namespace Psalms.AspNetCore.MultiTenant.Middlewares;

public class PsalmsTenantMiddleware<TenantModel>(RequestDelegate next) where TenantModel : class, ITenantModelBase
{
    public async Task InvokeAsync(HttpContext context)
    { 
        var tenantIdClaim = context.User?.FindFirst(TenantInfo.TenantId.ToString())?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || !int.TryParse(tenantIdClaim, out var tenantId))
        {
            await next(context);
            return;
        }

        using var scope = context.RequestServices.CreateScope();
        var tenantDb    = scope.ServiceProvider.GetRequiredService<IPsalmsTenantDbContext<TenantModel>>();
        var memory      = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var tenant = await tenantDb.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == int.Parse(tenantIdClaim));

        if (tenant == null)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Tenant id not founded.");
            return;
        }

        memory.Set(TenantInfo.DatabaseName, tenant.DatabaseName);
        memory.Set(TenantInfo.Tenant, tenant);

        await next(context);
    }
}
