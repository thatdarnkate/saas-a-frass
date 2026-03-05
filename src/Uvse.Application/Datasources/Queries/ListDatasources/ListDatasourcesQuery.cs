using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;

namespace Uvse.Application.Datasources.Queries.ListDatasources;

public sealed record ListDatasourcesQuery() : IRequest<IReadOnlyCollection<DatasourceResult>>;

internal sealed class ListDatasourcesQueryHandler : IRequestHandler<ListDatasourcesQuery, IReadOnlyCollection<DatasourceResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IPluginSettingsEncryptor _encryptor;

    public ListDatasourcesQueryHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext, IPluginSettingsEncryptor encryptor)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _encryptor = encryptor;
    }

    public async Task<IReadOnlyCollection<DatasourceResult>> Handle(ListDatasourcesQuery request, CancellationToken cancellationToken)
    {
        var datasources = await _dbContext.Datasources
            .AsNoTracking()
            .Include(ds => ds.DatasourceUsers)
            .Where(ds => ds.TenantId == _tenantService.TenantId)
            .ToListAsync(cancellationToken);

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext))
        {
            datasources = datasources
                .Where(datasource => datasource.DatasourceUsers.Any(datasourceUser => datasourceUser.UserId == _userContext.UserId))
                .ToList();
        }

        return datasources
            .OrderBy(datasource => datasource.Name)
            .Select(datasource => new DatasourceResult(
                datasource.Id,
                datasource.Name,
                datasource.Type,
                datasource.CreatedByUserId,
                datasource.CreatedAtUtc,
                datasource.IsActive,
                datasource.AccessScope,
                datasource.DatasourceUsers.Select(datasourceUser => datasourceUser.UserId).ToArray(),
                ConnectionDetailsMasker.ToPreview(_encryptor.Decrypt(datasource.ConnectionDetailsEncryptedJson))))
            .ToArray();
    }
}
