using System.ComponentModel.DataAnnotations;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Common.Interfaces;
using Uvse.Application.Common.Models;
using Uvse.Domain.Summaries;
using Uvse.Domain.Synthesis;

namespace Uvse.Application.Summaries.Queries.GenerateProviderSummary;

public sealed record GenerateProviderSummaryQuery(
    [property: Required][property: StringLength(128, MinimumLength = 1)] string ProviderKey,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    SummaryDetailLevel DetailLevel,
    SummaryAudienceTone AudienceTone) : IRequest<ProviderSummaryResult>;

internal sealed class GenerateProviderSummaryQueryHandler : IRequestHandler<GenerateProviderSummaryQuery, ProviderSummaryResult>
{
    private static readonly string[] SuperscriptDigits = ["⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹"];
    private static readonly TimeSpan MaxQueryWindow = TimeSpan.FromDays(31);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IUserContext _userContext;
    private readonly IProviderRegistry _providerRegistry;
    private readonly IFeatureService _featureService;

    public GenerateProviderSummaryQueryHandler(
        IApplicationDbContext dbContext,
        ITenantService tenantService,
        IUserContext userContext,
        IProviderRegistry providerRegistry,
        IFeatureService featureService)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _userContext = userContext;
        _providerRegistry = providerRegistry;
        _featureService = featureService;
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
            return new ProviderSummaryResult(existing.Id, existing.Title, existing.Content);
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
        var content = BuildSummary(title, request.DetailLevel, request.AudienceTone, orderedArtifacts);
        var summary = new GeneratedSummary(
            tenantId,
            request.ProviderKey,
            userId,
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

        _dbContext.GeneratedSummaries.Add(summary);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProviderSummaryResult(summary.Id, summary.Title, summary.Content);
    }

    private static string BuildSummary(
        string title,
        SummaryDetailLevel detailLevel,
        SummaryAudienceTone audienceTone,
        IReadOnlyList<ArtifactRecord> artifacts)
    {
        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine();
        builder.AppendLine(audienceTone == SummaryAudienceTone.Technical
            ? "Audience: Technical"
            : "Audience: Non-Technical");
        builder.AppendLine(detailLevel == SummaryDetailLevel.Executive
            ? "Detail Level: Executive"
            : "Detail Level: Detailed");
        builder.AppendLine();
        builder.AppendLine("Highlights");

        for (var index = 0; index < artifacts.Count; index++)
        {
            var artifact = artifacts[index];
            var citation = ToSuperscript(index + 1);
            var detail = detailLevel == SummaryDetailLevel.Executive
                ? artifact.Title
                : $"{artifact.Title}: {artifact.Summary}";
            builder.AppendLine($"- {detail}{citation}");
        }

        builder.AppendLine();
        builder.AppendLine("Bibliography");

        for (var index = 0; index < artifacts.Count; index++)
        {
            var artifact = artifacts[index];
            builder.AppendLine(
                $"[{index + 1}] {artifact.Title} ({artifact.OccurredAtUtc:yyyy-MM-dd}) - {artifact.SourceUrl}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string ToSuperscript(int number) =>
        string.Concat(number.ToString().Select(digit => SuperscriptDigits[digit - '0']));
}
