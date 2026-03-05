namespace Uvse.Domain.Synthesis;

public interface ICommunicationArtifactSource : IArtifactSource
{
    Task<IReadOnlyCollection<ICommunicationArtifact>> GetMessagesAsync(ArtifactQuery query, CancellationToken cancellationToken);

    async Task<IReadOnlyCollection<ArtifactRecord>> IArtifactSource.GetArtifactsAsync(
        ArtifactQuery query,
        CancellationToken cancellationToken)
    {
        var messages = await GetMessagesAsync(query, cancellationToken);
        return messages.Select(message => message.ToArtifactRecord()).ToArray();
    }
}
