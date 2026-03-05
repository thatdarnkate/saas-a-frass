namespace Uvse.Domain.Synthesis;

public interface IMailArtifact : IProviderArtifact
{
    string Subject { get; }
    string SenderAddress { get; }
    string? SenderDisplayName { get; }
    IReadOnlyCollection<string> ToAddresses { get; }
    IReadOnlyCollection<string> CcAddresses { get; }
    DateTimeOffset SentAtUtc { get; }
    DateTimeOffset? ReceivedAtUtc { get; }
    string BodyPreview { get; }
    string? ThreadId { get; }
    bool HasAttachments { get; }
}
