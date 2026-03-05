using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Uvse.Application;
using Uvse.Application.Common.Exceptions;
using Uvse.Application.Admin.Commands.EnablePlugin;
using Uvse.Application.Datasources.Commands.CreateDatasource;
using Uvse.Application.Datasources.Commands.DeleteDatasource;
using Uvse.Application.Datasources.Commands.UpdateDatasource;
using Uvse.Application.Datasources.Queries.GetDatasourceById;
using Uvse.Application.Datasources.Queries.ListDatasources;
using Uvse.Application.Projects.Commands.CreateProject;
using Uvse.Application.Projects.Commands.DeleteProject;
using Uvse.Application.Projects.Commands.UpdateProject;
using Uvse.Application.Projects.Queries.GetProjectById;
using Uvse.Application.Projects.Queries.ListProjects;
using Uvse.Application.Summaries.Commands.GenerateDatasourceSummary;
using Uvse.Application.Summaries.Commands.GenerateProjectSummary;
using Uvse.Application.Summaries.Queries.GetSummaryById;
using Uvse.Application.Summaries.Queries.GenerateProviderSummary;
using Uvse.Domain.Common;
using Uvse.Infrastructure;
using Uvse.Web.Contracts;
using Uvse.Web.Middleware;
using Uvse.Web.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton<RequestMetrics>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SystemRoles.TenantAdmin, policy => policy.RequireRole(SystemRoles.TenantAdmin));
    options.AddPolicy(ApiPolicies.ProjectRead, policy => policy.RequireRole(SystemRoles.StandardUser, SystemRoles.ProjectManager, SystemRoles.TenantAdmin));
    options.AddPolicy(ApiPolicies.ProjectManage, policy => policy.RequireRole(SystemRoles.ProjectManager, SystemRoles.TenantAdmin));
    options.AddPolicy(ApiPolicies.DatasourceRead, policy => policy.RequireRole(SystemRoles.StandardUser, SystemRoles.DataSourceManager, SystemRoles.TenantAdmin));
    options.AddPolicy(ApiPolicies.DatasourceManage, policy => policy.RequireRole(SystemRoles.DataSourceManager, SystemRoles.TenantAdmin));
    options.AddPolicy(ApiPolicies.ProjectSummaryGenerate, policy => policy.RequireRole(SystemRoles.ProjectManager));
    options.AddPolicy(ApiPolicies.DatasourceSummaryGenerate, policy => policy.RequireRole(SystemRoles.DataSourceManager));
    options.AddPolicy(SystemRoles.StandardUser, policy => policy.RequireRole(SystemRoles.StandardUser, SystemRoles.ProjectManager, SystemRoles.DataSourceManager, SystemRoles.TenantAdmin));
});

builder.Services.AddRateLimiter(options =>
{
    // Per-authenticated-user sliding window: protects expensive summary generation
    options.AddPolicy(RateLimitPolicies.SummaryGeneration, context =>
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(userId, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(5),
            SegmentsPerWindow = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Per-authenticated-user fixed window: admin operations
    options.AddPolicy(RateLimitPolicies.AdminOperations, context =>
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Uvse.Web"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Uvse.Web")
        .AddConsoleExporter());

var app = builder.Build();

app.UseExceptionHandler(handler =>
    handler.Run(async ctx =>
    {
        var exceptionFeature = ctx.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var (status, title, detail) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                exception.Message),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required or the access token is invalid."),
            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this action."),
            FeatureNotEnabledException => (
                StatusCodes.Status403Forbidden,
                "Feature Not Available",
                exception.Message),
            ProviderNotEnabledException => (
                StatusCodes.Status422UnprocessableEntity,
                "Provider Not Enabled",
                exception.Message),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred. Please try again later.")
        };

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { title, status, detail });
    }));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapPost(
        "/api/admin/plugins/enable",
        [Authorize(Policy = SystemRoles.TenantAdmin)] async (
            EnablePluginRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new EnablePluginCommand(request.ProviderKey, request.Settings), cancellationToken);
            return Results.Ok(result);
        })
    .WithName("EnablePlugin")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

var projectRoutes = app.MapGroup("/api/projects").RequireAuthorization(ApiPolicies.ProjectRead);

projectRoutes.MapGet(
        "/",
        async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ListProjectsQuery(), cancellationToken)))
    .WithName("ListProjects")
    .WithOpenApi();

projectRoutes.MapGet(
        "/{projectId:guid}",
        async (Guid projectId, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetProjectByIdQuery(projectId), cancellationToken)))
    .WithName("GetProjectById")
    .WithOpenApi();

projectRoutes.MapPost(
        "/",
        [Authorize(Policy = ApiPolicies.ProjectManage)] async (CreateProjectRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new CreateProjectCommand(request.Name, request.AllowedUserIds, request.DatasourceIds), cancellationToken)))
    .WithName("CreateProject")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

