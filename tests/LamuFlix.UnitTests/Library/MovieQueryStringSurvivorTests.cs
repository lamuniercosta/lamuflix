using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class MovieQueryStringSurvivorTests
{
    private const string Required = "sort=Title&direction=Ascending&page=1&pageSize=20";

    [Fact]
    public void Format_NullQuery_ThrowsArgumentNullException()
    {
        // arrange
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => MovieQueryString.Format(null!));

        // assert
        thrown.ParamName.ShouldBe("query");
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalse()
    {
        // arrange
        // act
        var parsed = MovieQueryString.TryParse(null, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        // arrange
        // act
        var parsed = MovieQueryString.TryParse(string.Empty, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_DuplicateText_ReturnsFalse()
    {
        // arrange
        var raw = $"text=first&text=second&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_ValidStatus_AddsStatus()
    {
        // arrange
        var raw = $"statuses=Failed&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Statuses.ShouldBe([EnrichmentStatus.Failed]);
    }

    [Fact]
    public void TryParse_RepeatedStatuses_KeepsEachInOrder()
    {
        // arrange
        var raw = $"statuses=Pending&statuses=NotFound&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Statuses.ShouldBe([EnrichmentStatus.Pending, EnrichmentStatus.NotFound]);
    }

    [Fact]
    public void TryParse_UnknownStatusName_ReturnsFalse()
    {
        // arrange
        var raw = $"statuses=Bogus&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyStatus_ReturnsFalse()
    {
        // arrange
        var raw = $"statuses=&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_ValidDirection_SetsDirection()
    {
        // arrange
        var raw = "sort=Year&direction=Descending&page=1&pageSize=20";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Direction.ShouldBe(SortDirection.Descending);
    }

    [Fact]
    public void TryParse_DuplicateValidDirection_ReturnsFalse()
    {
        // arrange
        var raw = "sort=Title&direction=Ascending&direction=Descending&page=1&pageSize=20";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_InvalidDirectionName_ReturnsFalse()
    {
        // arrange
        var raw = "sort=Title&direction=Sideways&page=1&pageSize=20";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_DuplicatePage_ReturnsFalse()
    {
        // arrange
        var raw = "sort=Title&direction=Ascending&page=1&page=2&pageSize=20";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyPageSize_ReturnsFalse()
    {
        // arrange
        var raw = "sort=Title&direction=Ascending&page=1&pageSize=";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_DuplicateYearMin_ReturnsFalse()
    {
        // arrange
        var raw = $"yearMin=1990&yearMin=2000&yearMax=2010&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_NonIntegerRuntimeMin_ReturnsFalse()
    {
        // arrange
        var raw = $"runtimeMin=abc&runtimeMax=120&runtimeIncludeUnknown=true&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyRuntimeBounds_ParsesAsOpenRange()
    {
        // arrange
        var raw = $"runtimeMin=&runtimeMax=&runtimeIncludeUnknown=false&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeTrue();
        query.ShouldNotBeNull();
        query.Runtime.ShouldBe(new RuntimeRange(null, null, false));
    }

    [Fact]
    public void TryParse_DuplicateInWatchlist_ReturnsFalse()
    {
        // arrange
        var raw = $"inWatchlist=true&inWatchlist=false&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_ValidInWatchlist_SetsFlag()
    {
        // arrange
        var raw = $"inWatchlist=false&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeTrue();
        query.ShouldNotBeNull();
        query.InWatchlist.ShouldBe(false);
    }

    [Fact]
    public void TryParse_SegmentWithoutEquals_ReturnsFalse()
    {
        // arrange
        var raw = $"{Required}&bogus";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }

    [Fact]
    public void TryParse_SegmentStartingWithEquals_ReturnsFalse()
    {
        // arrange
        var raw = $"=Title&{Required}";

        // act
        var parsed = MovieQueryString.TryParse(raw, out var query);

        // assert
        parsed.ShouldBeFalse();
        query.ShouldBeNull();
    }
}
