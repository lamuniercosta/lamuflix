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
