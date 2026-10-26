namespace LamuFlix.Core.Pipeline;

public static class TelemetryConstants
{
    public const string ActivitySourceName = "LamuFlix";

    public const string RabbitMqPublisherActivitySourceName = "RabbitMQ.Client.Publisher";

    public const string RabbitMqSubscriberActivitySourceName = "RabbitMQ.Client.Subscriber";

    public const string HandlerOutcome = "lamuflix.handler.outcome";

    public const string HandlerOutcomeValidationFailed = "validation_failed";

    public const string HandlerRequest = "lamuflix.handler.request";

    public const string ErrorType = "error.type";

    public const string MovieId = "lamuflix.movie.id";

    public const string EnrichmentEnqueue = "Enrichment.Enqueue";

    public const string EnrichmentProcess = "Enrichment.Process";

    public const string FeaturesProcess = "Features.Process";

    public const string FeaturesOutcome = "features.outcome";

    public const string MetadataLookup = "Metadata.Lookup";

    public const string EnrichmentOutcome = "enrichment.outcome";

    public const string MessagingDeliveryCount = "messaging.rabbitmq.delivery_count";

    public const string EnrichmentDurationMetricName = "lamuflix.enrichment.duration";

    public const string EnrichmentDurationUnit = "s";

    public const string EnrichmentOutcomeMetricName = "lamuflix.enrichment.outcome";

    public const string ImportCountMetricName = "lamuflix.import.count";

    public const string EmbeddingGenerate = "Embedding.Generate";

    public const string EmbeddingDurationMetricName = "lamuflix.embedding.duration";

    public const string EmbeddingDurationUnit = "s";

    public const string EmbeddingOutcomeMetricName = "lamuflix.embedding.outcome";

    public const string SucceededOutcome = "succeeded";

    public const string CancelledOutcome = "cancelled";

    public const string ProviderTagName = "provider";

    public const string OutcomeTagName = "outcome";

    public const string FailureCategoryTagName = "failure_category";

    public const string EnrichedOutcome = "enriched";

    public const string NotFoundOutcome = "not_found";

    public const string FailedOutcome = "failed";

    public const string OmdbProviderName = "omdb";

    public const string OutboxPendingMetricName = "lamuflix.outbox.pending";

    public const string OutboxOldestPendingAgeMetricName = "lamuflix.outbox.oldest_pending_age";

    public const string OutboxPublishedMetricName = "lamuflix.outbox.published";

    public const string OutboxFailuresMetricName = "lamuflix.outbox.failures";

    public const string OutboxRetentionDeletedMetricName = "lamuflix.outbox.retention.deleted";

    public const string OutboxRetentionFailuresMetricName = "lamuflix.outbox.retention.failures";
}
