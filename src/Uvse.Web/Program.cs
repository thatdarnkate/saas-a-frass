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
using Uvse.Application.Summaries.Queries.GenerateWeeklySummary;
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
    options.AddPolicy(SystemRoles.StandardUser, policy => policy.RequireRole(SystemRoles.StandardUser, SystemRoles.TenantAdmin));
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

app.MapPost(
        "/api/summaries/weekly",
        [Authorize(Policy = SystemRoles.StandardUser)] async (
            GenerateWeeklySummaryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GenerateWeeklySummaryQuery(
                    request.ProviderKey,
                    request.FromUtc,
                    request.ToUtc,
                    request.DetailLevel,
                    request.AudienceTone),
                cancellationToken);

            return Results.Ok(result);
        })
    .WithName("GenerateWeeklySummary")
    .WithOpenApi()
    .RequireRateLimiting(RateLimitPolicies.SummaryGeneration);

app.Run();

public partial class Program;

internal static class RateLimitPolicies
{
    public const string SummaryGeneration = "summary-generation";
    public const string AdminOperations = "admin-operations";
}
