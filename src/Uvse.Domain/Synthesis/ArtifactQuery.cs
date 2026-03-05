using Uvse.Domain.Summaries;

namespace Uvse.Domain.Synthesis;

public sealed record ArtifactQuery(
    Guid TenantId,
    string RequestingUserId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    SummaryDetailLevel DetailLevel,
    SummaryAudienceTone AudienceTone);
