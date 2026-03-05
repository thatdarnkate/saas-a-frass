using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Security;

namespace Uvse.Application.Datasources.Commands.DeleteDatasource;

public sealed record DeleteDatasourceCommand(Guid DatasourceId) : IRequest;

internal sealed class DeleteDatasourceCommandHandler : IRequestHandler<DeleteDatasourceCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public DeleteDatasourceCommandHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task Handle(DeleteDatasourceCommand request, CancellationToken cancellationToken)
    {
        AuthorizationHelpers.EnsureDatasourceManager(_userContext);

        var datasource = await _dbContext.Datasources
            .AsTracking()
            .Include(ds => ds.DatasourceUsers)
            .Include(ds => ds.ProjectDatasources)
            .SingleOrDefaultAsync(ds => ds.Id == request.DatasourceId && ds.TenantId == _tenantService.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Datasource was not found.");

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext) &&
            datasource.DatasourceUsers.All(du => du.UserId != _userContext.UserId))
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for this datasource.");
        }

        if (datasource.ProjectDatasources.Count > 0)
        {
            throw new ValidationException("Datasource cannot be deleted while it is attached to one or more projects.");
        }

        _dbContext.DatasourceUsers.RemoveRange(datasource.DatasourceUsers);
        _dbContext.Datasources.Remove(datasource);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
