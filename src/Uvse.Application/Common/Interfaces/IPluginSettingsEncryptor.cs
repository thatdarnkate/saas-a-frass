namespace Uvse.Application.Common.Interfaces;

public interface IPluginSettingsEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
