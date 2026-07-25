using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Rivage.Infrastructure.Data;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var raw =
            Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Database connection is missing. On Railway: add a PostgreSQL service, then set " +
                "ConnectionStrings__DefaultConnection (or DATABASE_URL) on the web service.");
        }

        var isDevelopment = environment?.IsDevelopment() == true
            || string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

        if (!isDevelopment && raw.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production is still using a localhost database connection. " +
                "Set ConnectionStrings__DefaultConnection from your Railway PostgreSQL variables.");
        }

        return Normalize(raw);
    }

    private static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
