using Uvse.Application.Common.Interfaces;

namespace Uvse.Infrastructure.Summaries;

internal sealed class SummaryLlmRegistry : ISummaryLlmRegistry
{
    private readonly IReadOnlyDictionary<string, ISummaryLlmProvider> _providers;

    public SummaryLlmRegistry(IEnumerable<ISummaryLlmProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public ISummaryLlmProvider GetRequiredProvider(string providerKey)
    {
        if (_providers.TryGetValue(providerKey, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException($"Summary LLM provider '{providerKey}' is not registered.");
    }
}
