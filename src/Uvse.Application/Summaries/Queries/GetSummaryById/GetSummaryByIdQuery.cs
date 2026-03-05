using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Domain.Summaries;

namespace Uvse.Application.Summaries.Queries.GetSummaryById;

public sealed record GetSummaryByIdQuery(Guid SummaryId) : IRequest<SummaryResult>;

internal sealed class GetSummaryByIdQueryHandler : IRequestHandler<GetSummaryByIdQuery, SummaryResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;

    public GetSummaryByIdQueryHandler(IApplicationDbContext dbContext, ITenantService tenantService, IUserContext userContext)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
    }

    public async Task<SummaryResult> Handle(GetSummaryByIdQuery request, CancellationToken cancellationToken)
    {
        var summary = await _dbContext.GeneratedSummaries
            .AsNoTracking()
            .SingleOrDefaultAsync(summary => summary.Id == request.SummaryId && summary.TenantId == _tenantService.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Summary was not found.");

        switch (summary.TargetType)
        {
            case SummaryTargetType.Project:
                await EnsureProjectAccessAsync(summary.ProjectId, cancellationToken);
                break;
            case SummaryTargetType.Datasource:
                await EnsureDatasourceAccessAsync(summary, cancellationToken);
                break;
        }

        return new SummaryResult(
            summary.Id,
            summary.Title,
            summary.Content,
            summary.TargetType,
            summary.ProjectId,
            summary.DatasourceId,
            summary.RequestedModes,
            summary.RequestedByUserId,
            summary.FromUtc,
            summary.ToUtc,
            summary.CreatedAtUtc);
    }

    private async Task EnsureProjectAccessAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        if (!projectId.HasValue)
        {
            throw new KeyNotFoundException("Project summary target was not set.");
        }

        var isAllowed = await _dbContext.ProjectUsers
            .AsNoTracking()
            .AnyAsync(projectUser => projectUser.ProjectId == projectId.Value && projectUser.UserId == _userContext.UserId, cancellationToken);

        if (!isAllowed)
        {
            throw new ForbiddenAccessException("The current user is not allow-listed for the project summary.");
        }
    }

    private async Task EnsureDatasourceAccessAsync(GeneratedSummary summary, CancellationToken cancellationToken)
    {
        if (!summary.DatasourceId.HasValue)
        {
            throw new KeyNotFoundException("Datasource summary target was not set.");
        }

        var isAllowed = await _dbContext.DatasourceUsers
            .AsNoTracking()
            .AnyAsync(datasourceUser => datasourceUser.DatasourceId == summary.DatasourceId.Value && datasourceUser.UserId == _userContext.UserId, cancellationToken);

        if (!isAllowed || !string.Equals(summary.RequestedByUserId, _userContext.UserId, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException("The current user is not allowed to retrieve this datasource summary.");
        }
    }
}
