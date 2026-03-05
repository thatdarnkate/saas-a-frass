using Uvse.Domain.Common;

namespace Uvse.Domain.Projects;

public sealed class Project : ITenantOwned
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<ProjectUser> ProjectUsers { get; private set; } = [];
    public ICollection<ProjectDatasource> ProjectDatasources { get; private set; } = [];

    private Project()
    {
    }

    public Project(Guid tenantId, string name, string createdByUserId, DateTimeOffset createdAtUtc)
    {
        TenantId = tenantId;
        Name = name;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public void Rename(string name)
    {
        Name = name;
    }
}
