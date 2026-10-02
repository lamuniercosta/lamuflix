using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;
using SortDirection = LamuFlix.Core.Library.SortDirection;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieCatalogSortingTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(SortCases))]
    public async Task OrderByMovieSort_SortAndDirection_ReturnsNullsLastAndExpectedOrder(MovieSort sort, SortDirection direction, string[] expected)
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var alpha = MovieCatalogSeed.Create("Alpha", "sort-alpha");
        var beta = MovieCatalogSeed.Create("Beta", "sort-beta");
        var nullKey = MovieCatalogSeed.Create("Zulu", "sort-null");
        foreach (var movie in new[] { alpha, beta })
        {
            movie.ReleaseYear = new ReleaseYear(movie == alpha ? 2000 : 2001, MovieCatalogSeed.FixedTime);
            movie.RuntimeMinutes = new Runtime(movie == alpha ? 90 : 100);
            movie.ImdbRating = new ImdbRating(movie == alpha ? 7.0m : 8.0m);
        }

        context.Movies.AddRange(alpha, beta, nullKey);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var actual = await context.Movies.OrderByMovieSort(sort, direction).Select(movie => movie.Title)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        // assert
        actual.ShouldBe(expected);
    }

    public static TheoryData<MovieSort, SortDirection, string[]> SortCases => new()
    {
        { MovieSort.Title, SortDirection.Ascending, ["Alpha", "Beta", "Zulu"] },
        { MovieSort.Title, SortDirection.Descending, ["Zulu", "Beta", "Alpha"] },
        { MovieSort.Year, SortDirection.Ascending, ["Alpha", "Beta", "Zulu"] },
        { MovieSort.Year, SortDirection.Descending, ["Beta", "Alpha", "Zulu"] },
        { MovieSort.Rating, SortDirection.Ascending, ["Alpha", "Beta", "Zulu"] },
        { MovieSort.Rating, SortDirection.Descending, ["Beta", "Alpha", "Zulu"] },
        { MovieSort.Runtime, SortDirection.Ascending, ["Alpha", "Beta", "Zulu"] },
        { MovieSort.Runtime, SortDirection.Descending, ["Beta", "Alpha", "Zulu"] }
    };

    [Fact]
    public async Task OrderByMovieSort_EqualYearAndTitle_UsesIdAsAscendingTieBreaker()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var first = MovieCatalogSeed.Create("Same", "tie-first");
        first.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var second = MovieCatalogSeed.Create("Same", "tie-second");
        second.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        context.Movies.AddRange(first, second);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var ordered = await context.Movies.OrderByMovieSort(MovieSort.Year, SortDirection.Descending)
            .Select(movie => movie.Id).ToArrayAsync(TestContext.Current.CancellationToken);

        // assert
        var firstId = first.Id;
        var secondId = second.Id;
        firstId.ShouldNotBeNull();
        secondId.ShouldNotBeNull();
        ordered.ShouldBe(new[] { firstId, secondId }.OrderBy(id => id.Value));
    }

    [Fact]
    public async Task OrderByMovieSort_EqualYear_UsesTitleAsSecondaryTieBreaker()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var bravo = MovieCatalogSeed.Create("Bravo", "tie-title-bravo");
        bravo.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var alpha = MovieCatalogSeed.Create("Alpha", "tie-title-alpha");
        alpha.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        context.Movies.AddRange(bravo, alpha);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var actual = await context.Movies.OrderByMovieSort(MovieSort.Year, SortDirection.Descending)
            .Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);

        // assert
        actual.ShouldBe(["Alpha", "Bravo"]);
    }
}