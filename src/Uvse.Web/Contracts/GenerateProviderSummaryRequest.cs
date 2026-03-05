using Uvse.Domain.Summaries;

namespace Uvse.Web.Contracts;

public sealed record GenerateProviderSummaryRequest(
    string ProviderKey,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    SummaryDetailLevel DetailLevel,
    SummaryAudienceTone AudienceTone);
