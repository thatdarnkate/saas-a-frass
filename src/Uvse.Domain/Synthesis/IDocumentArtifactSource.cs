namespace Uvse.Domain.Synthesis;

public interface IDocumentArtifactSource : IArtifactSource
{
    Task<IReadOnlyCollection<IDocumentArtifact>> GetDocumentsAsync(ArtifactQuery query, CancellationToken cancellationToken);

    async Task<IReadOnlyCollection<ArtifactRecord>> IArtifactSource.GetArtifactsAsync(
        ArtifactQuery query,
        CancellationToken cancellationToken)
    {
        var documents = await GetDocumentsAsync(query, cancellationToken);
        return documents.Select(document => document.ToArtifactRecord()).ToArray();
    }
}
