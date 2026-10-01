using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Infrastructure.RabbitMq;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class EnrichmentRoutingTests
{
    [Fact]
    public void Decide_Completed_IsAcked()
    {
        // arrange
        var outcome = new ProcessEnrichmentOutcome.Completed(Claimed: true);

        // act
        var disposition = EnrichmentRouting.Decide(outcome);

        // assert
        disposition.Action.ShouldBe(EnrichmentRouting.Ack);
        disposition.RoutingKey.ShouldBe(RabbitMqTopology.RequestedRoutingKey);
        disposition.NextAttempt.ShouldBeNull();
    }

    [Fact]
    public void Decide_SkippedClaim_IsAcked()
    {
        // arrange
        var outcome = new ProcessEnrichmentOutcome.Completed(Claimed: false);

        // act
        var disposition = EnrichmentRouting.Decide(outcome);

        // assert
        disposition.Action.ShouldBe(EnrichmentRouting.Ack);
        disposition.NextAttempt.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Decide_RetryAndRetryDelayed_GoToTheRetryQueueWithTheNextAttempt(bool delayed)
    {
        // arrange
        var action = delayed ? EnrichmentFailureAction.RetryDelayed : EnrichmentFailureAction.Retry;
        var outcome = new ProcessEnrichmentOutcome.Failed(new EnrichmentFailureDecision(action, 3));

        // act
        var disposition = EnrichmentRouting.Decide(outcome);

        // assert
        disposition.RoutingKey.ShouldBe(RabbitMqTopology.RetryRoutingKey);
        disposition.Action.ShouldBe(RabbitMqTopology.RetryRoutingKey);
        disposition.NextAttempt.ShouldBe(3);
    }

    [Fact]
    public void Decide_DeadLetter_GoesToTheDeadLetterQueue()
    {
        // arrange
        var outcome = new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null));

        // act
        var disposition = EnrichmentRouting.Decide(outcome);

        // assert
        disposition.RoutingKey.ShouldBe(RabbitMqTopology.DeadLetterRoutingKey);
        disposition.Action.ShouldBe(RabbitMqTopology.DeadLetterRoutingKey);
        disposition.NextAttempt.ShouldBeNull();
    }

    [Fact]
    public void Decide_WithoutAnOutcome_IsRejected()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed - the routing table must reject a missing outcome.
        ProcessEnrichmentOutcome outcome = null!;

        // act
        var rejected = Should.Throw<ArgumentOutOfRangeException>(() => EnrichmentRouting.Decide(outcome));

        // assert
        rejected.ParamName.ShouldBe("outcome");
    }
}
