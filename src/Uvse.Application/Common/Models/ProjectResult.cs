namespace Uvse.Application.Common.Models;

public sealed record ProjectResult(
    Guid Id,
    string Name,
    string CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<string> AllowedUserIds,
    IReadOnlyCollection<Guid> DatasourceIds);
