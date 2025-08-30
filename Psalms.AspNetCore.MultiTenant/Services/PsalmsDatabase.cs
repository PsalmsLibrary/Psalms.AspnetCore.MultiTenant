using Microsoft.Extensions.Configuration;

namespace Psalms.AspNetCore.MultiTenant.Services;

public class PsalmsDatabase
{
    public static string GetDbConnectionStringBase(IConfiguration config, string databaseName) => string.Format(config["DbConnectionBase"] ??
            throw new Exception("Multi Tenant connection not found"), databaseName);
}