using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rivage.Domain.Entities;
using Rivage.Domain.Interfaces;
using Rivage.Infrastructure.Data;
using Rivage.Infrastructure.Seed;
using Rivage.Infrastructure.Services;

namespace Rivage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRivageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RivageDbContext>((sp, options) =>
        {
            var env = sp.GetService<IHostEnvironment>();
            var connectionString = ConnectionStringResolver.Resolve(configuration, env);
            options.UseNpgsql(connectionString);
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<RivageDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

        services.Configure<AnamOptions>(opts =>
        {
            configuration.GetSection(AnamOptions.SectionName).Bind(opts);
            opts.ApiKey ??= Environment.GetEnvironmentVariable("ANAM_API_KEY");
            opts.AvatarId = Environment.GetEnvironmentVariable("ANAM_AVATAR_ID") ?? opts.AvatarId;
            opts.AvatarModel = Environment.GetEnvironmentVariable("ANAM_AVATAR_MODEL") ?? opts.AvatarModel;
            opts.VoiceId = Environment.GetEnvironmentVariable("ANAM_VOICE_ID") ?? opts.VoiceId;
            opts.LlmId = Environment.GetEnvironmentVariable("ANAM_LLM_ID") ?? opts.LlmId;
        });

        services.AddHttpClient<AnamAiAvatarService>();
        services.AddSingleton<MockAiAvatarService>();
        services.AddScoped<IAiAvatarService>(sp => sp.GetRequiredService<AnamAiAvatarService>());
        services.AddScoped<QuizScoringService>();
        services.AddScoped<EnrollmentService>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}