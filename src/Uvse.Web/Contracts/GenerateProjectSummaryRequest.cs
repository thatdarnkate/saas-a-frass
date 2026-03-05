using Uvse.Domain.Summaries;

namespace Uvse.Web.Contracts;

public sealed record GenerateProjectSummaryRequest(
    string RequesterId,
    Guid ProjectId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    IReadOnlyCollection<SummaryRequestedModes> RequestedModes,
    string LlmProvider,
    string? LlmModel = null,
    Guid? ComparisonSummaryId = null);
