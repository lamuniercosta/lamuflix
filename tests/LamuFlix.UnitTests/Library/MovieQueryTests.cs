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
}
