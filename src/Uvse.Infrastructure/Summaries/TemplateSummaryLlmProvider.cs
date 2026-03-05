using Uvse.Application.Common.Interfaces;
using Uvse.Application.Summaries.Common;

namespace Uvse.Infrastructure.Summaries;

internal sealed class TemplateSummaryLlmProvider : ISummaryLlmProvider
{
    public string ProviderKey { get; }

    public TemplateSummaryLlmProvider(string providerKey)
    {
        ProviderKey = providerKey;
    }

    public Task<string> GenerateSummaryAsync(SummaryLlmRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(TemplateSummaryComposer.Compose(
            request.Title,
            request.RequestedModes,
            request.Artifacts,
            request.ComparisonSummary));
    }
}
