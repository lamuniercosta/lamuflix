using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Infrastructure.RabbitMq;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class FeaturesRoutingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Decide_Completed_IsAckedRegardlessOfGeneration(bool generated)
    {
        // arrange
        var outcome = new ProcessFeaturesOutcome.Completed(generated);

        // act
        var disposition = FeaturesRouting.Decide(outcome);

        // assert
        disposition.Action.ShouldBe(FeaturesRouting.Ack);
        disposition.RoutingKey.ShouldBe(EnrichmentRouting.FeaturesRequestedRoutingKey);
        disposition.NextAttempt.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Decide_RetryAndRetryDelayed_UseTheFeaturesRetryKeyAndNextAttempt(bool delayed)
    {
        // arrange
        var action = delayed ? EnrichmentFailureAction.RetryDelayed : EnrichmentFailureAction.Retry;
        var outcome = new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(action, 4),
            EnrichmentFailureCategory.ProviderUnavailable);

        // act
        var disposition = FeaturesRouting.Decide(outcome);

        // assert
        disposition.Action.ShouldBe(FeaturesRouting.Retry);
        disposition.RoutingKey.ShouldBe(EnrichmentRouting.FeaturesRetryRoutingKey);
        disposition.NextAttempt.ShouldBe(4);
    }

    [Fact]
    public void Decide_DeadLetter_UsesOnlyTheFeaturesDeadLetterKey()
    {
        // arrange
        var outcome = new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse);

        // act
        var disposition = FeaturesRouting.Decide(outcome);

        // assert
        disposition.Action.ShouldBe(FeaturesRouting.DeadLetter);
        disposition.RoutingKey.ShouldBe(EnrichmentRouting.FeaturesDeadLetterRoutingKey);
        disposition.NextAttempt.ShouldBeNull();
    }

    [Fact]
    public void Decide_WithoutAnOutcome_IsRejected()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed - the routing table must reject a missing outcome.
        ProcessFeaturesOutcome outcome = null!;

        // act
        var rejected = Should.Throw<ArgumentNullException>(() => FeaturesRouting.Decide(outcome));

        // assert
        rejected.ParamName.ShouldBe("outcome");
    }
}
