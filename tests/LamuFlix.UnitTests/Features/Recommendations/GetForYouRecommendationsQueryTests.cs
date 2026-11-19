using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetForYouRecommendationsQueryTests
{
    [Fact]
    public void Constructor_OmittedTake_DefaultsToTen()
    {
        // act
        var query = new GetForYouRecommendationsQuery();

        // assert
        query.Take.ShouldBe(10);
        query.Offset.ShouldBe(0);
        query.ExcludedMovieIds.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ExclusionsAreDeduplicatedAndOffsetIsRetained()
    {
        var movieId = new LamuFlix.Core.Domain.MovieId(7);

        var query = new GetForYouRecommendationsQuery(
            offset: 3,
            excludedMovieIds: System.Collections.Immutable.ImmutableHashSet.Create(movieId, movieId));

        query.Offset.ShouldBe(3);
        query.ExcludedMovieIds.ShouldBe([movieId]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(50)]
    public void Constructor_ExplicitTake_RetainsValue(int take)
    {
        // act
        var query = new GetForYouRecommendationsQuery(take);

        // assert
        query.Take.ShouldBe(take);
    }

    [Fact]
    public void Equality_SameTake_IsEqual()
    {
        // act / assert
        new GetForYouRecommendationsQuery(4).ShouldBe(new GetForYouRecommendationsQuery(4));
    }

    [Fact]
    public void Equality_DifferentTake_IsNotEqual()
    {
        // act / assert
        new GetForYouRecommendationsQuery(4).ShouldNotBe(new GetForYouRecommendationsQuery(5));
    }
}
