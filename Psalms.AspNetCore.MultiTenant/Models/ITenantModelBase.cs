namespace Psalms.AspNetCore.MultiTenant.Models;

/// <summary>
/// Represents the base contract for tenant models in a multi-tenant architecture,
/// defining essential properties such as Id, Name, Subdomain, and DatabaseName.
/// </summary>
public interface ITenantModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the subdomain associated with the tenant.
    /// </summary>
    string Subdomain { get; set; }

    /// <summary>
    /// Gets or sets the database name used by the tenant.
    /// </summary>
    string DatabaseName { get; set; }
}