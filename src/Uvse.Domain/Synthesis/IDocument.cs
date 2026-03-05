using Uvse.Domain.Plugins;

namespace Uvse.Domain.Synthesis;

public interface IDocument : IProvider
{
    new IDocumentArtifactSource ArtifactSource { get; }

    ConceptualDomain IProvider.Domain => ConceptualDomain.Document;
    IArtifactSource IProvider.ArtifactSource => ArtifactSource;
}
