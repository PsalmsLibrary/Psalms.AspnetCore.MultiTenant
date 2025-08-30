namespace Psalms.AspNetCore.MultiTenant.Enums;

/// <summary>
/// Specifies keys used to identify and access tenant-related information
/// such as tenant ID, database name, and tenant object in a multi-tenant environment.
/// </summary>
public enum TenantInfo
{
    /// <summary>
    /// Represents the unique identifier of the tenant.
    /// </summary>
    TenantId,

    /// <summary>
    /// Represents the name of the tenant's database.
    /// </summary>
    DatabaseName,

    /// <summary>
    /// Represents the tenant object or entity.
    /// </summary>
    Tenant
}