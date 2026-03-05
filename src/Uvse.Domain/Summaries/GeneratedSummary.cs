using Uvse.Domain.Common;

namespace Uvse.Domain.Summaries;

public sealed class GeneratedSummary : ITenantOwned
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string ProviderKey { get; private set; } = string.Empty;
    public string RequestedByUserId { get; private set; } = string.Empty;
    public SummaryTargetType TargetType { get; private set; } = SummaryTargetType.Provider;
    public Guid? ProjectId { get; private set; }
    public Guid? DatasourceId { get; private set; }
    public Guid? ComparisonSummaryId { get; private set; }
    public SummaryRequestedModes RequestedModes { get; private set; } = SummaryRequestedModes.None;
    public SummaryDetailLevel DetailLevel { get; private set; }
    public SummaryAudienceTone AudienceTone { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset FromUtc { get; private set; }
    public DateTimeOffset ToUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private GeneratedSummary()
    {
    }

    public GeneratedSummary(
        Guid tenantId,
        string providerKey,
        string requestedByUserId,
        SummaryTargetType targetType,
        Guid? projectId,
        Guid? datasourceId,
        Guid? comparisonSummaryId,
        SummaryRequestedModes requestedModes,
        SummaryDetailLevel detailLevel,
        SummaryAudienceTone audienceTone,
        string title,
        string content,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        DateTimeOffset createdAtUtc)
    {
        TenantId = tenantId;
        ProviderKey = providerKey;
        RequestedByUserId = requestedByUserId;
        TargetType = targetType;
        ProjectId = projectId;
        DatasourceId = datasourceId;
        ComparisonSummaryId = comparisonSummaryId;
        RequestedModes = requestedModes;
        DetailLevel = detailLevel;
        AudienceTone = audienceTone;
        Title = title;
        Content = content;
        FromUtc = fromUtc;
        ToUtc = toUtc;
        CreatedAtUtc = createdAtUtc;
    }
}
