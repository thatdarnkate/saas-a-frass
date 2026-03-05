namespace Uvse.Web.Contracts;

public sealed record UpdateProjectRequest(
    string Name,
    IReadOnlyCollection<string> AllowedUserIds,
    IReadOnlyCollection<Guid> DatasourceIds);
