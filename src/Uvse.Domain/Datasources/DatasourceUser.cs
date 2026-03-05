namespace Uvse.Domain.Datasources;

public sealed class DatasourceUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DatasourceId { get; private set; }
    public string UserId { get; private set; } = string.Empty;

    public Datasource Datasource { get; private set; } = null!;

    private DatasourceUser()
    {
    }

    public DatasourceUser(Guid datasourceId, string userId)
    {
        DatasourceId = datasourceId;
        UserId = userId;
    }
}
