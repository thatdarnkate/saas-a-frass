using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Common;

internal static class BibliographyEntryFactory
{
    public static IReadOnlyList<BibliographyEntry> Create(Guid summaryId, IReadOnlyList<ArtifactRecord> artifacts)
    {
        return artifacts
            .Select((artifact, index) => new BibliographyEntry(
                summaryId,
                index + 1,
                artifact.SourceUrl,
                artifact.Summary))
            .ToArray();
    }
}
