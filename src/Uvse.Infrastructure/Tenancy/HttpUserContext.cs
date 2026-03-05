using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Uvse.Application.Common.Interfaces;

namespace Uvse.Infrastructure.Tenancy;

internal sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("The user identity claim is missing from the token.");

    public bool IsInRole(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        return user.IsInRole(role) || user.Claims.Any(claim => claim.Type == ClaimTypes.Role && claim.Value == role);
    }
}
