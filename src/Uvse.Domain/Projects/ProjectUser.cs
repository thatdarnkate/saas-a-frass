namespace Uvse.Domain.Projects;

public sealed class ProjectUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public string UserId { get; private set; } = string.Empty;

    public Project Project { get; private set; } = null!;

    private ProjectUser()
    {
    }

    public ProjectUser(Guid projectId, string userId)
    {
        ProjectId = projectId;
        UserId = userId;
    }
}
