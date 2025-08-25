namespace Psalms.AspNetCore.MultiTenant.Models;

public class TenantModel
{
    public int Id { get; set; } 
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}