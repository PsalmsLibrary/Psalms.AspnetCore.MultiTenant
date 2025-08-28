namespace Psalms.AspNetCore.MultiTenant.Models;

public interface ITenantModelBase
{
    int Id              { get; set; } 
    string Name         { get; set; }
    string Subdomain    { get; set; }
    string DatabaseName { get; set; }
}