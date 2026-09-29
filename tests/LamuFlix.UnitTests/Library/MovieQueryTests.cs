using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQueryTests
{
    [Fact]
    public void Equals_DefaultAndEmptyArrays_AreEqual()
    {
        var left = new MovieQuery();
        var right = new MovieQuery
        {
            GenreIds = [],
            ActorIds = [],
            Statuses = [],
        };

        left.Equals(right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_SameIdsInDifferentOrder_AreNotEqual()
    {
        var left = new MovieQuery { GenreIds = [1, 2], ActorIds = [3], Statuses = [EnrichmentStatus.Pending, EnrichmentStatus.Failed] };
        var right = new MovieQuery { GenreIds = [2, 1], ActorIds = [3], Statuses = [EnrichmentStatus.Failed, EnrichmentStatus.Pending] };

        left.Equals(right).ShouldBeFalse();
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_SameSequence_IsEqual()
    {
        var left = new MovieQuery
        {
            GenreIds = [1, 2],
            ActorIds = [4, 5],
            Statuses = [EnrichmentStatus.Enriched],
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 20),
        };
        var right = new MovieQuery
        {
            GenreIds = [1, 2],
            ActorIds = [4, 5],
            Statuses = [EnrichmentStatus.Enriched],
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 20),
        };

        left.Equals(right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(Equals_IsFalse_WhenExactlyOneFieldDiffersCases))]
    public void Equals_IsFalse_WhenExactlyOneFieldDiffers(string field, MovieQuery left, MovieQuery right)
    {
        left.Equals(right).ShouldBeFalse($"{field} should differ");
        right.Equals(left).ShouldBeFalse($"{field} should differ");
    }

    public static TheoryData<string, MovieQuery, MovieQuery> Equals_IsFalse_WhenExactlyOneFieldDiffersCases => new()
    {
        { "Text", BaseQuery(), BaseQuery() with { Text = "other" } },
        { "Runtime", BaseQuery(), BaseQuery() with { Runtime = new RuntimeRange(11, null, true) } },
        { "Year", BaseQuery(), BaseQuery() with { Year = new YearRange(1999, null) } },
        { "GenreIds", BaseQuery(), BaseQuery() with { GenreIds = [4, 5] } },
        { "ActorIds", BaseQuery(), BaseQuery() with { ActorIds = [7, 8] } },
        { "Statuses", BaseQuery(), BaseQuery() with { Statuses = [EnrichmentStatus.Pending, EnrichmentStatus.Failed] } },
        { "InWatchlist", BaseQuery(), BaseQuery() with { InWatchlist = false } },
        { "Sort", BaseQuery(), BaseQuery() with { Sort = MovieSort.Year } },
        { "Direction", BaseQuery(), BaseQuery() with { Direction = SortDirection.Descending } },
        { "Page", BaseQuery(), BaseQuery() with { Page = new Page(2, 20) } },
    };

    private static MovieQuery BaseQuery() =>
        new()
        {
            Text = "query",
            GenreIds = [1, 2],
            ActorIds = [3, 4],
            Runtime = new RuntimeRange(10, 20, true),
            Year = new YearRange(1998, 2000),
            Statuses = [EnrichmentStatus.Pending, EnrichmentStatus.Enriched],
            InWatchlist = true,
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 20),
        };
}
