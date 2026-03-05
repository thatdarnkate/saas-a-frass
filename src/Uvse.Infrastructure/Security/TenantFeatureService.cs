using Microsoft.Extensions.Configuration;
using Uvse.Application.Common.Interfaces;

namespace Uvse.Infrastructure.Security;

/// <summary>
/// Resolves feature flags with tenant-level granularity.
/// Resolution order: Features:Tenants:{tenantId}:{featureKey} → Features:{featureKey} → false.
/// Per-tenant overrides are set in configuration (or overriding config sources such as
/// environment variables or a future database-backed provider).
/// </summary>
internal sealed class TenantFeatureService : IFeatureService
{
    private readonly IConfiguration _configuration;

    public TenantFeatureService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<bool> IsEnabledAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken)
    {
        // Per-tenant override
        var tenantValue = _configuration[$"Features:Tenants:{tenantId}:{featureKey}"];
        if (tenantValue is not null && bool.TryParse(tenantValue, out var tenantFlag))
        {
            return Task.FromResult(tenantFlag);
        }

        // Global default
        var globalFlag = _configuration.GetValue<bool>($"Features:{featureKey}");
        return Task.FromResult(globalFlag);
    }
}
