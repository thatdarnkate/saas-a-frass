using Uvse.Domain.Summaries;

namespace Uvse.Web.Contracts;

public sealed record GenerateDatasourceSummaryRequest(
    string RequesterId,
    Guid DatasourceId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    IReadOnlyCollection<SummaryRequestedModes> RequestedModes,
    string LlmProvider,
    string? LlmModel = null,
    Guid? ComparisonSummaryId = null);
