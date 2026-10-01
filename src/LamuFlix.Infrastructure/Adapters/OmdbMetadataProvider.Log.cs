using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OmdbMetadataProvider
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "The metadata provider rejected the API key with HTTP 401. Check the OMDb API key configuration.")]
        public static partial void InvalidApiKey(ILogger logger);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "The metadata provider is rate limiting this library: HTTP {StatusCode} after exhausting the retry budget.")]
        public static partial void RateLimited(ILogger logger, string statusCode);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Warning,
            Message = "The metadata provider is unavailable: HTTP {StatusCode}, category {Category}, failure {FailureType}.")]
        public static partial void ProviderUnavailable(ILogger logger, string statusCode, string category, string failureType);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Warning,
            Message = "The metadata provider returned an unusable response: {Reason}.")]
        public static partial void InvalidResponse(ILogger logger, string reason);
    }
}