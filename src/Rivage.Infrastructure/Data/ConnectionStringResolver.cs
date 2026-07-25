using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Rivage.Infrastructure.Data;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("DATABASE_URL"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
            configuration["CONNECTION_STRING"],
            configuration["DATABASE_URL"],
            configuration.GetConnectionString("DefaultConnection")
        };

        var raw = candidates.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "No database connection configured. On Railway: add PostgreSQL in the SAME project as the web service, " +
                "then on the web service Variables add a reference to Postgres DATABASE_URL " +
                "(name it DATABASE_URL or ConnectionStrings__DefaultConnection).");
        }

        // Unresolved Railway template left as-is
        if (raw.Contains("${{", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Connection string still contains unresolved ${{...}} placeholders. " +
                "In Railway Variables, use Add Variable Reference (do not paste ${{Postgres...}} as plain text), " +
                "or copy the real DATABASE_URL from the Postgres service Connect tab.");
        }

        var isDevelopment = environment?.IsDevelopment() == true
            || string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

        if (!isDevelopment && raw.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production is using a localhost database connection. " +
                "Point ConnectionStrings__DefaultConnection / DATABASE_URL at your Railway Postgres.");
        }

        var normalized = Normalize(raw);
        EnsureUsable(normalized);
        return normalized;
    }

    private static void EnsureUsable(string connectionString)
    {
        var parts = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("Host", out var host) || string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "Database Host is empty. Link Railway Postgres variables to the web service " +
                "(prefer referencing DATABASE_URL from the Postgres service).");
        }

        if (!parts.TryGetValue("Username", out var user) || string.IsNullOrWhiteSpace(user))
        {
            throw new InvalidOperationException("Database Username is empty in the connection string.");
        }

        if (!parts.TryGetValue("Database", out var db) || string.IsNullOrWhiteSpace(db))
        {
            throw new InvalidOperationException("Database name is empty in the connection string.");
        }
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
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
