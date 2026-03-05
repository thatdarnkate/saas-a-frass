using Uvse.Domain.Synthesis;

namespace Uvse.Application.Common.Interfaces;

public interface IProviderRegistry
{
    IProvider GetRequiredProvider(string providerKey);
}
