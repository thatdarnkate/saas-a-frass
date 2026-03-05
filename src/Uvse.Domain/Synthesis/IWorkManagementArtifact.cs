namespace Uvse.Domain.Synthesis;

public interface IWorkManagementArtifact : IProviderArtifact
{
    string WorkItemKey { get; }
    string WorkItemType { get; }
    string Status { get; }
    string? Priority { get; }
    string? AssigneeUserId { get; }
    string? ReporterUserId { get; }
    DateTimeOffset? CreatedAtUtc { get; }
    DateTimeOffset UpdatedAtUtc { get; }
    string? IterationName { get; }
    string? ParentWorkItemKey { get; }
}
