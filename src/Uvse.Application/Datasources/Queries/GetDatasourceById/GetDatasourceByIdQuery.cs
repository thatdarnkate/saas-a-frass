using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;

namespace Uvse.Application.Datasources.Queries.GetDatasourceById;

public sealed record GetDatasourceByIdQuery(Guid DatasourceId) : IRequest<DatasourceResult>;

internal sealed class GetDatasourceByIdQueryHandler : IRequestHandler<GetDatasourceByIdQuery, DatasourceResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IPluginSettingsEncryptor _encryptor;

    public GetDatasourceByIdQueryHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext, IPluginSettingsEncryptor encryptor)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _encryptor = encryptor;
    }

    public async Task<DatasourceResult> Handle(GetDatasourceByIdQuery request, CancellationToken cancellationToken)
    {
        var datasource = await _dbContext.Datasources
            .AsNoTracking()
            .Include(ds => ds.DatasourceUsers)
            .SingleOrDefaultAsync(ds => ds.Id == request.DatasourceId && ds.TenantId == _tenantService.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Datasource was not found.");

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext) &&
            datasource.DatasourceUsers.All(du => du.UserId != _userContext.UserId))
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for this datasource.");
        }

        return new DatasourceResult(
            datasource.Id,
            datasource.Name,
            datasource.Type,
            datasource.CreatedByUserId,
            datasource.CreatedAtUtc,
            datasource.IsActive,
            datasource.AccessScope,
            datasource.DatasourceUsers.Select(du => du.UserId).ToArray(),
            ConnectionDetailsMasker.ToPreview(_encryptor.Decrypt(datasource.ConnectionDetailsEncryptedJson)));
    }
}
