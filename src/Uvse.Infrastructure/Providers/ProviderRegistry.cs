using Uvse.Application.Common.Interfaces;
using Uvse.Domain.Synthesis;

namespace Uvse.Infrastructure.Providers;

internal sealed class ProviderRegistry : IProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IProvider> _providers;

    public ProviderRegistry(IEnumerable<IProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IProvider GetRequiredProvider(string providerKey)
    {
        if (_providers.TryGetValue(providerKey, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException($"Provider '{providerKey}' is not registered.");
    }
}
