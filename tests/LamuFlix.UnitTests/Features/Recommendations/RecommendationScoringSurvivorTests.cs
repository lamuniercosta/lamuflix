using System;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class RecommendationScoringSurvivorTests
{
    [Fact]
    public void ProfileContribution_NullOptions_ThrowsArgumentNullException()
    {
        // arrange
        var evidence = new ReviewAffinityEvidence(0.3, 0.0, true, null);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => RecommendationScoring.ProfileContribution(0.2, evidence, null!));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Genres_NullFeatures_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => RecommendationScoring.Genres(null!));

        // assert
        failure.ParamName.ShouldBe("features");
    }

    [Fact]
    public void Cast_NullFeatures_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => RecommendationScoring.Cast(null!));

        // assert
        failure.ParamName.ShouldBe("features");
    }

    [Fact]
    public void Directors_NullFeatures_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => RecommendationScoring.Directors(null!));

        // assert
        failure.ParamName.ShouldBe("features");
    }

    [Fact]
    public void Genres_UnsortedInput_ReturnsOrdinalOrder()
    {
        // arrange
        var features = new MovieFeatures(genres: ["b", "a", "c"]);

        // act
        var genres = RecommendationScoring.Genres(features);

        // assert
        genres.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public void Cast_UnsortedInput_ReturnsOrdinalOrder()
    {
        // arrange
        var features = new MovieFeatures(cast: ["b", "a", "c"]);

        // act
        var cast = RecommendationScoring.Cast(features);

        // assert
        cast.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public void Directors_UnsortedInput_ReturnsOrdinalOrder()
    {
        // arrange
        var features = new MovieFeatures(directors: ["b", "a", "c"]);

        // act
        var directors = RecommendationScoring.Directors(features);

        // assert
        directors.ShouldBe(["a", "b", "c"]);
    }
}
