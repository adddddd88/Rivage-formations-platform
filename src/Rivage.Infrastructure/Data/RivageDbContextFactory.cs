using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rivage.Infrastructure.Data;

public class RivageDbContextFactory : IDesignTimeDbContextFactory<RivageDbContext>
{
    public RivageDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                 ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_HOST")
                 ?? "Host=localhost;Port=5433;Database=RivageDb;Username=rivage;Password=Rivage_Pg_S3cure!";

        var options = new DbContextOptionsBuilder<RivageDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new RivageDbContext(options);
    }
}
