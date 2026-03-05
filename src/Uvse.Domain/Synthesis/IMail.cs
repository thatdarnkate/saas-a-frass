using Uvse.Domain.Plugins;

namespace Uvse.Domain.Synthesis;

public interface IMail : IProvider
{
    new IMailArtifactSource ArtifactSource { get; }

    ConceptualDomain IProvider.Domain => ConceptualDomain.Mail;
    IArtifactSource IProvider.ArtifactSource => ArtifactSource;
}
