using Uvse.Domain.Plugins;

namespace Uvse.Domain.Synthesis;

public interface IWorkManagement : IProvider
{
    new IWorkManagementArtifactSource ArtifactSource { get; }

    ConceptualDomain IProvider.Domain => ConceptualDomain.WorkManagement;
    IArtifactSource IProvider.ArtifactSource => ArtifactSource;
}
