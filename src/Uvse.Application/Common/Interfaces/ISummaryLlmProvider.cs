using Uvse.Application.Summaries.Common;

namespace Uvse.Application.Common.Interfaces;

public interface ISummaryLlmProvider
{
    string ProviderKey { get; }
    Task<string> GenerateSummaryAsync(SummaryLlmRequest request, CancellationToken cancellationToken);
}
