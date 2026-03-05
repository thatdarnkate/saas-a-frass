using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Summaries.Common;
using Uvse.Domain.Common;
using Uvse.Domain.Datasources;
using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Commands.GenerateDatasourceSummary;

public sealed record GenerateDatasourceSummaryCommand(
    [property: Required] string RequesterId,
    Guid DatasourceId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    [property: MinLength(1)] IReadOnlyCollection<SummaryRequestedModes> RequestedModes,
    [property: Required][property: StringLength(64, MinimumLength = 1)] string LlmProvider,
    string? LlmModel = null,
    Guid? ComparisonSummaryId = null) : IRequest<SummaryResult>;

internal sealed class GenerateDatasourceSummaryCommandHandler : IRequestHandler<GenerateDatasourceSummaryCommand, SummaryResult>
{
    private static readonly TimeSpan MaxQueryWindow = TimeSpan.FromDays(31);
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IProviderRegistry _providerRegistry;
    private readonly ISummaryLlmRegistry _summaryLlmRegistry;

    public GenerateDatasourceSummaryCommandHandler(
        IApplicationDbContext dbContext,
        ITenantService tenantService,
        IUserContext userContext,
        IProviderRegistry providerRegistry,
        ISummaryLlmRegistry summaryLlmRegistry)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _providerRegistry = providerRegistry;
        _summaryLlmRegistry = summaryLlmRegistry;
    }

    public async Task<SummaryResult> Handle(GenerateDatasourceSummaryCommand request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        EnsureAuthenticatedRequester(request.RequesterId);

        if (!_userContext.IsInRole(SystemRoles.DataSourceManager))
        {
            throw new ForbiddenAccessException("Only datasource managers on the datasource allow list can generate datasource summaries.");
        }

        var tenantId = _tenantService.TenantId;
        var datasource = await _dbContext.Datasources
            .AsNoTracking()
            .Include(ds => ds.DatasourceUsers)
            .SingleOrDefaultAsync(ds => ds.Id == request.DatasourceId && ds.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Datasource was not found.");

        if (datasource.DatasourceUsers.All(datasourceUser => datasourceUser.UserId != request.RequesterId))
        {
            throw new ForbiddenAccessException("The requester is not allow-listed for the datasource.");
        }

        if (datasource.AccessScope == DatasourceAccessScope.ProjectOnly)
        {
            throw new ValidationException("ProjectOnly datasources cannot be summarized directly through the datasource summary endpoint.");
        }

        var provider = _providerRegistry.GetRequiredProvider(datasource.Type);
        var artifacts = await provider.ArtifactSource.GetArtifactsAsync(
            new ArtifactQuery(
                tenantId,
                request.RequesterId,
                request.FromUtc,
                request.ToUtc,
                SummaryDetailLevel.Detailed,
                SummaryAudienceTone.Technical),
            cancellationToken);

        if (artifacts.Count == 0)
        {
            throw new KeyNotFoundException("No accessible artifacts were found for the requested datasource and time span.");
        }

        var comparison = await ResolveComparisonSummaryAsync(request.DatasourceId, request.ComparisonSummaryId, cancellationToken);
        var modes = CombineModes(request.RequestedModes);
        var orderedArtifacts = artifacts.OrderByDescending(artifact => artifact.OccurredAtUtc).ToList();
        var title = $"Datasource Summary: {datasource.Name} ({request.FromUtc:yyyy-MM-dd} to {request.ToUtc:yyyy-MM-dd})";
        var llm = _summaryLlmRegistry.GetRequiredProvider(request.LlmProvider);
        var content = await llm.GenerateSummaryAsync(
            new SummaryLlmRequest(
                request.LlmProvider,
                request.LlmModel,
                SummaryTargetType.Datasource,
                title,
                modes,
                orderedArtifacts,
                comparison),
            cancellationToken);

        var summary = new GeneratedSummary(
            tenantId,
            datasource.Type,
            request.RequesterId,
            llm.ProviderKey,
            request.LlmModel,
            SummaryTargetType.Datasource,
            null,
            request.DatasourceId,
            comparison?.Id,
            modes,
            SummaryDetailLevel.Detailed,
            SummaryAudienceTone.Technical,
            title,
            content,
            request.FromUtc,
            request.ToUtc,
            DateTimeOffset.UtcNow);
        summary.AddBibliographyEntries(BibliographyEntryFactory.Create(summary.Id, orderedArtifacts));

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
            summary.LlmProviderKey,
            summary.LlmModel,
            summary.RequestedByUserId,
            summary.FromUtc,
            summary.ToUtc,
            summary.CreatedAtUtc,
            summary.BibliographyEntries
                .OrderBy(entry => entry.Position)
                .Select(entry => new BibliographyEntryResult(entry.Id, entry.Position, entry.Hyperlink, entry.SourceText))
                .ToArray());
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

    private static void ValidateRequest(GenerateDatasourceSummaryCommand request)
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
        Guid datasourceId,
        Guid? requestedComparisonSummaryId,
        CancellationToken cancellationToken)
    {
        if (requestedComparisonSummaryId.HasValue)
        {
            var requested = await _dbContext.GeneratedSummaries
                .AsNoTracking()
                .Include(summary => summary.BibliographyEntries)
                .SingleOrDefaultAsync(summary => summary.Id == requestedComparisonSummaryId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Comparison summary was not found.");

            if (requested.TargetType != SummaryTargetType.Datasource || requested.DatasourceId != datasourceId)
            {
                throw new ValidationException("Comparison summary does not match the requested datasource target.");
            }

            return requested;
        }

        return await _dbContext.GeneratedSummaries
            .AsNoTracking()
            .Include(summary => summary.BibliographyEntries)
            .Where(summary => summary.TargetType == SummaryTargetType.Datasource && summary.DatasourceId == datasourceId)
            .OrderByDescending(summary => summary.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
