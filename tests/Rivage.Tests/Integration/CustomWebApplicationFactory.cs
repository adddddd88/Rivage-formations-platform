using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rivage.Infrastructure.Data;

namespace Rivage.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDbName = "RivageTests_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        var usePostgres = !string.IsNullOrWhiteSpace(connectionString);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Satisfy AddRivageInfrastructure before ConfigureTestServices replaces the provider.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = usePostgres
                    ? connectionString
                    : "Host=localhost;Port=5433;Database=RivageTestsDummy;Username=rivage;Password=dummy",
                ["TESTING"] = "1"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveDbContextRegistrations(services);

            if (usePostgres)
            {
                services.AddDbContext<RivageDbContext>(options =>
                    options.UseNpgsql(connectionString));
            }
            else
            {
                services.AddDbContext<RivageDbContext>(options =>
                    options.UseInMemoryDatabase(_inMemoryDbName));
            }
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<RivageDbContext>) ||
                d.ServiceType == typeof(RivageDbContext) ||
                (d.ServiceType.IsGenericType
                 && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)
                 && d.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(RivageDbContext)))
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);
    }
}
