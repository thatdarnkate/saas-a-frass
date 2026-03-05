namespace Uvse.Application.Common.Models;

public sealed record BibliographyEntryResult(
    Guid BibliographyEntryId,
    int Position,
    string Hyperlink,
    string SourceText);
