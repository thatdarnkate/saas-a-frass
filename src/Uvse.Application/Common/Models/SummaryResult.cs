using Uvse.Domain.Summaries;

namespace Uvse.Application.Common.Models;

public sealed record SummaryResult(
    Guid SummaryId,
    string Title,
    string Content,
    SummaryTargetType TargetType,
    Guid? ProjectId,
    Guid? DatasourceId,
    SummaryRequestedModes RequestedModes,
    string LlmProviderKey,
    string? LlmModel,
    string RequestedByUserId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    DateTimeOffset CreatedAtUtc);
