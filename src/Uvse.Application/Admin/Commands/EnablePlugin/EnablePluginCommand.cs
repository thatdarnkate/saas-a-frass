using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Domain.Common;
using Uvse.Domain.Plugins;

namespace Uvse.Application.Admin.Commands.EnablePlugin;

public sealed record EnablePluginCommand(
    [property: Required][property: StringLength(128, MinimumLength = 1)] string ProviderKey,
    Dictionary<string, string>? Settings = null)
    : IRequest<EnablePluginResult>;

internal sealed class EnablePluginCommandHandler : IRequestHandler<EnablePluginCommand, EnablePluginResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IProviderRegistry _providerRegistry;
    private readonly IPluginSettingsEncryptor _encryptor;

    public EnablePluginCommandHandler(
        IApplicationDbContext dbContext,
        ITenantService tenantService,
        IUserContext userContext,
        IProviderRegistry providerRegistry,
        IPluginSettingsEncryptor encryptor)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _providerRegistry = providerRegistry;
        _encryptor = encryptor;
    }

    public async Task<EnablePluginResult> Handle(EnablePluginCommand request, CancellationToken cancellationToken)
    {
        // Defense-in-depth: enforce TenantAdmin at the application boundary regardless of how the handler is invoked
        if (!_userContext.IsInRole(SystemRoles.TenantAdmin))
        {
            throw new ForbiddenAccessException("Only tenant administrators can enable providers.");
        }

        var provider = _providerRegistry.GetRequiredProvider(request.ProviderKey);
        var tenantId = _tenantService.TenantId;
        var nowUtc = DateTimeOffset.UtcNow;

        var existingPlugin = await _dbContext.TenantPlugins
            .AsTracking()
            .SingleOrDefaultAsync(
                plugin => plugin.TenantId == tenantId && plugin.ProviderKey == request.ProviderKey,
                cancellationToken);

        var encryptedSettingsJson = ResolveEncryptedSettings(request.Settings, existingPlugin);

        if (existingPlugin is null)
        {
            existingPlugin = new TenantPlugin(tenantId, request.ProviderKey, provider.Domain, nowUtc, encryptedSettingsJson);
            _dbContext.TenantPlugins.Add(existingPlugin);
        }
        else
        {
            existingPlugin.Enable(nowUtc, encryptedSettingsJson);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new EnablePluginResult(tenantId, request.ProviderKey, nowUtc);
    }

    private string ResolveEncryptedSettings(Dictionary<string, string>? settings, TenantPlugin? existingPlugin)
    {
        if (settings is not null)
        {
            var settingsJson = JsonSerializer.Serialize(settings);
            return _encryptor.Encrypt(settingsJson);
        }

        if (existingPlugin is not null)
        {
            return existingPlugin.EncryptedSettingsJson;
        }

        return _encryptor.Encrypt("{}");
    }
}
