using System.Text.Json;

namespace Uvse.Application.Common.Security;

internal static class ConnectionDetailsMasker
{
    public static IReadOnlyDictionary<string, string> ToPreview(string decryptedJson)
    {
        var details = JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedJson) ?? [];
        return details.ToDictionary(kvp => kvp.Key, kvp => Mask(kvp.Value), StringComparer.OrdinalIgnoreCase);
    }

    public static string Serialize(Dictionary<string, string>? details) =>
        JsonSerializer.Serialize(details ?? new Dictionary<string, string>());

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        return $"{value[..2]}***{value[^2..]}";
    }
}
