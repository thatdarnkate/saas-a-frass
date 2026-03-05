using Microsoft.EntityFrameworkCore;
using Uvse.Domain.Plugins;
using Uvse.Domain.Summaries;

namespace Uvse.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TenantPlugin> TenantPlugins { get; }
    DbSet<GeneratedSummary> GeneratedSummaries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
