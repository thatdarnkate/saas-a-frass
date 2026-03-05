namespace Uvse.Domain.Synthesis;

public interface IWorkManagementArtifactSource : IArtifactSource
{
    Task<IReadOnlyCollection<IWorkManagementArtifact>> GetWorkItemsAsync(ArtifactQuery query, CancellationToken cancellationToken);

    async Task<IReadOnlyCollection<ArtifactRecord>> IArtifactSource.GetArtifactsAsync(
        ArtifactQuery query,
        CancellationToken cancellationToken)
    {
        var workItems = await GetWorkItemsAsync(query, cancellationToken);
        return workItems.Select(workItem => workItem.ToArtifactRecord()).ToArray();
    }
}
