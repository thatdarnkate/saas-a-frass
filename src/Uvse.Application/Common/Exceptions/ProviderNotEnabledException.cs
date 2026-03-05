namespace Uvse.Application.Common.Exceptions;

public sealed class ProviderNotEnabledException : Exception
{
    public ProviderNotEnabledException(string providerKey)
        : base($"Provider '{providerKey}' is not enabled for this tenant.")
    {
    }
}
