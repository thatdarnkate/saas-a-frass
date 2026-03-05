using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;
using Uvse.Domain.Datasources;

namespace Uvse.Application.Datasources.Commands.UpdateDatasource;

public sealed record UpdateDatasourceCommand(
    Guid DatasourceId,
    [property: Required][property: StringLength(200, MinimumLength = 1)] string Name,
    [property: Required][property: StringLength(100, MinimumLength = 1)] string Type,
    bool IsActive,
    DatasourceAccessScope AccessScope,
    [property: MinLength(1)] IReadOnlyCollection<string> AllowedUserIds,
    Dictionary<string, string>? ConnectionDetails = null) : IRequest<DatasourceResult>;

internal sealed class UpdateDatasourceCommandHandler : IRequestHandler<UpdateDatasourceCommand, DatasourceResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IPluginSettingsEncryptor _encryptor;

    public UpdateDatasourceCommandHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext, IPluginSettingsEncryptor encryptor)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _encryptor = encryptor;
    }

    public async Task<DatasourceResult> Handle(UpdateDatasourceCommand request, CancellationToken cancellationToken)
    {
        AuthorizationHelpers.EnsureDatasourceManager(_userContext);

        var tenantId = _tenantService.TenantId;
        var datasource = await _dbContext.Datasources
            .AsTracking()
            .Include(ds => ds.DatasourceUsers)
            .Include(ds => ds.ProjectDatasources)
            .SingleOrDefaultAsync(ds => ds.Id == request.DatasourceId && ds.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Datasource was not found.");

        if (!AuthorizationHelpers.IsTenantAdmin(_userContext) &&
            datasource.DatasourceUsers.All(du => du.UserId != _userContext.UserId))
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for this datasource.");
        }

        var name = request.Name.Trim();
        var exists = await _dbContext.Datasources
            .AnyAsync(ds => ds.TenantId == tenantId && ds.Id != datasource.Id && ds.Name == name, cancellationToken);
        if (exists)
        {
            throw new ValidationException($"A datasource named '{name}' already exists for the tenant.");
        }

        if (request.AccessScope == DatasourceAccessScope.UserOnly && datasource.ProjectDatasources.Count > 0)
        {
            throw new ValidationException("A datasource already linked to projects cannot be changed to UserOnly.");
        }

        var decrypted = request.ConnectionDetails is null
            ? _encryptor.Decrypt(datasource.ConnectionDetailsEncryptedJson)
            : ConnectionDetailsMasker.Serialize(request.ConnectionDetails);
        var encrypted = request.ConnectionDetails is null
            ? datasource.ConnectionDetailsEncryptedJson
            : _encryptor.Encrypt(decrypted);

        datasource.Update(name, request.Type.Trim(), request.IsActive, request.AccessScope, encrypted);

        _dbContext.DatasourceUsers.RemoveRange(datasource.DatasourceUsers);
        var allowedUsers = request.AllowedUserIds
            .Append(_userContext.UserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var userId in allowedUsers)
        {
            datasource.DatasourceUsers.Add(new DatasourceUser(datasource.Id, userId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new DatasourceResult(
            datasource.Id,
            datasource.Name,
            datasource.Type,
            datasource.CreatedByUserId,
            datasource.CreatedAtUtc,
            datasource.IsActive,
            datasource.AccessScope,
            allowedUsers,
            ConnectionDetailsMasker.ToPreview(decrypted));
    }
}
