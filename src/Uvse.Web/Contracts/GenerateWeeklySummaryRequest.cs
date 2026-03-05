using Uvse.Domain.Summaries;

namespace Uvse.Web.Contracts;

public sealed record GenerateWeeklySummaryRequest(
    string ProviderKey,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    SummaryDetailLevel DetailLevel,
    SummaryAudienceTone AudienceTone);
