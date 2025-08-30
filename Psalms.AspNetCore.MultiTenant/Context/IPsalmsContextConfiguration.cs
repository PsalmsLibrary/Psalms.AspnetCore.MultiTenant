using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Psalms.AspNetCore.MultiTenant.Context;

/// <summary>
/// Provides configuration actions for setting up DbContext options
/// for both tenant-specific and application-wide contexts in a multi-tenant environment.
/// </summary>
public interface IPsalmsContextConfiguration
{
    /// <summary>
    /// Returns an action to configure the DbContext options for a tenant context using the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration source for tenant context setup.</param>
    /// <returns>An action to configure <see cref="DbContextOptionsBuilder"/> for the tenant context.</returns>
    Action<DbContextOptionsBuilder> TenantContextConfig(IConfiguration configuration);

    /// <summary>
    /// Returns an action to configure the DbContext options for the application context.
    /// </summary>
    /// <returns>An action to configure <see cref="DbContextOptionsBuilder"/> for the application context.</returns>
    Action<DbContextOptionsBuilder> AppContextConfig();

}