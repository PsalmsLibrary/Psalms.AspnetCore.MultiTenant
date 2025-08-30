using Microsoft.EntityFrameworkCore;
using Psalms.AspNetCore.MultiTenant.Models;

namespace Psalms.AspNetCore.MultiTenant.Context;

/// <summary>
/// Defines the contract for a tenant-aware DbContext in a multi-tenant architecture.
/// Provides access to tenant entities and supports asynchronous persistence of changes.
/// </summary>
/// <typeparam name="TTenant">The tenant entity type, which must implement <see cref="ITenantModelBase"/>.</typeparam>
public interface IPsalmsTenantDbContext<TTenant> where TTenant : class, ITenantModelBase
{
    /// <summary>
    /// Gets or sets the DbSet containing tenant entities.
    /// </summary>
    public DbSet<TTenant> Tenants { get; set; }

    /// <summary>
    /// Persists all changes made in the context to the database asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task ApplyChangesAsync();
}