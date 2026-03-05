using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Uvse.Infrastructure.Tenancy;

namespace Uvse.Infrastructure.BackgroundJobs;

internal sealed class SummaryIngestionHeartbeatJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SummaryIngestionHeartbeatJob> _logger;

    public SummaryIngestionHeartbeatJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SummaryIngestionHeartbeatJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Summary ingestion heartbeat executed at {TimestampUtc}.", DateTimeOffset.UtcNow);

        // Pattern for tenant-scoped work in background jobs:
        //
        //   foreach (var tenantId in await GetAllTenantIds())
        //   {
        //       using var scope = _scopeFactory.CreateScope();
        //       var tenantCtx = scope.ServiceProvider.GetRequiredService<TenantContext>();
        //       tenantCtx.SetTenantId(tenantId);
        //
        //       var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UvseDbContext>>();
        //       await using var db = await factory.CreateDbContextAsync();
        //       // db will now apply the tenant query filter for tenantId
        //   }

        return Task.CompletedTask;
    }
}
