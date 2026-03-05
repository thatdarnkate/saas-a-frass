using Uvse.Domain.Datasources;

namespace Uvse.Web.Contracts;

public sealed record UpdateDatasourceRequest(
    string Name,
    string Type,
    bool IsActive,
    DatasourceAccessScope AccessScope,
    IReadOnlyCollection<string> AllowedUserIds,
    Dictionary<string, string>? ConnectionDetails = null);
