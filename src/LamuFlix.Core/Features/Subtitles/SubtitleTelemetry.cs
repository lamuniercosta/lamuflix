using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleTelemetry
{
    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);

    private static readonly Meter ApplicationMeter = new(TelemetryConstants.ActivitySourceName);

    private static readonly Histogram<double> Duration =
        ApplicationMeter.CreateHistogram<double>(
            TelemetryConstants.SubtitleDurationMetricName,
            unit: TelemetryConstants.SubtitleDurationUnit);

    private static readonly Counter<long> Outcome =
        ApplicationMeter.CreateCounter<long>(TelemetryConstants.SubtitleOutcomeMetricName);

    public static Activity? StartPrepare() => Start(TelemetryConstants.SubtitlePrepare);

    public static Activity? StartServe() => Start(TelemetryConstants.SubtitleServe);

    public static Activity? StartProviderLookup() => Start(TelemetryConstants.SubtitleProviderLookup);

    public static Activity? StartDownload() => Start(TelemetryConstants.SubtitleDownload);

    public static void Complete(Activity? activity, MovieId movieId, string outcome, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(movieId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);

        if (activity is not null)
        {
            activity.SetTag(TelemetryConstants.MovieId, movieId.Value);
            activity.SetTag(TelemetryConstants.OutcomeTagName, outcome);
        }

        var tag = new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, outcome);
        Outcome.Add(1, tag);
        Duration.Record(elapsed.TotalSeconds, tag);
    }

    public static void Fail(Activity? activity, MovieId movieId, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(movieId);
        ArgumentNullException.ThrowIfNull(exception);

        var category = SubtitleDiagnostics.Category(exception);
        if (activity is not null)
        {
            activity.SetTag(TelemetryConstants.MovieId, movieId.Value);
            activity.SetTag(TelemetryConstants.OutcomeTagName, category);
            activity.SetTag(TelemetryConstants.FailureCategoryTagName, category);
            activity.SetStatus(ActivityStatusCode.Error);
        }

        Outcome.Add(1, new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, category));
    }

    private static Activity? Start(string name) =>
        Source.StartActivity(name);
}
