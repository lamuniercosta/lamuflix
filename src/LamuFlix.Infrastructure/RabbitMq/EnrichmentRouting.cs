using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;

namespace LamuFlix.Infrastructure.RabbitMq;

public static class EnrichmentRouting
{
    public const string Ack = "ack";
    public const string Requeue = "requeue";
    public const string DeadLetter = "dead-letter";

    public static Disposition Decide(ProcessEnrichmentOutcome outcome) =>
        outcome switch
        {
            ProcessEnrichmentOutcome.Completed => new Disposition(Ack, RabbitMqTopology.RequestedRoutingKey, null),
            ProcessEnrichmentOutcome.Failed failed => From(failed.Decision),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown enrichment outcome."),
        };

    private static Disposition From(EnrichmentFailureDecision decision) =>
        decision.Action switch
        {
            _ when ReferenceEquals(decision.Action, EnrichmentFailureAction.Retry) => Retry(decision),
            _ when ReferenceEquals(decision.Action, EnrichmentFailureAction.RetryDelayed) => Retry(decision),
            _ => new Disposition(RabbitMqTopology.DeadLetterRoutingKey, RabbitMqTopology.DeadLetterRoutingKey, null),
        };

    private static Disposition Retry(EnrichmentFailureDecision decision) =>
        new(RabbitMqTopology.RetryRoutingKey, RabbitMqTopology.RetryRoutingKey, decision.NextAttempt);

    public sealed record Disposition(string Action, string RoutingKey, int? NextAttempt);
}