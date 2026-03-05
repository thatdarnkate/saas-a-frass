using System.Text;
using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Common;

internal static class SummaryComposer
{
    public static string Compose(
        string title,
        SummaryRequestedModes requestedModes,
        IReadOnlyList<ArtifactRecord> artifacts,
        GeneratedSummary? comparisonSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine(title);

        if (requestedModes.HasFlag(SummaryRequestedModes.Executive))
        {
            AppendExecutiveSection(builder, artifacts);
        }

        if (requestedModes.HasFlag(SummaryRequestedModes.Detailed))
        {
            AppendDetailedSection(builder, artifacts);
        }

        if (requestedModes.HasFlag(SummaryRequestedModes.Delta))
        {
            AppendDeltaSection(builder, artifacts, comparisonSummary);
        }

        AppendBibliography(builder, artifacts);
        return builder.ToString().TrimEnd();
    }

    private static void AppendExecutiveSection(StringBuilder builder, IReadOnlyList<ArtifactRecord> artifacts)
    {
        builder.AppendLine();
        builder.AppendLine("Executive Summary");
        foreach (var artifact in artifacts)
        {
            builder.AppendLine($"- {artifact.Title}");
        }
    }

    private static void AppendDetailedSection(StringBuilder builder, IReadOnlyList<ArtifactRecord> artifacts)
    {
        builder.AppendLine();
        builder.AppendLine("Detailed Summary");
        for (var index = 0; index < artifacts.Count; index++)
        {
            var artifact = artifacts[index];
            builder.AppendLine($"- {artifact.Title}: {artifact.Summary}{ToSuperscript(index + 1)}");
        }
    }

    private static void AppendDeltaSection(
        StringBuilder builder,
        IReadOnlyList<ArtifactRecord> artifacts,
        GeneratedSummary? comparisonSummary)
    {
        builder.AppendLine();
        builder.AppendLine("Delta Summary");

        if (comparisonSummary is null)
        {
            builder.AppendLine("- No previous summary was available for comparison.");
            return;
        }

        var previousTitles = ParseBibliographyTitles(comparisonSummary.Content);
        var currentTitles = artifacts.Select(artifact => artifact.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = currentTitles.Except(previousTitles, StringComparer.OrdinalIgnoreCase).OrderBy(title => title).ToArray();
        var removed = previousTitles.Except(currentTitles, StringComparer.OrdinalIgnoreCase).OrderBy(title => title).ToArray();

        builder.AppendLine($"- Compared against summary {comparisonSummary.Id} created at {comparisonSummary.CreatedAtUtc:O}.");

        if (added.Length == 0 && removed.Length == 0)
        {
            builder.AppendLine("- No title-level changes were detected against the comparison summary.");
            return;
        }

        if (added.Length > 0)
        {
            builder.AppendLine($"- Added: {string.Join(", ", added)}");
        }

        if (removed.Length > 0)
        {
            builder.AppendLine($"- Removed: {string.Join(", ", removed)}");
        }
    }

    private static void AppendBibliography(StringBuilder builder, IReadOnlyList<ArtifactRecord> artifacts)
    {
        builder.AppendLine();
        builder.AppendLine("Bibliography");
        for (var index = 0; index < artifacts.Count; index++)
        {
            var artifact = artifacts[index];
            builder.AppendLine(
                $"[{index + 1}] {artifact.Title} ({artifact.OccurredAtUtc:yyyy-MM-dd}) - {artifact.SourceUrl}");
        }
    }

    private static HashSet<string> ParseBibliographyTitles(string content)
    {
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Where(line => line.StartsWith("[", StringComparison.Ordinal)))
        {
            var closingBracket = line.IndexOf(']');
            var openParen = line.LastIndexOf(" (", StringComparison.Ordinal);
            if (closingBracket < 0 || openParen <= closingBracket)
            {
                continue;
            }

            var title = line[(closingBracket + 2)..openParen].Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                titles.Add(title);
            }
        }

        return titles;
    }

    private static string ToSuperscript(int number)
    {
        var digits = new[] { "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹" };
        return string.Concat(number.ToString().Select(digit => digits[digit - '0']));
    }
}
