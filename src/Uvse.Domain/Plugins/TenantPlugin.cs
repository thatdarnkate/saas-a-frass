using Uvse.Domain.Common;

namespace Uvse.Domain.Plugins;

public sealed class TenantPlugin : ITenantOwned
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string ProviderKey { get; private set; } = string.Empty;
    public ConceptualDomain Domain { get; private set; }
    public bool Enabled { get; private set; }
    public string EncryptedSettingsJson { get; private set; } = "{}";
    public DateTimeOffset EnabledAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private TenantPlugin()
    {
    }

    public TenantPlugin(Guid tenantId, string providerKey, ConceptualDomain domain, DateTimeOffset nowUtc, string encryptedSettingsJson = "{}")
    {
        TenantId = tenantId;
        ProviderKey = providerKey;
        Domain = domain;
        Enabled = true;
        EncryptedSettingsJson = encryptedSettingsJson;
        EnabledAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Enable(DateTimeOffset nowUtc, string encryptedSettingsJson = "{}")
    {
        Enabled = true;
        EncryptedSettingsJson = encryptedSettingsJson;
        UpdatedAtUtc = nowUtc;
    }

    public void Disable(DateTimeOffset nowUtc)
    {
        Enabled = false;
        UpdatedAtUtc = nowUtc;
    }
}
