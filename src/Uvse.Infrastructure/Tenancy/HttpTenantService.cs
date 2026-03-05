using Microsoft.AspNetCore.Http;
using Uvse.Application.Common.Interfaces;

namespace Uvse.Infrastructure.Tenancy;

/// <summary>
/// Scoped tenant context. In HTTP request scope it resolves the tenant from the validated JWT claim.
/// Background jobs that need tenant-scoped DB access must call SetTenantId before using any
/// tenant-dependent service within their scope.
/// </summary>
internal sealed class TenantContext : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _explicitTenantId;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId => _explicitTenantId ?? ResolveFromHttpContext();

    /// <summary>
    /// Sets the tenant for this scope. Intended for use by background jobs and test harnesses.
    /// Must be called before any tenant-scoped service is accessed within the scope.
    /// </summary>
    public void SetTenantId(Guid tenantId) => _explicitTenantId = tenantId;

    private Guid ResolveFromHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "TenantId cannot be resolved: no HTTP request context is active and SetTenantId was not called. " +
                "Background jobs must call SetTenantId on this scoped TenantContext before accessing tenant-scoped services.");

        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value
            ?? throw new UnauthorizedAccessException("The 'tenant_id' claim is missing from the token.");

        return Guid.TryParse(tenantClaim, out var tenantId)
            ? tenantId
            : throw new UnauthorizedAccessException($"The 'tenant_id' claim value '{tenantClaim}' is not a valid GUID.");
    }
}
