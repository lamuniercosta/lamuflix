namespace LamuFlix.Core.Pipeline;

public static class TelemetryConstants
{
    public const string ActivitySourceName = "LamuFlix";

    public const string HandlerRequest = "lamuflix.handler.request";

    public const string ErrorType = "error.type";

    public const string MovieId = "lamuflix.movie.id";

    public const string EnrichmentEnqueue = "Enrichment.Enqueue";

    public const string EnrichmentProcess = "Enrichment.Process";

    public const string MetadataLookup = "Metadata.Lookup";

    public const string EnrichmentOutcome = "enrichment.outcome";

    public const string MessagingDeliveryCount = "messaging.rabbitmq.delivery_count";
}
