using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Uvse.Application.Common.Interfaces;
using Uvse.Domain.Synthesis;
using Uvse.Infrastructure.BackgroundJobs;
using Uvse.Infrastructure.Persistence;
using Uvse.Infrastructure.Providers;
using Uvse.Infrastructure.Security;
using Uvse.Infrastructure.Summaries;
using Uvse.Infrastructure.Tenancy;

namespace Uvse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("UvseDb")
            ?? throw new InvalidOperationException("Connection string 'UvseDb' is not configured.");

        services.AddHttpContextAccessor();

        // TenantContext is registered as itself so background jobs can call SetTenantId,
        // and as ITenantService for the rest of the application.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantService>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<UvseDbContext>());

        services.AddDbContext<UvseDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // IDbContextFactory is required for background workers that create short-lived,
        // tenant-scoped contexts outside of the normal request lifetime.
        // Worker pattern: create a scope, call tenantContext.SetTenantId(id), then use the factory.
        services.AddDbContextFactory<UvseDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }, ServiceLifetime.Scoped);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");
            options.InstanceName = "uvse:";
        });

        var dataProtection = services.AddDataProtection()
            .SetApplicationName("Uvse");

        var keyRingPath = configuration["Security:DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            Directory.CreateDirectory(keyRingPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }
        else if (environment.IsDevelopment())
        {
            var developmentKeyPath = Path.Combine(environment.ContentRootPath, ".keys");
            Directory.CreateDirectory(developmentKeyPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(developmentKeyPath));
        }
        else
        {
            throw new InvalidOperationException(
                "Security:DataProtection:KeyRingPath must be configured outside Development so encrypted plugin settings remain decryptable across restarts and instances.");
        }

        services.AddScoped<IPluginSettingsEncryptor, DataProtectionPluginSettingsEncryptor>();
        services.AddSingleton<ISummaryLlmProvider>(_ => new TemplateSummaryLlmProvider("template"));
        services.AddSingleton<ISummaryLlmProvider>(_ => new TemplateSummaryLlmProvider("openai"));
        services.AddSingleton<ISummaryLlmProvider>(_ => new TemplateSummaryLlmProvider("gemini"));
        services.AddSingleton<ISummaryLlmProvider>(_ => new TemplateSummaryLlmProvider("claude"));
        services.AddSingleton<ISummaryLlmProvider>(_ => new TemplateSummaryLlmProvider("copilot"));
        services.AddSingleton<ISummaryLlmRegistry, SummaryLlmRegistry>();

        services.AddScoped<IFeatureService, TenantFeatureService>();

        if (environment.IsDevelopment())
        {
            services.AddSingleton<IProvider, MockJiraProvider>();
        }

        services.AddSingleton<IProviderRegistry, ProviderRegistry>();

        services.AddQuartz(options =>
        {
            var jobKey = new JobKey(nameof(SummaryIngestionHeartbeatJob));
            options.AddJob<SummaryIngestionHeartbeatJob>(builder => builder.WithIdentity(jobKey));
            options.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(SummaryIngestionHeartbeatJob)}-trigger")
                .StartNow()
                .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(15)).RepeatForever()));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
