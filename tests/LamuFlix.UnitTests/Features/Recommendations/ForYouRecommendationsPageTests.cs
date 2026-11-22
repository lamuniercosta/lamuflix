using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class ForYouRecommendationsPageTests
{
    [Fact]
    public void Constructor_ItemsAndHasMore_AreRetained()
    {
        // arrange
        var items = ImmutableList.Create(NewRecommendation(7), NewRecommendation(9));

        // act
        var page = new ForYouRecommendationsPage(items, true, true);

        // assert
        page.Items.ShouldBe(items);
        page.HasMore.ShouldBeTrue();
        page.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_EmptyItems_HasNoMore()
    {
        // act
        var page = new ForYouRecommendationsPage(ImmutableList<RecommendedMovie>.Empty, false, false);

        // assert
        page.Items.ShouldBeEmpty();
        page.HasMore.ShouldBeFalse();
        page.HasSeeds.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithoutSeeds_RetainsFalseHasSeeds()
    {
        // arrange
        var items = ImmutableList.Create(NewRecommendation(7));

        // act
        var page = new ForYouRecommendationsPage(items, false, false);

        // assert
        page.HasSeeds.ShouldBeFalse();
    }

    private static RecommendedMovie NewRecommendation(int id) =>
        new(
            new MovieSummary(
                new MovieId(id),
                $"Movie {id}",
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                false,
                false,
                Review.None),
            0.5,
            ImmutableList<RecommendationEvidence>.Empty);
}
