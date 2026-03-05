namespace Uvse.Domain.Synthesis;

public interface IProviderArtifact
{
    string ExternalId { get; }
    string Title { get; }
    string Summary { get; }
    string SourceUrl { get; }
    DateTimeOffset OccurredAtUtc { get; }
    string VisibleToUserId { get; }

    ArtifactRecord ToArtifactRecord()
    {
        return new ArtifactRecord(
            ExternalId,
            Title,
            Summary,
            SourceUrl,
            OccurredAtUtc,
            VisibleToUserId);
    }
}
