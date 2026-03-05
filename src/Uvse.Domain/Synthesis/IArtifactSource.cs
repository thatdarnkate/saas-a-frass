namespace Uvse.Domain.Synthesis;

public interface IArtifactSource
{
    Task<IReadOnlyCollection<ArtifactRecord>> GetArtifactsAsync(ArtifactQuery query, CancellationToken cancellationToken);
}
