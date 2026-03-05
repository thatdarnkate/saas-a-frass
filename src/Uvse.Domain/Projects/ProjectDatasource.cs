using Uvse.Domain.Datasources;

namespace Uvse.Domain.Projects;

public sealed class ProjectDatasource
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public Guid DatasourceId { get; private set; }

    public Project Project { get; private set; } = null!;
    public Datasource Datasource { get; private set; } = null!;

    private ProjectDatasource()
    {
    }

    public ProjectDatasource(Guid projectId, Guid datasourceId)
    {
        ProjectId = projectId;
        DatasourceId = datasourceId;
    }
}
