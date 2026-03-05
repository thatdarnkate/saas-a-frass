using System.ComponentModel.DataAnnotations;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Application.Summaries.Common;
using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Queries.GenerateProviderSummary;

public sealed record GenerateProviderSummaryQuery(
    [property: Required][property: StringLength(128, MinimumLength = 1)] string ProviderKey,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    SummaryDetailLevel DetailLevel,
    SummaryAudienceTone AudienceTone,
    [property: Required][property: StringLength(64, MinimumLength = 1)] string LlmProvider,
    string? LlmModel = null) : IRequest<ProviderSummaryResult>;

internal sealed class GenerateProviderSummaryQueryHandler : IRequestHandler<GenerateProviderSummaryQuery, ProviderSummaryResult>
{
    private static readonly string[] SuperscriptDigits = ["⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹"];
    private static readonly TimeSpan MaxQueryWindow = TimeSpan.FromDays(31);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IProviderRegistry _providerRegistry;
    private readonly IFeatureService _featureService;
    private readonly ISummaryLlmRegistry _summaryLlmRegistry;

    public GenerateProviderSummaryQueryHandler(
        IApplicationDbContext dbContext,
        ITenantService tenantService,
        IUserContext userContext,
        IProviderRegistry providerRegistry,
        IFeatureService featureService,
        ISummaryLlmRegistry summaryLlmRegistry)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _providerRegistry = providerRegistry;
        _featureService = featureService;
        _summaryLlmRegistry = summaryLlmRegistry;
    }

    public async Task<ProviderSummaryResult> Handle(GenerateProviderSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.ToUtc <= request.FromUtc)
        {
            throw new ValidationException("ToUtc must be after FromUtc.");
        }

        if (request.ToUtc - request.FromUtc > MaxQueryWindow)
        {
            throw new ValidationException(
                $"The requested date range exceeds the maximum allowed window of {MaxQueryWindow.Days} days.");
        }

        var tenantId = _tenantService.TenantId;
        var userId = _userContext.UserId;

        var isFeatureEnabled = await _featureService.IsEnabledAsync(tenantId, "provider-summary", cancellationToken);
        if (!isFeatureEnabled)
        {
            throw new FeatureNotEnabledException("provider-summary");
        }

        var provider = _providerRegistry.GetRequiredProvider(request.ProviderKey);

        var providerIsEnabled = await _dbContext.TenantPlugins
            .Where(plugin => plugin.ProviderKey == request.ProviderKey && plugin.Enabled)
            .Select(plugin => plugin.Id)
            .AnyAsync(cancellationToken);

        if (!providerIsEnabled)
        {
            throw new ProviderNotEnabledException(request.ProviderKey);
        }

        var existing = await _dbContext.GeneratedSummaries
            .Include(summary => summary.BibliographyEntries)
            .FirstOrDefaultAsync(
                s => s.TargetType == SummaryTargetType.Provider
                     && s.ProviderKey == request.ProviderKey
                     && s.RequestedByUserId == userId
                     && s.DetailLevel == request.DetailLevel
                     && s.AudienceTone == request.AudienceTone
                     && s.FromUtc == request.FromUtc
                     && s.ToUtc == request.ToUtc,
                cancellationToken);

        if (existing is not null)
        {
            return new ProviderSummaryResult(
                existing.Id,
                existing.Title,
                existing.Content,
                existing.BibliographyEntries
                    .OrderBy(entry => entry.Position)
                    .Select(entry => new BibliographyEntryResult(entry.Id, entry.Position, entry.Hyperlink, entry.SourceText))
                    .ToArray());
        }

        var artifacts = await provider.ArtifactSource.GetArtifactsAsync(
            new ArtifactQuery(
                tenantId,
                userId,
                request.FromUtc,
                request.ToUtc,
                request.DetailLevel,
                request.AudienceTone),
            cancellationToken);

        if (artifacts.Count == 0)
        {
            throw new KeyNotFoundException("No accessible artifacts were found for the requested window.");
        }

        var orderedArtifacts = artifacts
            .OrderByDescending(artifact => artifact.OccurredAtUtc)
            .ToList();

        var title = $"Provider Summary ({request.FromUtc:yyyy-MM-dd} to {request.ToUtc:yyyy-MM-dd})";
        var llm = _summaryLlmRegistry.GetRequiredProvider(request.LlmProvider);
        var content = await llm.GenerateSummaryAsync(
            new SummaryLlmRequest(
                request.LlmProvider,
                request.LlmModel,
                SummaryTargetType.Provider,
                title,
                request.DetailLevel == SummaryDetailLevel.Executive
                    ? SummaryRequestedModes.Executive
                    : SummaryRequestedModes.Detailed,
                orderedArtifacts,
                null),
            cancellationToken);
        var summary = new GeneratedSummary(
            tenantId,
            request.ProviderKey,
            userId,
            llm.ProviderKey,
            request.LlmModel,
            SummaryTargetType.Provider,
            null,
            null,
            null,
            request.DetailLevel == SummaryDetailLevel.Executive
                ? SummaryRequestedModes.Executive
                : SummaryRequestedModes.Detailed,
            request.DetailLevel,
            request.AudienceTone,
            title,
            content,
            request.FromUtc,
            request.ToUtc,
            DateTimeOffset.UtcNow);
        summary.AddBibliographyEntries(BibliographyEntryFactory.Create(summary.Id, orderedArtifacts));

        _dbContext.GeneratedSummaries.Add(summary);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProviderSummaryResult(
            summary.Id,
            summary.Title,
            summary.Content,
            summary.BibliographyEntries
                .OrderBy(entry => entry.Position)
                .Select(entry => new BibliographyEntryResult(entry.Id, entry.Position, entry.Hyperlink, entry.SourceText))
                .ToArray());
    }
}
