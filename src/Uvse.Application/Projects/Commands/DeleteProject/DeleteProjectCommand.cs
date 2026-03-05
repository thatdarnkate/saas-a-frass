using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Security;

namespace Uvse.Application.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest;

internal sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public DeleteProjectCommandHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        AuthorizationHelpers.EnsureProjectManager(_userContext);

        var project = await _dbContext.Projects
            .AsTracking()
            .Include(p => p.ProjectUsers)
            .Include(p => p.ProjectDatasources)
            .SingleOrDefaultAsync(p => p.Id == request.ProjectId && p.TenantId == _tenantService.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext) &&
            project.ProjectUsers.All(pu => pu.UserId != _userContext.UserId))
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for this project.");
        }

        _dbContext.ProjectUsers.RemoveRange(project.ProjectUsers);
        _dbContext.ProjectDatasources.RemoveRange(project.ProjectDatasources);
        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
