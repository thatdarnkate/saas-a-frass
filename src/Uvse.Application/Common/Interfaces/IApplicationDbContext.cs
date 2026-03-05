using Microsoft.EntityFrameworkCore;
using Uvse.Domain.Datasources;
using Uvse.Domain.Plugins;
using Uvse.Domain.Projects;
using Uvse.Domain.Summaries;

namespace Uvse.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Datasource> Datasources { get; }
    DbSet<DatasourceUser> DatasourceUsers { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectDatasource> ProjectDatasources { get; }
    DbSet<ProjectUser> ProjectUsers { get; }
    DbSet<TenantPlugin> TenantPlugins { get; }
    DbSet<GeneratedSummary> GeneratedSummaries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
