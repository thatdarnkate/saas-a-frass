using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Common.Security;
using Uvse.Domain.Datasources;

namespace Uvse.Application.Datasources.Commands.CreateDatasource;

public sealed record CreateDatasourceCommand(
    [property: Required][property: StringLength(200, MinimumLength = 1)] string Name,
    [property: Required][property: StringLength(100, MinimumLength = 1)] string Type,
    bool IsActive,
    DatasourceAccessScope AccessScope,
    [property: MinLength(1)] IReadOnlyCollection<string> AllowedUserIds,
    [property: Required] Dictionary<string, string> ConnectionDetails) : IRequest<DatasourceResult>;

internal sealed class CreateDatasourceCommandHandler : IRequestHandler<CreateDatasourceCommand, DatasourceResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IPluginSettingsEncryptor _encryptor;

    public CreateDatasourceCommandHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext, IPluginSettingsEncryptor encryptor)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _encryptor = encryptor;
    }

    public async Task<DatasourceResult> Handle(CreateDatasourceCommand request, CancellationToken cancellationToken)
    {
        AuthorizationHelpers.EnsureDatasourceManager(_userContext);

        var tenantId = _tenantService.TenantId;
        var name = request.Name.Trim();
        var exists = await _dbContext.Datasources.AnyAsync(ds => ds.TenantId == tenantId && ds.Name == name, cancellationToken);
        if (exists)
        {
            throw new ValidationException($"A datasource named '{name}' already exists for the tenant.");
        }

        var allowedUsers = request.AllowedUserIds
            .Append(_userContext.UserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var serialized = ConnectionDetailsMasker.Serialize(request.ConnectionDetails);
        var encrypted = _encryptor.Encrypt(serialized);

        var datasource = new Datasource(
            tenantId,
            name,
            request.Type.Trim(),
            _userContext.UserId,
            DateTimeOffset.UtcNow,
            request.IsActive,
            request.AccessScope,
            encrypted);

        foreach (var userId in allowedUsers)
        {
            datasource.DatasourceUsers.Add(new DatasourceUser(datasource.Id, userId));
        }

        _dbContext.Datasources.Add(datasource);
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
            ConnectionDetailsMasker.ToPreview(serialized));
    }
}
