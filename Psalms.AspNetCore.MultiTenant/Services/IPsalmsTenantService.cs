using Microsoft.EntityFrameworkCore;
using Psalms.AspNetCore.MultiTenant.DTOs;
using Psalms.AspNetCore.MultiTenant.Models;
using System.Linq.Expressions;

namespace Psalms.AspNetCore.MultiTenant.Services;

/// <summary>
/// Defines operations for managing tenants in multi-tenant environments.
/// </summary>
public interface IPsalmsTenantService
{
    /// <summary>
    /// Retrieves a tenant that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">Expression used to filter the desired tenant.</param>
    /// <returns>An instance of <see cref="TenantModel"/> matching the filter, or null if not found.</returns>
    Task<TenantModel?> GetTenantByAsync(Expression<Func<TenantModel, bool>> predicate);

    /// <summary>
    /// Creates a new tenant using the provided DTO and database context.
    /// </summary>
    /// <param name="tenant">Data of the tenant to be created.</param>
    /// <param name="context">Database context for persistence.</param>
    /// <returns>An asynchronous task representing the creation operation.</returns>
    Task CreateTenantAsync(TenantDto tenant, DbContext context);
}