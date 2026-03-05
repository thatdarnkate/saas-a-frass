using System.Diagnostics;
using System.Diagnostics.Metrics;
using Uvse.Web.Observability;

namespace Uvse.Web.Middleware;

public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public RequestMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, RequestMetrics metrics)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Use the matched route template to avoid high-cardinality tags from path parameters
            var routeTemplate = (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)
                ?.RoutePattern.RawText
                ?? context.Request.Path.ToString();

            var tags = new TagList
            {
                { "route", routeTemplate },
                { "method", context.Request.Method },
                { "status_code", context.Response.StatusCode }
            };

            metrics.RequestCounter.Add(1, tags);
            metrics.RequestDurationMilliseconds.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                metrics.ErrorCounter.Add(1, tags);
            }
        }
    }
}
