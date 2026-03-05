using Microsoft.AspNetCore.DataProtection;
using Uvse.Application.Common.Interfaces;

namespace Uvse.Infrastructure.Security;

internal sealed class DataProtectionPluginSettingsEncryptor : IPluginSettingsEncryptor
{
    private readonly IDataProtector _protector;

    public DataProtectionPluginSettingsEncryptor(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("Uvse.PluginSettings.v1");
    }

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);
}
