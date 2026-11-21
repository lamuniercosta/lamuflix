namespace LamuFlix.UnitTests.Features.Recommendations;

using System;
using System.Collections.Immutable;
using LamuFlix.Core.Features.Recommendations;

public sealed class RecommendationReasonBuilderSurvivorTests
{
    [Fact]
    public void EnsureFallbackReason_NullBuilder_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(
            () => RecommendationReasonBuilder.EnsureFallbackReason(null!));

        // assert
        failure.ParamName.ShouldBe("builder");
    }

    [Fact]
    public void EnsureFallbackReason_EmptyBuilder_AddsNoSharedFeaturesFallback()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();

        // act
        RecommendationReasonBuilder.EnsureFallbackReason(builder);

        // assert
        builder.ShouldHaveSingleItem().Text.ShouldBe("No shared features identified");
    }

    [Fact]
    public void BuildSeedReasons_NullEvidence_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(
            () => RecommendationReasonBuilder.BuildSeedReasons(SeedSignal.Liked, "Heat", null!));

        // assert
        failure.ParamName.ShouldBe("evidence");
    }

    [Fact]
    public void BuildSeedReasons_WatchlistSignal_UsesWatchlistTemplate()
    {
        // act
        var reasons = RecommendationReasonBuilder.BuildSeedReasons(
            SeedSignal.Watchlist,
            "Heat",
            []);

        // assert
        reasons.ShouldHaveSingleItem().Text.ShouldBe("Because Heat is on your watchlist");
    }
}
