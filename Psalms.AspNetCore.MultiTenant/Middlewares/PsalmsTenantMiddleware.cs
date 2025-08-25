using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Enums;

namespace Psalms.AspNetCore.MultiTenant.Middlewares;

public class PsalmsTenantMiddleware(RequestDelegate next)
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
        var tenantDb    = scope.ServiceProvider.GetRequiredService<PsalmsTenantDbContext>();
        var memory      = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var dbName = (await tenantDb.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == int.Parse(tenantIdClaim)))?.DatabaseName;

        if (dbName == null)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Tenant id not founded.");
            return;
        }

        memory.Set(TenantInfo.DatabaseName, dbName);

        await next(context);
    }
}
