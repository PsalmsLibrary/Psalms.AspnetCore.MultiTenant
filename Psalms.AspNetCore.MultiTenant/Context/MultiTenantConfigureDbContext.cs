using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Psalms.AspNetCore.MultiTenant.Enums;
using Psalms.AspNetCore.MultiTenant.Services;

namespace Psalms.AspNetCore.MultiTenant.Context;

public class MultiTenantConfigureDbContext : DbContext
{
    public MultiTenantConfigureDbContext(DbContextOptions options, IMemoryCache cache, IConfiguration config) : base(options)
    {
        Database.SetConnectionString(PsalmsTenantService.GetDbConnectionString
            (
                config, 
                cache.Get<string>(TenantInfo.DatabaseName) ?? throw new Exception("Database name not found in cache"))
            );
    }
}