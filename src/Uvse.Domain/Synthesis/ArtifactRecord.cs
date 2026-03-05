namespace Uvse.Domain.Synthesis;

public sealed record ArtifactRecord(
    string ExternalId,
    string Title,
    string Summary,
    string SourceUrl,
    DateTimeOffset OccurredAtUtc,
    string VisibleToUserId);
