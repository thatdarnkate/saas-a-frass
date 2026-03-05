using Uvse.Domain.Datasources;

namespace Uvse.Web.Contracts;

public sealed record CreateDatasourceRequest(
    string Name,
    string Type,
    bool IsActive,
    DatasourceAccessScope AccessScope,
    IReadOnlyCollection<string> AllowedUserIds,
    Dictionary<string, string> ConnectionDetails);
