using Uvse.Domain.Plugins;

namespace Uvse.Domain.Synthesis;

public interface ICommunication : IProvider
{
    new ICommunicationArtifactSource ArtifactSource { get; }

    ConceptualDomain IProvider.Domain => ConceptualDomain.Communication;
    IArtifactSource IProvider.ArtifactSource => ArtifactSource;
}
