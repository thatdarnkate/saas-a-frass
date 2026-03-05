using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Common;

public sealed record SummaryLlmRequest(
    string ProviderKey,
    string? Model,
    SummaryTargetType TargetType,
    string Title,
    SummaryRequestedModes RequestedModes,
    IReadOnlyList<ArtifactRecord> Artifacts,
    GeneratedSummary? ComparisonSummary);
