namespace Uvse.Application.Common.Interfaces;

public interface ISummaryLlmRegistry
{
    ISummaryLlmProvider GetRequiredProvider(string providerKey);
}
