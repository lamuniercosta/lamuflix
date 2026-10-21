using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OllamaSharpEmbeddingGenerator
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "The embedding provider at {Endpoint} with model {Model} failed: category {Category}.")]
        public static partial void Failed(ILogger logger, string endpoint, string model, string category);
    }
}
