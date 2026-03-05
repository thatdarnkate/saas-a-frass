using Uvse.Domain.Common;
using Uvse.Domain.Projects;

namespace Uvse.Domain.Datasources;

public sealed class Datasource : ITenantOwned
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DatasourceAccessScope AccessScope { get; private set; }
    public string ConnectionDetailsEncryptedJson { get; private set; } = string.Empty;

    public ICollection<DatasourceUser> DatasourceUsers { get; private set; } = [];
    public ICollection<ProjectDatasource> ProjectDatasources { get; private set; } = [];

    private Datasource()
    {
    }

    public Datasource(
        Guid tenantId,
        string name,
        string type,
        string createdByUserId,
        DateTimeOffset createdAtUtc,
        bool isActive,
        DatasourceAccessScope accessScope,
        string connectionDetailsEncryptedJson)
    {
        TenantId = tenantId;
        Name = name;
        Type = type;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        IsActive = isActive;
        AccessScope = accessScope;
        ConnectionDetailsEncryptedJson = connectionDetailsEncryptedJson;
    }

    public void Update(string name, string type, bool isActive, DatasourceAccessScope accessScope, string connectionDetailsEncryptedJson)
    {
        Name = name;
        Type = type;
        IsActive = isActive;
        AccessScope = accessScope;
        ConnectionDetailsEncryptedJson = connectionDetailsEncryptedJson;
    }
}
