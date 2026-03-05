namespace Uvse.Application.Common.Exceptions;

public sealed class FeatureNotEnabledException : Exception
{
    public FeatureNotEnabledException(string featureKey)
        : base($"The '{featureKey}' feature is not enabled for this tenant.")
    {
    }
}
