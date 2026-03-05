namespace Uvse.Web.Contracts;

public sealed record CreateProjectRequest(
    string Name,
    IReadOnlyCollection<string> AllowedUserIds,
    IReadOnlyCollection<Guid> DatasourceIds);
