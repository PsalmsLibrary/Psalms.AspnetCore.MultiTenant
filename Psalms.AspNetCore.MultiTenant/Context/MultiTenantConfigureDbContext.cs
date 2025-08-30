﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Psalms.AspNetCore.MultiTenant.Enums;
using Psalms.AspNetCore.MultiTenant.Services;

namespace Psalms.AspNetCore.MultiTenant.Context;

/// <summary>
/// Represents a DbContext configured for multi-tenant scenarios.
/// Sets the database connection string dynamically based on tenant information
/// retrieved from the memory cache and configuration.
/// </summary>
public class MultiTenantConfigureDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="MultiTenantConfigureDbContext"/>,
    /// setting the connection string according to the current tenant.
    /// </summary>
    /// <param name="options">The options for configuring the DbContext.</param>
    /// <param name="cache">The memory cache used to retrieve tenant information.</param>
    /// <param name="config">The configuration source for database settings.</param>
    public MultiTenantConfigureDbContext(DbContextOptions options, IHttpContextAccessor accessor, IConfiguration config) : base(options)
    {
        var dbName = accessor.HttpContext.Items[TenantInfo.DatabaseName];

        if (dbName is not null)
            Database.SetConnectionString(PsalmsDatabase.GetDbConnectionStringBase(config, dbName.ToString()!));
    }
}