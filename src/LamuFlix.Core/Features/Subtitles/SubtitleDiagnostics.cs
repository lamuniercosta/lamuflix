using System;
using System.Diagnostics;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleDiagnostics
{
    public static void TagMovie(MovieId movieId)
    {
        ArgumentNullException.ThrowIfNull(movieId);
        Activity.Current?.SetTag(TelemetryConstants.MovieId, movieId.Value);
    }

    public static void TagFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Activity.Current?.SetTag(TelemetryConstants.FailureCategoryTagName, Category(exception));
    }

    public static string Category(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            NotFoundException => TelemetryConstants.NotFoundOutcome,
            ValidationException => TelemetryConstants.HandlerOutcomeValidationFailed,
            LibraryFileAccessDeniedException => TelemetryConstants.DeniedOutcome,
            _ => TelemetryConstants.FailedOutcome,
        };
    }
}
