using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.IntegrationTests;

public sealed class ApiImportTraceCapture : IDisposable
{
    private static readonly string[] SourceNames =
    [
        TelemetryConstants.ActivitySourceName,
        TelemetryConstants.RabbitMqPublisherActivitySourceName,
        TelemetryConstants.RabbitMqSubscriberActivitySourceName,
    ];

    private readonly ConcurrentBag<Activity> stopped = [];
    private readonly ActivityListener listener;

    public ApiImportTraceCapture()
    {
        listener = new ActivityListener
        {
            ShouldListenTo = source => Array.IndexOf(SourceNames, source.Name) >= 0,
            Sample = AlwaysSample,
            ActivityStopped = activity => stopped.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);
    }

    public IReadOnlyList<Activity> Stopped => [.. stopped];

    public void Dispose() => listener.Dispose();

    private static ActivitySamplingResult AlwaysSample(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllData;
}
