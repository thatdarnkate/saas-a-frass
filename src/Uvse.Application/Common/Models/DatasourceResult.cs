using Uvse.Domain.Datasources;

namespace Uvse.Application.Common.Models;

public sealed record DatasourceResult(
    Guid Id,
    string Name,
    string Type,
    string CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    bool IsActive,
    DatasourceAccessScope AccessScope,
    IReadOnlyCollection<string> AllowedUserIds,
    IReadOnlyDictionary<string, string> ConnectionDetailsPreview);
