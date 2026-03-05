namespace Uvse.Domain.Synthesis;

public interface IDocumentArtifact : IProviderArtifact
{
    string DocumentType { get; }
    string ContentType { get; }
    string? AuthorUserId { get; }
    string? LastModifiedByUserId { get; }
    DateTimeOffset? CreatedAtUtc { get; }
    DateTimeOffset LastModifiedAtUtc { get; }
    string? ContainerName { get; }
    IReadOnlyCollection<string> Tags { get; }
}
