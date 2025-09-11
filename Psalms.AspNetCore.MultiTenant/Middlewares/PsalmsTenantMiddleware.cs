using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Psalms.AspNetCore.MultiTenant.Context;
using Psalms.AspNetCore.MultiTenant.Enums;
using Psalms.AspNetCore.MultiTenant.Models;
using Psalms.AspNetCore.MultiTenant.Services;

namespace Psalms.AspNetCore.MultiTenant.Middlewares;

/// <summary>
/// Middleware responsible for resolving and caching tenant information
/// based on the authenticated user's claims in a multi-tenant application.
/// </summary>
/// <typeparam name="TenantModel">The tenant entity type, which must implement <see cref="ITenantModelBase"/>.</typeparam>
public class PsalmsTenantMiddleware<TenantModel, AppContext>(RequestDelegate next)
    where TenantModel : class, ITenantModelBase
    where AppContext : DbContext
{
    /// <summary>
    /// Processes the HTTP request to resolve the tenant from user claims,
    /// caches tenant data, and sets the database context for the request.
    /// Returns 403 if the tenant is not found.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous middleware operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantIdClaim = context.User?.FindFirst(TenantInfo.TenantId.ToString())?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || !int.TryParse(tenantIdClaim, out var tenantId))
        {
            await next(context);
            return;
        }

        var tenantDb = context.RequestServices.GetRequiredService<IPsalmsTenantDbContext<TenantModel>>();
        var appDb    = context.RequestServices.GetRequiredService<AppContext>();
        var config   = context.RequestServices.GetRequiredService<IConfiguration>();

        var tenant = await tenantDb.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant == null)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Tenant not found.");
            return;
        }

        appDb.Database.SetConnectionString(PsalmsDatabase.GetDbConnectionStringBase(config, tenant.Name));
       
        await next(context);
    }
}