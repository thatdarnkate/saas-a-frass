using Uvse.Domain.Plugins;
using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Infrastructure.Providers;

internal sealed class MockJiraProvider : IProvider, IArtifactSource
{
    public string ProviderKey => "jira-mock";
    public string DisplayName => "Mock Jira";
    public ConceptualDomain Domain => ConceptualDomain.WorkManagement;
    public IArtifactSource ArtifactSource => this;

    public Task<IReadOnlyCollection<ArtifactRecord>> GetArtifactsAsync(ArtifactQuery query, CancellationToken cancellationToken)
    {
        var allArtifacts = new[]
        {
            new ArtifactRecord(
                "UVSE-101",
                "Delivered tenant-aware summary endpoint",
                "Implemented the weekly summary API and validated tenant-scoped access for enabled providers.",
                "https://jira.example.local/browse/UVSE-101",
                query.ToUtc.AddDays(-1),
                query.RequestingUserId),
            new ArtifactRecord(
                "UVSE-102",
                "Reduced synthesis latency",
                query.AudienceTone == SummaryAudienceTone.Technical
                    ? "Added explicit projections and no-tracking reads for plugin configuration lookups."
                    : "Improved summary generation speed during busy reporting windows.",
                "https://jira.example.local/browse/UVSE-102",
                query.ToUtc.AddDays(-2),
                query.RequestingUserId),
            new ArtifactRecord(
                "UVSE-103",
                "Hardened provider activation flow",
                "Added admin-only plugin enablement with strict tenant binding and audit-friendly timestamps.",
                "https://jira.example.local/browse/UVSE-103",
                query.ToUtc.AddDays(-3),
                query.RequestingUserId)
        };

        IReadOnlyCollection<ArtifactRecord> visibleArtifacts = allArtifacts
            .Where(artifact =>
                artifact.VisibleToUserId == query.RequestingUserId &&
                artifact.OccurredAtUtc >= query.FromUtc &&
                artifact.OccurredAtUtc <= query.ToUtc)
            .ToArray();

        return Task.FromResult(visibleArtifacts);
    }
}
