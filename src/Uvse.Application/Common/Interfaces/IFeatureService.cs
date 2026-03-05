namespace Uvse.Application.Common.Interfaces;

public interface IFeatureService
{
    Task<bool> IsEnabledAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken);
}
