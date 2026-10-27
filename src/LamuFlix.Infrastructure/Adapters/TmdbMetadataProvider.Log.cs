using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class TmdbMetadataProvider
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "The secondary metadata provider skips the lookup without an outbound request: Enabled is {Enabled} and a blank API key is treated as unavailable.")]
        public static partial void NotConfigured(ILogger logger, bool enabled);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "The secondary metadata provider is unavailable: HTTP {StatusCode}, category {Category}, failure {FailureType}.")]
        public static partial void ProviderUnavailable(ILogger logger, string statusCode, string category, string failureType);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Warning,
            Message = "The secondary metadata provider returned an unusable response: {Reason}.")]
        public static partial void InvalidResponse(ILogger logger, string reason);
    }
}
