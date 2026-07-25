using Microsoft.AspNetCore.HttpOverrides;
using Rivage.Infrastructure;
using Rivage.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRivageInfrastructure(builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller}/{action=Index}/{id?}",
    constraints: new
    {
        controller = "Categories|Formations|Modules|Trainers|Quizzes|AdminDashboard"
    });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await seeder.MigrateAndSeedAsync();
        logger.LogInformation("Database migrated and seeded.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/seed failed: {Message}", ex.Message);
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException(
                "Startup failed while connecting to the database. " +
                "On Railway, ensure Postgres is in the same project and the web service has DATABASE_URL " +
                "(or ConnectionStrings__DefaultConnection) set via Variable Reference. " +
                $"Inner error: {ex.Message}", ex);
    }
}

app.Run();

public partial class Program;