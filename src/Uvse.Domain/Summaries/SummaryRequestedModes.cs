namespace Uvse.Domain.Summaries;

[Flags]
public enum SummaryRequestedModes
{
    None = 0,
    Executive = 1,
    Detailed = 2,
    Delta = 4
}