projectRoutes.MapPut(
        "/{projectId:guid}",
        [Authorize(Policy = ApiPolicies.ProjectManage)] async (Guid projectId, UpdateProjectRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new UpdateProjectCommand(projectId, request.Name, request.AllowedUserIds, request.DatasourceIds), cancellationToken)))
    .WithName("UpdateProject")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

projectRoutes.MapDelete(
        "/{projectId:guid}",
        [Authorize(Policy = ApiPolicies.ProjectManage)] async (Guid projectId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteProjectCommand(projectId), cancellationToken);
            return Results.NoContent();
        })
    .WithName("DeleteProject")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

var datasourceRoutes = app.MapGroup("/api/datasources").RequireAuthorization(ApiPolicies.DatasourceRead);

datasourceRoutes.MapGet(
        "/",
        async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ListDatasourcesQuery(), cancellationToken)))
    .WithName("ListDatasources")
    .WithOpenApi();

datasourceRoutes.MapGet(
        "/{datasourceId:guid}",
        async (Guid datasourceId, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetDatasourceByIdQuery(datasourceId), cancellationToken)))
    .WithName("GetDatasourceById")
    .WithOpenApi();

datasourceRoutes.MapPost(
        "/",
        [Authorize(Policy = ApiPolicies.DatasourceManage)] async (CreateDatasourceRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new CreateDatasourceCommand(
                    request.Name,
                    request.Type,
                    request.IsActive,
                    request.AccessScope,
                    request.AllowedUserIds,
                    request.ConnectionDetails),
                cancellationToken)))
    .WithName("CreateDatasource")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

datasourceRoutes.MapPut(
        "/{datasourceId:guid}",
        [Authorize(Policy = ApiPolicies.DatasourceManage)] async (Guid datasourceId, UpdateDatasourceRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new UpdateDatasourceCommand(
                    datasourceId,
                    request.Name,
                    request.Type,
                    request.IsActive,
                    request.AccessScope,
                    request.AllowedUserIds,
                    request.ConnectionDetails),
                cancellationToken)))
    .WithName("UpdateDatasource")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

datasourceRoutes.MapDelete(
        "/{datasourceId:guid}",
        [Authorize(Policy = ApiPolicies.DatasourceManage)] async (Guid datasourceId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteDatasourceCommand(datasourceId), cancellationToken);
            return Results.NoContent();
        })
    .WithName("DeleteDatasource")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.AdminOperations);

var summaryRoutes = app.MapGroup("/api/summaries").RequireAuthorization(SystemRoles.StandardUser);

summaryRoutes.MapGet(
        "/{summaryId:guid}",
        async (Guid summaryId, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetSummaryByIdQuery(summaryId), cancellationToken)))
    .WithName("GetSummaryById")
    .WithOpenApi();

summaryRoutes.MapPost(
        "/projects",
        [Authorize(Policy = ApiPolicies.ProjectSummaryGenerate)] async (
            GenerateProjectSummaryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new GenerateProjectSummaryCommand(
                    request.RequesterId,
                    request.ProjectId,
                    request.FromUtc,
                    request.ToUtc,
                    request.RequestedModes,
                    request.ComparisonSummaryId),
                cancellationToken)))
    .WithName("GenerateProjectSummary")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.SummaryGeneration);

summaryRoutes.MapPost(
        "/datasources",
        [Authorize(Policy = ApiPolicies.DatasourceSummaryGenerate)] async (
            GenerateDatasourceSummaryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(
                new GenerateDatasourceSummaryCommand(
                    request.RequesterId,
                    request.DatasourceId,
                    request.FromUtc,
                    request.ToUtc,
                    request.RequestedModes,
                    request.ComparisonSummaryId),
                cancellationToken)))
    .WithName("GenerateDatasourceSummary")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.SummaryGeneration);

app.MapPost(
        "/api/summaries/providers",
        [Authorize(Policy = SystemRoles.StandardUser)] async (
            GenerateProviderSummaryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GenerateProviderSummaryQuery(
                    request.ProviderKey,
                    request.FromUtc,
                    request.ToUtc,
                    request.DetailLevel,
                    request.AudienceTone),
                cancellationToken);

            return Results.Ok(result);
        })
    .WithName("GenerateProviderSummary")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.SummaryGeneration);

app.Run();

public partial class Program;

internal static class RateLimitPolicies
{
    public const string SummaryGeneration = "summary-generation";
    public const string AdminOperations = "admin-operations";
}

internal static class ApiPolicies
{
    public const string ProjectRead = "project-read";
    public const string ProjectManage = "project-manage";
    public const string DatasourceRead = "datasource-read";
    public const string DatasourceManage = "datasource-manage";
    public const string ProjectSummaryGenerate = "project-summary-generate";
    public const string DatasourceSummaryGenerate = "datasource-summary-generate";
}
