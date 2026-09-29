using FsCheck.Fluent;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQueryStringTests
{
    [Fact]
    public void Format_RepeatedGenreIds_PreservesOrder()
    {
        var query = Valid(genres: [1, 2, 3]);

        var formatted = MovieQueryString.Format(query);

        formatted.ShouldContain("genreIds=1&genreIds=2&genreIds=3");
    }

    [Fact]
    public void Format_NullText_OmitsTextKey()
    {
        var formatted = MovieQueryString.Format(Valid(text: null));

        formatted.ShouldNotContain("text=");
    }

    [Fact]
    public void Format_EmptyText_EmitsEmptyValue()
    {
        MovieQueryString.TryParse(MovieQueryString.Format(Valid(text: string.Empty)), out var parsed).ShouldBeTrue();
        parsed!.Text.ShouldBe(string.Empty);
    }

    [Fact]
    public void Format_NullRuntimeBounds_EmitsEmptyRangeKeys()
    {
        var query = Valid(runtime: new RuntimeRange(null, null, false));

        var formatted = MovieQueryString.Format(query);

        formatted.ShouldContain("runtimeMin=&runtimeMax=&runtimeIncludeUnknown=false");
    }

    [Fact]
    public void Format_NullYearBounds_EmitsEmptyRangeKeys()
    {
        var formatted = MovieQueryString.Format(Valid(year: new YearRange(null, null)));

        formatted.ShouldContain("yearMin=&yearMax=");
    }

    [Fact]
    public void TryParse_MinimalQuery_SucceedsAndRoundTrips()
    {
        const string raw = "sort=Title&direction=Ascending&page=1&pageSize=20";

        MovieQueryString.TryParse(raw, out var query).ShouldBeTrue();
        query!.Sort.ShouldBe(MovieSort.Title);
        query.Direction.ShouldBe(SortDirection.Ascending);
        query.Page.ShouldBe(new Page(1, 20));
        MovieQueryString.TryParse(MovieQueryString.Format(query), out var again).ShouldBeTrue();
        again.ShouldBe(query);
    }

    [Fact]
    public void TryParse_MissingPageSize_ReturnsFalse()
    {
        MovieQueryString.TryParse("sort=Title&direction=Ascending&page=1", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_UnknownKey_IsIgnored()
    {
        MovieQueryString.TryParse("noise=1&sort=Title&direction=Ascending&page=1&pageSize=20", out var query).ShouldBeTrue();
        query!.Text.ShouldBeNull();
    }

    [Fact]
    public void TryParse_MalformedInt_ReturnsFalse()
    {
        MovieQueryString.TryParse("page=nope&sort=Title&direction=Ascending&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_MalformedBool_ReturnsFalse()
    {
        MovieQueryString.TryParse("inWatchlist=yes&sort=Title&direction=Ascending&page=1&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_NonExactName_ReturnsFalse()
    {
        MovieQueryString.TryParse("sort=title&direction=Ascending&page=1&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_DuplicatedScalar_ReturnsFalse()
    {
        MovieQueryString.TryParse("sort=Title&sort=Year&direction=Ascending&page=1&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_PartialRange_ReturnsFalse()
    {
        MovieQueryString.TryParse("yearMin=1990&sort=Title&direction=Ascending&page=1&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_EmptyNonRangeValue_ReturnsFalse()
    {
        MovieQueryString.TryParse("sort=&direction=Ascending&page=1&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_EmptyText_IsEmptyString()
    {
        MovieQueryString.TryParse("text=&sort=Title&direction=Ascending&page=1&pageSize=20", out var query).ShouldBeTrue();
        query!.Text.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Format_RoundTripsValidatorAcceptedQueries()
    {
        Prop.ForAll(MovieQueryFixture.Queries(), query =>
        {
            var formatted = MovieQueryString.Format(query);
            return MovieQueryString.TryParse(formatted, out var parsed) && query.Equals(parsed);
        }).QuickCheckThrowOnFailure();
    }

    private static MovieQuery Valid(
        string? text = null,
        int[]? genres = null,
        RuntimeRange? runtime = null,
        YearRange? year = null) =>
        new()
        {
            Text = text,
            GenreIds = genres is null ? [] : [.. genres],
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 20),
            Runtime = runtime,
            Year = year,
            Statuses = [EnrichmentStatus.Pending],
        };
}
