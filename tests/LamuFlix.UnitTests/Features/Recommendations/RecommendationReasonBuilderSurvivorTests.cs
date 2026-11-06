using System;
using System.Collections.Immutable;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class RecommendationReasonBuilderSurvivorTests
{
    [Fact]
    public void AddProfileReasons_NullBuilder_ThrowsArgumentNullException()
    {
        // arrange
        var evidence = new ProfileSimilarityEvidence(0.0, ImmutableArray<string>.Empty, false, 0.0);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(
            () => RecommendationReasonBuilder.AddProfileReasons(null!, evidence));

        // assert
        failure.ParamName.ShouldBe("builder");
    }

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
    public void AddProfileReasons_WhitespaceGenre_EmitsNothing()
    {
        // arrange
        var builder = ImmutableList.CreateBuilder<Reason>();
        var evidence = new ProfileSimilarityEvidence(0.5, ImmutableArray.Create("   "), false, 0.0);

        // act
        RecommendationReasonBuilder.AddProfileReasons(builder, evidence);

        // assert
        builder.ShouldBeEmpty();
    }
}
