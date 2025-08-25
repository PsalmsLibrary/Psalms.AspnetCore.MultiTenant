using Psalms.AspNetCore.MultiTenant.Models;
using System.ComponentModel.DataAnnotations;

namespace Psalms.AspNetCore.MultiTenant.DTOs;

public class TenantDto
{
    [Required(ErrorMessage="Tenant name is required.")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tenant subdomain is required.")]
    public string Subdomain { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tenant databaseName is required.")]
    public string DatabaseName { get; set; } = string.Empty;

    public TenantModel Map()
        => new()
        {
            Name         = Name,
            Subdomain    = Subdomain,
            DatabaseName = DatabaseName
        };
}