using Uvse.Domain.Plugins;

namespace Uvse.Domain.Synthesis;

public interface IProvider
{
    string ProviderKey { get; }
    string DisplayName { get; }
    ConceptualDomain Domain { get; }
    IArtifactSource ArtifactSource { get; }
}
