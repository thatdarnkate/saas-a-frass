namespace Uvse.Application.Common.Models;

public sealed record EnablePluginResult(Guid TenantId, string ProviderKey, DateTimeOffset EnabledAtUtc);
