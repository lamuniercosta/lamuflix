using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Persistence;

public sealed partial class HybridCachedRecommendationProfileSource
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Cached taste profile payload is {PayloadBytes} bytes against a {BudgetBytes} byte budget (fraction {Fraction}).")]
        public static partial void PayloadObserved(ILogger logger, int payloadBytes, long budgetBytes, double fraction);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "Cached taste profile payload is {PayloadBytes} bytes against a {BudgetBytes} byte budget (fraction {Fraction}), at or above the configured warning fraction.")]
        public static partial void PayloadNearLimit(ILogger logger, int payloadBytes, long budgetBytes, double fraction);
    }
}
