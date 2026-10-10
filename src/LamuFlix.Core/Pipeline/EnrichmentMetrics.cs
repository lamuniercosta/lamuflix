using System.Diagnostics.Metrics;

namespace LamuFlix.Core.Pipeline;

public static class EnrichmentMetrics
{
    private static readonly Meter ApplicationMeter = new(TelemetryConstants.ActivitySourceName);

    public static readonly Histogram<double> EnrichmentDuration =
        ApplicationMeter.CreateHistogram<double>(TelemetryConstants.EnrichmentDurationMetricName, unit: "s");

    public static readonly Counter<long> EnrichmentOutcome =
        ApplicationMeter.CreateCounter<long>(TelemetryConstants.EnrichmentOutcomeMetricName);

    public static readonly Counter<long> ImportCount =
        ApplicationMeter.CreateCounter<long>(TelemetryConstants.ImportCountMetricName);
}
