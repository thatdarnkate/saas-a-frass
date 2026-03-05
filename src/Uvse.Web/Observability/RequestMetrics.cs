using System.Diagnostics.Metrics;

namespace Uvse.Web.Observability;

public sealed class RequestMetrics : IDisposable
{
    private readonly Meter _meter = new("Uvse.Web");

    public Counter<long> RequestCounter { get; }
    public Counter<long> ErrorCounter { get; }
    public Histogram<double> RequestDurationMilliseconds { get; }

    public RequestMetrics()
    {
        RequestCounter = _meter.CreateCounter<long>("http.server.requests");
        ErrorCounter = _meter.CreateCounter<long>("http.server.errors");
        RequestDurationMilliseconds = _meter.CreateHistogram<double>("http.server.duration.ms");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
