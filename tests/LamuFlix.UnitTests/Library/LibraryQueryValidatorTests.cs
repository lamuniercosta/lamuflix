using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class LibraryQueryValidatorTests
{
    private readonly BrowseMoviesQueryValidator browseValidator = new();
    private readonly GetMovieDetailsQueryValidator detailsValidator = new();
    private readonly MovieQueryValidator movieQueryValidator = new();

    [Fact]
    public void BrowseValidator_AcceptedMovieQuery_IsValid()
    {
        browseValidator.Validate(new BrowseMoviesQuery(Accepted() with { Page = new Page(1, 100) }))
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public void BrowseValidator_ReportsExactlyTheNestedMovieQueryFailures()
    {
        var query = new MovieQuery { Page = new Page(1, 101) };

        var inner = movieQueryValidator.Validate(query);
        var result = browseValidator.Validate(new BrowseMoviesQuery(query));

        inner.IsValid.ShouldBeFalse();
        result.IsValid.ShouldBe(inner.IsValid);
        result.Errors.Select(error => error.PropertyName)
            .ShouldBe(inner.Errors.Select(error => $"Query.{error.PropertyName}"));
    }

    [Fact]
    public void BrowseValidator_MissingSortAndDirection_FailsBothNestedProperties()
    {
        var result = browseValidator.Validate(new BrowseMoviesQuery(new MovieQuery { Page = new Page(1, 20) }));

        result.Errors.Select(error => error.PropertyName).ShouldBe(["Query.Sort", "Query.Direction"]);
    }

    [Fact]
    public void BrowseValidator_InvertedYearRange_FailsTheNestedYearProperty()
    {
        var query = Accepted() with { Year = new YearRange(2000, 1999) };

        browseValidator.Validate(new BrowseMoviesQuery(query)).Errors
            .ShouldContain(error => error.PropertyName == "Query.Year");
    }

    [Fact]
    public void GetMovieDetailsValidator_NullId_IsInvalid()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed
        // The Id-not-null rule under test can only be exercised with a null Id.
        var query = new GetMovieDetailsQuery(null!);

        // act
        var result = detailsValidator.Validate(query);

        // assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldBe(["Id"]);
    }

    [Fact]
    public void GetMovieDetailsValidator_ConstructedId_IsValid()
    {
        detailsValidator.Validate(new GetMovieDetailsQuery(new MovieId(1))).IsValid.ShouldBeTrue();
    }

    private static MovieQuery Accepted() => new()
    {
        Sort = MovieSort.Title,
        Direction = SortDirection.Ascending,
        Page = new Page(1, 20),
    };
}
