using System.ComponentModel.DataAnnotations;

namespace Psalms.AspNetCore.MultiTenant.Models.Dto;

/// <summary>
/// A default DTO to PsalmsTenant
/// </summary>
public class PsalmsTenantDto
{
    [Required(ErrorMessage= "Tenant Name is required.")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tenant Subdomain is required.")]
    public string Subdomain { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tenant DatabaseName is required.")]
    public string DatabaseName { get; set; } = string.Empty;

    public PsalmsTenantModel GetModel()
        => new()
        {
            Name         = this.Name,
            Subdomain    = this.Subdomain,
            DatabaseName = this.DatabaseName,
        };
}
