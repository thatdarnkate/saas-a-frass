using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;

namespace Uvse.Application.Projects.Queries.ListProjects;

public sealed record ListProjectsQuery() : IRequest<IReadOnlyCollection<ProjectResult>>;

internal sealed class ListProjectsQueryHandler : IRequestHandler<ListProjectsQuery, IReadOnlyCollection<ProjectResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public ListProjectsQueryHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task<IReadOnlyCollection<ProjectResult>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.ProjectUsers)
            .Include(p => p.ProjectDatasources)
            .Where(p => p.TenantId == _tenantService.TenantId)
            .ToListAsync(cancellationToken);

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext))
        {
            projects = projects
                .Where(project => project.ProjectUsers.Any(projectUser => projectUser.UserId == _userContext.UserId))
                .ToList();
        }

        return projects
            .OrderBy(project => project.Name)
            .Select(project => new ProjectResult(
                project.Id,
                project.Name,
                project.CreatedByUserId,
                project.CreatedAtUtc,
                project.ProjectUsers.Select(projectUser => projectUser.UserId).ToArray(),
                project.ProjectDatasources.Select(projectDatasource => projectDatasource.DatasourceId).ToArray()))
            .ToArray();
    }
}
