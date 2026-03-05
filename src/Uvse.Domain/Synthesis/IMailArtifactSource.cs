namespace Uvse.Domain.Synthesis;

public interface IMailArtifactSource : IArtifactSource
{
    Task<IReadOnlyCollection<IMailArtifact>> GetMessagesAsync(ArtifactQuery query, CancellationToken cancellationToken);

    async Task<IReadOnlyCollection<ArtifactRecord>> IArtifactSource.GetArtifactsAsync(
        ArtifactQuery query,
        CancellationToken cancellationToken)
    {
        var messages = await GetMessagesAsync(query, cancellationToken);
        return messages.Select(message => message.ToArtifactRecord()).ToArray();
    }
}
