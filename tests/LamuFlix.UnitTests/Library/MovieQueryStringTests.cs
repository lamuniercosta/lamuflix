using System;
using System.Globalization;
using FsCheck.Xunit;
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
        parsed.ShouldNotBeNull();
        parsed.Text.ShouldBe(string.Empty);
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
    public void Format_NullRating_OmitsRatingKeys()
    {
        var formatted = MovieQueryString.Format(Valid());

        formatted.ShouldNotContain("minRating=");
        formatted.ShouldNotContain("maxRating=");
    }

    [Fact]
    public void Format_NullRatingBounds_EmitsEmptyRangeKeys()
    {
        var formatted = MovieQueryString.Format(Valid(rating: new RatingRange(null, null)));

        formatted.ShouldContain("minRating=&maxRating=");
    }

    [Theory]
    [InlineData(7, "minRating=7&maxRating=")]
    [InlineData(7.1, "minRating=7.1&maxRating=")]
    [InlineData(0, "minRating=0&maxRating=")]
    [InlineData(10, "minRating=10&maxRating=")]
    public void Format_RatingBound_EmitsShortestInvariantForm(double bound, string expected)
    {
        var formatted = MovieQueryString.Format(Valid(rating: new RatingRange((decimal)bound, null)));

        formatted.ShouldContain(expected);
    }

    [Fact]
    public void Format_TrailingZeroRatingBounds_EmitsShortestInvariantForm()
    {
        var formatted = MovieQueryString.Format(Valid(rating: new RatingRange(7.0m, 10.00m)));

        formatted.ShouldContain("minRating=7&maxRating=10");
    }

    [Fact]
    public void TryParse_MinimalQuery_SucceedsAndRoundTrips()
    {
        const string raw = "sort=Title&direction=Ascending&page=1&pageSize=20";

        MovieQueryString.TryParse(raw, out var query).ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Sort.ShouldBe(MovieSort.Title);
        query.Direction.ShouldBe(SortDirection.Ascending);
        query.Page.ShouldBe(new Page(1, 20));
        MovieQueryString.TryParse(MovieQueryString.Format(query), out var again).ShouldBeTrue();
        again.ShouldBe(query);
    }

    [Fact]
    public void TryParse_Fails_WhenRuntimeIncludeUnknownIsEmpty()
    {
        MovieQueryString.TryParse(
            WithRequiredQuery("runtimeMin=1&runtimeMax=2&runtimeIncludeUnknown="),
            out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RuntimeRangeIsPartialCases))]
    public void TryParse_Fails_WhenRuntimeRangeIsPartial(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(YearRangeIsPartialCases))]
    public void TryParse_Fails_WhenYearRangeIsPartial(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RequiredKeyIsMissingCases))]
    public void TryParse_Fails_WhenRequiredKeyIsMissing(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_UnknownKey_IsIgnored()
    {
        MovieQueryString.TryParse("noise=1&sort=Title&direction=Ascending&page=1&pageSize=20", out var query).ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Text.ShouldBeNull();
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
    public void TryParse_EmptyNonRangeValue_ReturnsFalse()
    {
        MovieQueryString.TryParse("sort=&direction=Ascending&page=1&pageSize=20", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_EmptyText_IsEmptyString()
    {
        MovieQueryString.TryParse("text=&sort=Title&direction=Ascending&page=1&pageSize=20", out var query).ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Text.ShouldBe(string.Empty);
    }

    [Theory]
    [MemberData(nameof(HalfBoundedRangesCases))]
    public void TryParse_RoundTrips_HalfBoundedRanges(MovieQuery query)
    {
        var formatted = MovieQueryString.Format(query);

        MovieQueryString.TryParse(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(query);
    }

    [Fact]
    public void TryParse_AbsentRatingGroup_LeavesRatingNull()
    {
        MovieQueryString.TryParse(WithRequiredQuery("text=matrix"), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.Rating.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyRatingBounds_PreservesPresentNullBounds()
    {
        MovieQueryString.TryParse(WithRequiredQuery("minRating=&maxRating="), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.Rating.ShouldBe(new RatingRange(null, null));
    }

    [Theory]
    [MemberData(nameof(RatingRangeIsPartialCases))]
    public void TryParse_Fails_WhenRatingRangeIsPartial(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RatingLexicalRejectCases))]
    public void TryParse_Fails_WhenRatingBoundIsLexicallyRejected(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RatingOutOfDomainCases))]
    public void TryParse_Fails_WhenRatingBoundIsOutOfDomain(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RatingInvertedCases))]
    public void TryParse_Fails_WhenRatingBoundsAreInverted(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RatingDuplicateCases))]
    public void TryParse_Fails_WhenRatingKeyIsDuplicated(string raw)
    {
        MovieQueryString.TryParse(raw, out _).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RatingEquivalentFormCases))]
    public void TryParse_EquivalentRatingForms_ParseToSameRange(string min, string max)
    {
        MovieQueryString.TryParse(WithRequiredQuery($"minRating={min}&maxRating={max}"), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.Rating.ShouldBe(new RatingRange(7m, 8m));
    }

    [Theory]
    [MemberData(nameof(RatingRangesCases))]
    public void Format_RatingRange_RoundTrips(MovieQuery query)
    {
        var formatted = MovieQueryString.Format(query);

        MovieQueryString.TryParse(formatted, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(query);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Format_RoundTripsRatingQueries()
    {
        // arrange
        var ratings = MovieQueryFixture.RatingQueries();

        // act
        var holds = Prop.ForAll(ratings, query =>
        {
            var formatted = MovieQueryString.Format(query);
            return MovieQueryString.TryParse(formatted, out var parsed) && query.Equals(parsed);
        });

        // assert
        holds.QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Format_RoundTripsScalarQueries()
    {
        // arrange
        var scalars = MovieQueryFixture.ScalarQueries();

        // act
        var holds = Prop.ForAll(scalars, query =>
        {
            var formatted = MovieQueryString.Format(query);
            return MovieQueryString.TryParse(formatted, out var parsed) && query.Equals(parsed);
        });

        // assert
        holds.QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Format_RoundTripsRangeQueries()
    {
        // arrange
        var ranges = MovieQueryFixture.RangeQueries();

        // act
        var holds = Prop.ForAll(ranges, query =>
        {
            var formatted = MovieQueryString.Format(query);
            return MovieQueryString.TryParse(formatted, out var parsed) && query.Equals(parsed);
        });

        // assert
        holds.QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Format_NonNullDirectorId_EmitsDirectorId()
    {
        var formatted = MovieQueryString.Format(Valid(directorId: 42));

        formatted.ShouldContain("directorId=42");
    }

    [Fact]
    public void Format_NullDirectorId_OmitsDirectorIdKey()
    {
        var formatted = MovieQueryString.Format(Valid());

        formatted.ShouldNotContain("directorId=");
    }

    [Fact]
    public void TryParse_ValidDirectorId_RoundTrips()
    {
        MovieQueryString.TryParse(WithRequiredQuery("directorId=42"), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.DirectorId.ShouldBe(42);
        MovieQueryString.TryParse(MovieQueryString.Format(parsed), out var again).ShouldBeTrue();
        again.ShouldBe(parsed);
    }

    [Fact]
    public void TryParse_AbsentDirectorId_LeavesNull()
    {
        MovieQueryString.TryParse(WithRequiredQuery("text=matrix"), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.DirectorId.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyDirectorId_IsNull()
    {
        MovieQueryString.TryParse(WithRequiredQuery("directorId="), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.DirectorId.ShouldBeNull();
    }

    [Theory]
    [InlineData("directorId=1&directorId=2")]
    [InlineData("directorId=&directorId=")]
    public void TryParse_DuplicateDirectorId_ReturnsFalse(string raw)
    {
        MovieQueryString.TryParse(WithRequiredQuery(raw), out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    public void TryParse_MalformedDirectorId_ReturnsFalse(string raw)
    {
        MovieQueryString.TryParse(WithRequiredQuery($"directorId={raw}"), out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    public void TryParse_OverflowingDirectorId_ReturnsFalse(string raw)
    {
        MovieQueryString.TryParse(WithRequiredQuery($"directorId={raw}"), out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("999999")]
    public void TryParse_UnknownZeroOrNegativeDirectorId_ParsesWithoutValidation(string raw)
    {
        MovieQueryString.TryParse(WithRequiredQuery($"directorId={raw}"), out var parsed).ShouldBeTrue();

        parsed.ShouldNotBeNull();
        parsed.DirectorId.ShouldBe(int.Parse(raw, CultureInfo.InvariantCulture));
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Format_RoundTripsDirectorQueries()
    {
        // arrange
        var directors = MovieQueryFixture.DirectorQueries();

        // act
        var holds = Prop.ForAll(directors, query =>
        {
            var formatted = MovieQueryString.Format(query);
            return MovieQueryString.TryParse(formatted, out var parsed)
                && query.Equals(parsed)
                && parsed.DirectorId == query.DirectorId;
        });

        // assert
        holds.QuickCheckThrowOnFailure();
    }

    public static TheoryData<string> RuntimeRangeIsPartialCases => new()
    {
        WithRequiredQuery("runtimeMin=1"),
        WithRequiredQuery("runtimeMax=2"),
        WithRequiredQuery("runtimeIncludeUnknown=true"),
        WithRequiredQuery("runtimeMin=1&runtimeMax=2"),
        WithRequiredQuery("runtimeMin=1&runtimeIncludeUnknown=true"),
        WithRequiredQuery("runtimeMax=2&runtimeIncludeUnknown=false"),
    };

    public static TheoryData<string> YearRangeIsPartialCases => new()
    {
        WithRequiredQuery("yearMin=1990"),
        WithRequiredQuery("yearMax=1999"),
    };

    public static TheoryData<string> RequiredKeyIsMissingCases => new()
    {
        "direction=Ascending&page=1&pageSize=20",
        "sort=Title&page=1&pageSize=20",
        "sort=Title&direction=Ascending&pageSize=20",
        "sort=Title&direction=Ascending&page=1",
    };

    public static TheoryData<MovieQuery> HalfBoundedRangesCases => new()
    {
        Valid(runtime: new RuntimeRange(5, null, true)),
        Valid(runtime: new RuntimeRange(null, 5, false)),
        Valid(year: new YearRange(2000, null)),
        Valid(year: new YearRange(null, 2000)),
    };

    public static TheoryData<string> RatingRangeIsPartialCases => new()
    {
        WithRequiredQuery("minRating=7"),
        WithRequiredQuery("maxRating=7"),
        WithRequiredQuery("minRating="),
        WithRequiredQuery("maxRating="),
    };

    public static TheoryData<string> RatingLexicalRejectCases =>
        Rejects("+7", "-0", "1e1", " 7", "7 ", "7,0", "7.", ".5", "7.00", "7.10", "NaN");

    public static TheoryData<string> RatingOutOfDomainCases =>
        Rejects("10.1", "11", "999");

    public static TheoryData<string> RatingInvertedCases => new()
    {
        WithRequiredQuery("minRating=8&maxRating=7"),
        WithRequiredQuery("minRating=7.5&maxRating=7.4"),
    };

    public static TheoryData<string> RatingDuplicateCases => new()
    {
        WithRequiredQuery("minRating=7&minRating=8&maxRating="),
        WithRequiredQuery("minRating=7&maxRating=8&maxRating=9"),
        WithRequiredQuery("minRating=&maxRating=&minRating="),
    };

    public static TheoryData<string, string> RatingEquivalentFormCases => new()
    {
        { "7", "8" },
        { "7.0", "8.0" },
        { "07.0", "08.0" },
    };

    public static TheoryData<MovieQuery> RatingRangesCases => new()
    {
        Valid(rating: new RatingRange(7m, null)),
        Valid(rating: new RatingRange(null, 7m)),
        Valid(rating: new RatingRange(null, null)),
        Valid(rating: new RatingRange(0m, 10m)),
        Valid(rating: new RatingRange(7.5m, 7.5m)),
        Valid(rating: new RatingRange(0.1m, 9.9m)),
    };

    private static TheoryData<string> Rejects(params string[] bounds)
    {
        var cases = new TheoryData<string>();
        foreach (var bound in bounds)
        {
            cases.Add(WithRequiredQuery($"minRating={bound}&maxRating="));
            cases.Add(WithRequiredQuery($"minRating=&maxRating={bound}"));
        }

        return cases;
    }

    private static MovieQuery Valid(
        string? text = null,
        int[]? genres = null,
        RuntimeRange? runtime = null,
        YearRange? year = null,
        RatingRange? rating = null,
        int? directorId = null) =>
        new()
        {
            Text = text,
            GenreIds = genres is null ? [] : [.. genres],
            DirectorId = directorId,
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 20),
            Runtime = runtime,
            Year = year,
            Rating = rating,
            Statuses = [EnrichmentStatus.Pending],
        };

    private static string WithRequiredQuery(string raw) =>
        $"{raw}&sort=Title&direction=Ascending&page=1&pageSize=20";

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool CanonicalTenthStep_ParsesToExpectedBound(int tenth)
    {
        var normalized = Math.Abs(tenth) % 101;
        var expected = normalized / 10m;
        var raw = expected.ToString("0.0", CultureInfo.InvariantCulture);

        return MovieQueryString.TryParseRatingBound(raw, out var bound)
            && bound == expected;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool AcceptedBound_AgreesWithImdbRating(decimal candidate)
    {
        var raw = candidate.ToString("0.0####", CultureInfo.InvariantCulture);

        if (!MovieQueryString.TryParseRatingBound(raw, out var bound))
        {
            return true;
        }

        return bound is not null
            && ImdbRating.TryCreate(bound.Value, out _)
            && bound.Value.ToString("0.0", CultureInfo.InvariantCulture) == raw;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool NullOrEmpty_RatingBound_ParsesToNull(string? raw)
    {
        if (!string.IsNullOrEmpty(raw))
        {
            return true;
        }

        return MovieQueryString.TryParseRatingBound(raw, out var bound) && bound is null;
    }
}
