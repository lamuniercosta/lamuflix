using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQuerySurvivorTests
{
    [Fact]
    public void Equals_NullOther_ReturnsFalse()
    {
        // arrange
        var query = BaseQuery();

        // act
        var equal = query.Equals(null);

        // assert
        equal.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_EqualQueries_AreEqual()
    {
        // arrange
        var left = BaseQuery();
        var right = BaseQuery();

        // act
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // assert
        left.Equals(right).ShouldBeTrue();
        leftHash.ShouldBe(rightHash);
    }

    [Theory]
    [MemberData(nameof(HashComponentCases))]
    public void GetHashCode_QueryDifferingInOneComponent_HashDiffers(string component, MovieQuery other)
    {
        // arrange
        var baseline = BaseQuery();

        // act
        var baselineHash = baseline.GetHashCode();
        var otherHash = other.GetHashCode();

        // assert
        other.Equals(baseline).ShouldBeFalse($"{component} should differ");
        otherHash.ShouldNotBe(baselineHash, $"{component} should contribute to the hash");
    }

    [Fact]
    public void GetHashCode_SequenceLengthDiffers_HashDiffers()
    {
        // arrange
        // Without the length prefix both queries feed the hash the same flat run of ids (1, 2).
        var longerGenres = new MovieQuery { GenreIds = [1, 2], ActorIds = [] };
        var longerActors = new MovieQuery { GenreIds = [1], ActorIds = [2] };

        // act
        var genresHash = longerGenres.GetHashCode();
        var actorsHash = longerActors.GetHashCode();

        // assert
        longerGenres.Equals(longerActors).ShouldBeFalse();
        genresHash.ShouldNotBe(actorsHash);
    }

    [Fact]
    public void GenreIds_DefaultArray_NormalizesToEmpty()
    {
        // arrange
        // act
        var query = new MovieQuery { GenreIds = default };

        // assert
        query.GenreIds.IsDefault.ShouldBeFalse();
        query.GenreIds.ShouldBeEmpty();
        query.Equals(new MovieQuery()).ShouldBeTrue();
    }

    [Fact]
    public void ActorIds_DefaultArray_NormalizesToEmpty()
    {
        // arrange
        // act
        var query = new MovieQuery { ActorIds = default };

        // assert
        query.ActorIds.IsDefault.ShouldBeFalse();
        query.ActorIds.ShouldBeEmpty();
        query.Equals(new MovieQuery()).ShouldBeTrue();
    }

    [Fact]
    public void Statuses_DefaultArray_NormalizesToEmpty()
    {
        // arrange
        // act
        var query = new MovieQuery { Statuses = default };

        // assert
        query.Statuses.IsDefault.ShouldBeFalse();
        query.Statuses.ShouldBeEmpty();
        query.Equals(new MovieQuery()).ShouldBeTrue();
    }

    public static TheoryData<string, MovieQuery> HashComponentCases => new()
    {
        { "Text", BaseQuery() with { Text = "other" } },
        { "GenreIds", BaseQuery() with { GenreIds = [4, 5] } },
        { "ActorIds", BaseQuery() with { ActorIds = [7, 8] } },
        { "Runtime", BaseQuery() with { Runtime = new RuntimeRange(11, null, true) } },
        { "Year", BaseQuery() with { Year = new YearRange(1999, null) } },
        { "Statuses", BaseQuery() with { Statuses = [EnrichmentStatus.Failed] } },
        { "InWatchlist", BaseQuery() with { InWatchlist = false } },
        { "CollectionId", BaseQuery() with { CollectionId = 42 } },
        { "Sort", BaseQuery() with { Sort = MovieSort.Year } },
        { "Direction", BaseQuery() with { Direction = SortDirection.Descending } },
        { "Page", BaseQuery() with { Page = new Page(2, 20) } },
        { "DirectorId", BaseQuery() with { DirectorId = 42 } },
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
            CollectionId = 7,
            DirectorId = 7,
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 20),
        };
}
