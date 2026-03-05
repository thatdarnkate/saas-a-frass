namespace Uvse.Application.Common.Models;

public sealed record ProviderSummaryResult(
    Guid SummaryId,
    string Title,
    string Content,
    IReadOnlyCollection<BibliographyEntryResult> BibliographyEntries);
