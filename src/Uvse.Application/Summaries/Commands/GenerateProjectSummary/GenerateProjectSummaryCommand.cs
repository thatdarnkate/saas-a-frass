using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Summaries.Common;
using Uvse.Domain.Common;
using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Commands.GenerateProjectSummary;

public sealed record GenerateProjectSummaryCommand(
    [property: Required] string RequesterId,
    Guid ProjectId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    [property: MinLength(1)] IReadOnlyCollection<SummaryRequestedModes> RequestedModes,
    Guid? ComparisonSummaryId = null) : IRequest<SummaryResult>;

internal sealed class GenerateProjectSummaryCommandHandler : IRequestHandler<GenerateProjectSummaryCommand, SummaryResult>
{
    private static readonly TimeSpan MaxQueryWindow = TimeSpan.FromDays(31);
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IProviderRegistry _providerRegistry;

    public GenerateProjectSummaryCommandHandler(
        IApplicationDbContext dbContext,
        ITenantService tenantService,
        IUserContext userContext,
        IProviderRegistry providerRegistry)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _providerRegistry = providerRegistry;
    }

    public async Task<SummaryResult> Handle(GenerateProjectSummaryCommand request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        EnsureAuthenticatedRequester(request.RequesterId);

        if (!_userContext.IsInRole(SystemRoles.ProjectManager))
        {
            throw new ForbiddenAccessException("Only project managers on the project allow list can generate project summaries.");
        }

        var tenantId = _tenantService.TenantId;
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.ProjectUsers)
            .Include(p => p.ProjectDatasources)
            .SingleOrDefaultAsync(p => p.Id == request.ProjectId && p.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        if (project.ProjectUsers.All(projectUser => projectUser.UserId != request.RequesterId))
        {
            throw new ForbiddenAccessException("The requester is not allow-listed for the project.");
        }

        var datasourceIds = project.ProjectDatasources.Select(pd => pd.DatasourceId).Distinct().ToArray();
        var datasources = await _dbContext.Datasources
            .AsNoTracking()
            .Include(ds => ds.DatasourceUsers)
            .Where(ds => ds.TenantId == tenantId && datasourceIds.Contains(ds.Id))
            .ToListAsync(cancellationToken);

        var artifacts = new List<ArtifactRecord>();
        foreach (var datasource in datasources)
        {
            var provider = _providerRegistry.GetRequiredProvider(datasource.Type);
            var sourceArtifacts = await provider.ArtifactSource.GetArtifactsAsync(
                new ArtifactQuery(
                    tenantId,
                    request.RequesterId,
                    request.FromUtc,
                    request.ToUtc,
                    SummaryDetailLevel.Detailed,
                    SummaryAudienceTone.Technical),
                cancellationToken);
            artifacts.AddRange(sourceArtifacts);
        }

        if (artifacts.Count == 0)
        {
            throw new KeyNotFoundException("No accessible artifacts were found for the requested project and time span.");
        }

        var comparison = await ResolveComparisonSummaryAsync(
            SummaryTargetType.Project,
            request.ProjectId,
            null,
            request.ComparisonSummaryId,
            cancellationToken);

        var modes = CombineModes(request.RequestedModes);
        var orderedArtifacts = artifacts.OrderByDescending(artifact => artifact.OccurredAtUtc).ToList();
        var title = $"Project Summary: {project.Name} ({request.FromUtc:yyyy-MM-dd} to {request.ToUtc:yyyy-MM-dd})";
        var content = SummaryComposer.Compose(title, modes, orderedArtifacts, comparison);

        var summary = new GeneratedSummary(
            tenantId,
            "project",
            request.RequesterId,
            SummaryTargetType.Project,
            request.ProjectId,
            null,
            comparison?.Id,
            modes,
            SummaryDetailLevel.Detailed,
            SummaryAudienceTone.Technical,
            title,
            content,
            request.FromUtc,
            request.ToUtc,
            DateTimeOffset.UtcNow);

        _dbContext.GeneratedSummaries.Add(summary);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

    private static SummaryRequestedModes CombineModes(IEnumerable<SummaryRequestedModes> modes)
    {
        var combined = modes.Aggregate(SummaryRequestedModes.None, (current, mode) => current | mode);
        if (combined == SummaryRequestedModes.None)
        {
            throw new ValidationException("At least one summary mode must be requested.");
        }

        return combined;
    }

    private void EnsureAuthenticatedRequester(string requesterId)
    {
        if (!string.Equals(requesterId, _userContext.UserId, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException("The requester ID must match the authenticated user.");
        }
    }

    private static void ValidateRequest(GenerateProjectSummaryCommand request)
    {
        if (request.ToUtc <= request.FromUtc)
        {
            throw new ValidationException("ToUtc must be after FromUtc.");
        }

        if (request.ToUtc - request.FromUtc > MaxQueryWindow)
        {
            throw new ValidationException($"The requested date range exceeds the maximum allowed window of {MaxQueryWindow.Days} days.");
        }
    }

    private async Task<GeneratedSummary?> ResolveComparisonSummaryAsync(
        SummaryTargetType targetType,
        Guid? projectId,
        Guid? datasourceId,
        Guid? requestedComparisonSummaryId,
        CancellationToken cancellationToken)
    {
        if (requestedComparisonSummaryId.HasValue)
        {
            var requested = await _dbContext.GeneratedSummaries
                .AsNoTracking()
                .SingleOrDefaultAsync(summary => summary.Id == requestedComparisonSummaryId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Comparison summary was not found.");

            if (requested.TargetType != targetType || requested.ProjectId != projectId || requested.DatasourceId != datasourceId)
            {
                throw new ValidationException("Comparison summary does not match the requested project target.");
            }

            return requested;
        }

        return await _dbContext.GeneratedSummaries
            .AsNoTracking()
            .Where(summary => summary.TargetType == targetType && summary.ProjectId == projectId && summary.DatasourceId == datasourceId)
            .OrderByDescending(summary => summary.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
