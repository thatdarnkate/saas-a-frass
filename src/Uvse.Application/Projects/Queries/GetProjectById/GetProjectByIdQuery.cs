using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;

namespace Uvse.Application.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectResult>;

internal sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public GetProjectByIdQueryHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task<ProjectResult> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.ProjectUsers)
            .Include(p => p.ProjectDatasources)
            .SingleOrDefaultAsync(p => p.Id == request.ProjectId && p.TenantId == _tenantService.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext) &&
            project.ProjectUsers.All(pu => pu.UserId != _userContext.UserId))
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for this project.");
        }

        return new ProjectResult(
            project.Id,
            project.Name,
            project.CreatedByUserId,
            project.CreatedAtUtc,
            project.ProjectUsers.Select(pu => pu.UserId).ToArray(),
            project.ProjectDatasources.Select(pd => pd.DatasourceId).ToArray());
    }
}
