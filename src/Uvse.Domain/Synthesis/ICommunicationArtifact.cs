namespace Uvse.Domain.Synthesis;

public interface ICommunicationArtifact : IProviderArtifact
{
    string ConversationId { get; }
    string? ThreadId { get; }
    string MessageType { get; }
    string SenderUserId { get; }
    string? SenderDisplayName { get; }
    string ChannelId { get; }
    string ChannelName { get; }
    IReadOnlyCollection<string> ParticipantUserIds { get; }
    string Body { get; }
    DateTimeOffset SentAtUtc { get; }
    int ReplyCount { get; }
}
