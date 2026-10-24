using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;

namespace LamuFlix.Infrastructure.RabbitMq;

public static class FeaturesRouting
{
    public const string Ack = "ack";
    public const string Retry = "retry";
    public const string DeadLetter = "dead-letter";

    public static Disposition Decide(ProcessFeaturesOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return outcome switch
        {
            ProcessFeaturesOutcome.Completed => new Disposition(Ack, EnrichmentRouting.FeaturesRequestedRoutingKey, null),
            ProcessFeaturesOutcome.Failed failed => From(failed.Decision),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown feature outcome."),
        };
    }

    private static Disposition From(EnrichmentFailureDecision decision) =>
        decision.Action switch
        {
            _ when ReferenceEquals(decision.Action, EnrichmentFailureAction.Retry) => RetryDisposition(decision),
            _ when ReferenceEquals(decision.Action, EnrichmentFailureAction.RetryDelayed) => RetryDisposition(decision),
            _ => new Disposition(
                DeadLetter,
                EnrichmentRouting.FeaturesDeadLetterRoutingKey,
                null),
        };

    private static Disposition RetryDisposition(EnrichmentFailureDecision decision) =>
        new(Retry, EnrichmentRouting.FeaturesRetryRoutingKey, decision.NextAttempt);

    public sealed record Disposition(string Action, string RoutingKey, int? NextAttempt);
}
