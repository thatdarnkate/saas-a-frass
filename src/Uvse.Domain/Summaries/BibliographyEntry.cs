namespace Uvse.Domain.Summaries;

public sealed class BibliographyEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GeneratedSummaryId { get; private set; }
    public int Position { get; private set; }
    public string Hyperlink { get; private set; } = string.Empty;
    public string SourceText { get; private set; } = string.Empty;
    public GeneratedSummary GeneratedSummary { get; private set; } = null!;

    private BibliographyEntry()
    {
    }

    public BibliographyEntry(Guid generatedSummaryId, int position, string hyperlink, string sourceText)
    {
        GeneratedSummaryId = generatedSummaryId;
        Position = position;
        Hyperlink = hyperlink;
        SourceText = sourceText;
    }
}
