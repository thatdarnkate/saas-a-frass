using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Domain.Common;

namespace Uvse.Application.Common.Security;

internal static class AuthorizationHelpers
{
    public static bool IsTenantAdmin(IUserContext userContext) => userContext.IsInRole(SystemRoles.TenantAdmin);

    public static void EnsureProjectManager(IUserContext userContext)
    {
        if (!userContext.IsInRole(SystemRoles.ProjectManager) && !userContext.IsInRole(SystemRoles.TenantAdmin))
        {
            throw new ForbiddenAccessException("The current user is not allowed to manage projects.");
        }
    }

    public static void EnsureDatasourceManager(IUserContext userContext)
    {
        if (!userContext.IsInRole(SystemRoles.DataSourceManager) && !userContext.IsInRole(SystemRoles.TenantAdmin))
        {
            throw new ForbiddenAccessException("The current user is not allowed to manage datasources.");
        }
    }
}
