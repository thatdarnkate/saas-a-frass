using Uvse.Domain.Summaries;

namespace Uvse.Web.Contracts;

public sealed record GenerateDatasourceSummaryRequest(
    string RequesterId,
    Guid DatasourceId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    IReadOnlyCollection<SummaryRequestedModes> RequestedModes,
    Guid? ComparisonSummaryId = null);
