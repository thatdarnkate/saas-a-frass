using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;
using Uvse.Domain.Projects;

namespace Uvse.Application.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    [property: Required][property: StringLength(200, MinimumLength = 1)] string Name,
    [property: MinLength(1)] IReadOnlyCollection<string> AllowedUserIds,
    [property: MinLength(1)] IReadOnlyCollection<Guid> DatasourceIds) : IRequest<ProjectResult>;

internal sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public UpdateProjectCommandHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task<ProjectResult> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        AuthorizationHelpers.EnsureProjectManager(_userContext);

        var tenantId = _tenantService.TenantId;
        var project = await _dbContext.Projects
            .AsTracking()
            .Include(p => p.ProjectUsers)
            .Include(p => p.ProjectDatasources)
            .SingleOrDefaultAsync(p => p.Id == request.ProjectId && p.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        EnsureProjectAccess(project);
        await EnsureProjectNameAvailable(project.Id, tenantId, request.Name, cancellationToken);

        var datasourceIds = request.DatasourceIds.Distinct().ToArray();
        var datasources = await _dbContext.Datasources
            .Where(ds => ds.TenantId == tenantId && datasourceIds.Contains(ds.Id))
            .ToListAsync(cancellationToken);

        if (datasources.Count != datasourceIds.Length)
        {
            throw new KeyNotFoundException("One or more datasources were not found for the tenant.");
        }

        if (datasources.Any(ds => ds.AccessScope == Domain.Datasources.DatasourceAccessScope.UserOnly))
        {
            throw new ValidationException("UserOnly datasources cannot be attached to projects.");
        }

        project.Rename(request.Name.Trim());

        _dbContext.ProjectUsers.RemoveRange(project.ProjectUsers);
        _dbContext.ProjectDatasources.RemoveRange(project.ProjectDatasources);

        var allowedUsers = request.AllowedUserIds
            .Append(_userContext.UserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var userId in allowedUsers)
        {
            project.ProjectUsers.Add(new ProjectUser(project.Id, userId));
        }

        foreach (var datasourceId in datasourceIds)
        {
            project.ProjectDatasources.Add(new ProjectDatasource(project.Id, datasourceId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ProjectResult(project.Id, project.Name, project.CreatedByUserId, project.CreatedAtUtc, allowedUsers, datasourceIds);
    }

    private void EnsureProjectAccess(Project project)
    {
        if (AuthorizationHelpers.IsTenantAdmin(_userContext))
        {
            return;
        }

        var allowed = project.ProjectUsers.Any(pu => pu.UserId == _userContext.UserId);
        if (!allowed)
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for this project.");
        }
    }

    private async Task EnsureProjectNameAvailable(Guid projectId, Guid tenantId, string name, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Projects
            .AnyAsync(p => p.TenantId == tenantId && p.Id != projectId && p.Name == name.Trim(), cancellationToken);
        if (exists)
        {
            throw new ValidationException($"A project named '{name}' already exists for the tenant.");
        }
    }
}
