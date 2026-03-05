namespace Uvse.Web.Contracts;

public sealed record EnablePluginRequest(
    string ProviderKey,
    Dictionary<string, string>? Settings = null);
