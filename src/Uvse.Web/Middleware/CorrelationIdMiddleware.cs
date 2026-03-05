using System.Diagnostics;

namespace Uvse.Web.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxCorrelationIdLength = 64;
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);
        await _next(context);
    }

    private static string ResolveCorrelationId(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue) || headerValue.Length > MaxCorrelationIdLength)
        {
            return Guid.NewGuid().ToString("n");
        }

        // Only allow alphanumeric characters and hyphens to prevent header injection
        foreach (var c in headerValue)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
            {
                return Guid.NewGuid().ToString("n");
            }
        }

        return headerValue;
    }
}
