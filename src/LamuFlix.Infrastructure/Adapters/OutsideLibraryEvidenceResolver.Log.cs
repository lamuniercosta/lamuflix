using System;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed partial class OutsideLibraryEvidenceResolver
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "Outside-library lookup for TMDb id {TmdbId}: sends {UsedSends}/{Ceiling}, external-id calls {ExternalIdCalls}, UTC date {UsageDate}, status {Status}.")]
        public static partial void Lookup(
            ILogger logger,
            int tmdbId,
            int usedSends,
            int ceiling,
            int externalIdCalls,
            DateOnly usageDate,
            CandidateLookupStatus status);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Information,
            Message = "Outside-library evidence for TMDb id {TmdbId} deferred after quota denial at send {UsedSends} of {Ceiling} for UTC date {UsageDate}; the remainder of this load uses cached evidence only.")]
        public static partial void Deferred(
            ILogger logger,
            int tmdbId,
            int usedSends,
            int ceiling,
            DateOnly usageDate);
    }
}
