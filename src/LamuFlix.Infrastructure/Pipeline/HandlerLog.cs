using System;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Pipeline;

internal static partial class HandlerLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Handled {RequestType} in {ElapsedMilliseconds} ms")]
    public static partial void Completed(ILogger logger, string requestType, double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Validation failed for {RequestType} in {ElapsedMilliseconds} ms")]
    public static partial void ValidationFailed(ILogger logger, string requestType, double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Handler {RequestType} failed in {ElapsedMilliseconds} ms")]
    public static partial void Failed(ILogger logger, Exception exception, string requestType, double elapsedMilliseconds);
}
