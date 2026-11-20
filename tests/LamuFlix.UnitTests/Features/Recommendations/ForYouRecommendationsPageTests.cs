using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        var page = new ForYouRecommendationsPage(items, true);

        // assert
        page.Items.ShouldBe(items);
        page.HasMore.ShouldBeTrue();
        page.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_EmptyItems_HasZeroCountAndNoMore()
    {
        // act
        var page = new ForYouRecommendationsPage(ImmutableList<RecommendedMovie>.Empty, false);

        // assert
        page.Items.ShouldBeEmpty();
        page.HasMore.ShouldBeFalse();
        page.Count.ShouldBe(0);
    }

    [Fact]
    public void Indexer_ReturnsItemAtPosition()
    {
        // arrange
        var page = new ForYouRecommendationsPage(
            ImmutableList.Create(NewRecommendation(7), NewRecommendation(9)),
            false);

        // act / assert
        page[0].Summary.Id.ShouldBe(new MovieId(7));
        page[1].Summary.Id.ShouldBe(new MovieId(9));
    }

    [Fact]
    public void Enumeration_YieldsItemsInOrder()
    {
        // arrange
        var page = new ForYouRecommendationsPage(
            ImmutableList.Create(NewRecommendation(7), NewRecommendation(9)),
            false);

        // act
        var ids = page.Select(item => item.Summary.Id.Value).ToList();

        // assert
        ids.ShouldBe(new[] { 7, 9 });
    }

    [Fact]
    public void NonGenericEnumeration_YieldsItemsInOrder()
    {
        // arrange
        var page = new ForYouRecommendationsPage(
            ImmutableList.Create(NewRecommendation(7), NewRecommendation(9)),
            false);

        // act
        var ids = new List<int>();
        foreach (var item in (IEnumerable)page)
        {
            ids.Add(((RecommendedMovie)item).Summary.Id.Value);
        }

        // assert
        ids.ShouldBe(new[] { 7, 9 });
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
            ImmutableList<Reason>.Empty);
}
