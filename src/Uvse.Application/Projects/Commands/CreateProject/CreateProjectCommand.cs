using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;
using Uvse.Domain.Projects;

namespace Uvse.Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    [property: Required][property: StringLength(200, MinimumLength = 1)] string Name,
    [property: MinLength(1)] IReadOnlyCollection<string> AllowedUserIds,
    [property: MinLength(1)] IReadOnlyCollection<Guid> DatasourceIds) : IRequest<ProjectResult>;

internal sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public CreateProjectCommandHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task<ProjectResult> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        AuthorizationHelpers.EnsureProjectManager(_userContext);

        var tenantId = _tenantService.TenantId;
        var normalizedUsers = NormalizeUsers(request.AllowedUserIds, _userContext.UserId);
        var datasourceIds = request.DatasourceIds.Distinct().ToArray();

        await EnsureProjectNameAvailable(tenantId, request.Name, cancellationToken);
        var datasources = await LoadAttachableDatasourcesAsync(tenantId, datasourceIds, cancellationToken);

        var project = new Project(tenantId, request.Name.Trim(), _userContext.UserId, DateTimeOffset.UtcNow);
        foreach (var userId in normalizedUsers)
        {
            project.ProjectUsers.Add(new ProjectUser(project.Id, userId));
        }

        foreach (var datasource in datasources)
        {
            project.ProjectDatasources.Add(new ProjectDatasource(project.Id, datasource.Id));
        }

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectResult(project.Id, project.Name, project.CreatedByUserId, project.CreatedAtUtc, normalizedUsers, datasourceIds);
    }

    private async Task EnsureProjectNameAvailable(Guid tenantId, string name, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Projects.AnyAsync(p => p.TenantId == tenantId && p.Name == name.Trim(), cancellationToken);
        if (exists)
        {
            throw new ValidationException($"A project named '{name}' already exists for the tenant.");
        }
    }

    private async Task<IReadOnlyCollection<Domain.Datasources.Datasource>> LoadAttachableDatasourcesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> datasourceIds,
        CancellationToken cancellationToken)
    {
        var datasources = await _dbContext.Datasources
            .Where(ds => ds.TenantId == tenantId && datasourceIds.Contains(ds.Id))
            .ToListAsync(cancellationToken);

        if (datasources.Count != datasourceIds.Count)
        {
            throw new KeyNotFoundException("One or more datasources were not found for the tenant.");
        }

        if (datasources.Any(ds => ds.AccessScope == Domain.Datasources.DatasourceAccessScope.UserOnly))
        {
            throw new ValidationException("UserOnly datasources cannot be attached to projects.");
        }

        return datasources;
    }

    private static IReadOnlyCollection<string> NormalizeUsers(IEnumerable<string> allowedUserIds, string creatorUserId) =>
        allowedUserIds
            .Append(creatorUserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
