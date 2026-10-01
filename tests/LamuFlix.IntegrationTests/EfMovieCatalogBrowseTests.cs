using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;
using SortDirection = LamuFlix.Core.Library.SortDirection;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieCatalogBrowseTests(PostgresFixture fixture)
{
    [Theory]
    [MemberData(nameof(TextCases))]
    public async Task WhereText_TextCondition_ReturnsLiteralCaseInsensitiveMatches(string? text, string[] titles)
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var movies = new[]
        {
            MovieCatalogSeed.Create("Movie", "text-movie"),
            MovieCatalogSeed.Create("Movie Night", "text-night"),
            MovieCatalogSeed.Create("A MOVIE", "text-case"),
            MovieCatalogSeed.Create("100%\\file_name_v2", "text-combined"),
            MovieCatalogSeed.Create("Only\\%Adjacent", "text-adjacent"),
            MovieCatalogSeed.Create("Metadata Elsewhere", "text-metadata")
        };
        movies[^1].MetadataTitle = "Search This Metadata";
        context.Movies.AddRange(movies);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var actual = await context.Movies.WhereText(text).Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);

        // assert
        actual.Order().ShouldBe(titles.Order());
    }

    public static TheoryData<string?, string[]> TextCases => new()
    {
        { "movie", ["Movie", "Movie Night", "A MOVIE"] },
        { "Night", ["Movie Night"] },
        { "Movie ", ["Movie Night"] },
        { "%", ["100%\\file_name_v2", "Only\\%Adjacent"] },
        { "_", ["100%\\file_name_v2"] },
        { "\\", ["100%\\file_name_v2", "Only\\%Adjacent"] },
        { "100%\\file_name_v2", ["100%\\file_name_v2"] },
        { "\\%", ["Only\\%Adjacent"] },
        { null, ["Movie", "Movie Night", "A MOVIE", "100%\\file_name_v2", "Only\\%Adjacent", "Metadata Elsewhere"] },
        { "", ["Movie", "Movie Night", "A MOVIE", "100%\\file_name_v2", "Only\\%Adjacent", "Metadata Elsewhere"] },
        { "   ", ["Movie", "Movie Night", "A MOVIE", "100%\\file_name_v2", "Only\\%Adjacent", "Metadata Elsewhere"] },
        { "Search This Metadata", [] }
    };

    [Fact]
    public async Task WhereGenres_MultipleAndRepeatedIds_ReturnsEachMatchingMovieOnce()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var firstGenre = new GenreRecord { Name = "Drama" };
        var secondGenre = new GenreRecord { Name = "Comedy" };
        var both = MovieCatalogSeed.Create("Both", "genres-both");
        both.Genres.Add(firstGenre);
        both.Genres.Add(secondGenre);
        var onlyOne = MovieCatalogSeed.Create("One", "genres-one");
        onlyOne.Genres.Add(firstGenre);
        var unrelated = MovieCatalogSeed.Create("None", "genres-none");
        context.Movies.AddRange(both, onlyOne, unrelated);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var matching = await context.Movies.WhereGenres([firstGenre.Id, secondGenre.Id, firstGenre.Id])
            .Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);
        var empty = await context.Movies.WhereGenres([]).CountAsync(TestContext.Current.CancellationToken);
        var defaultIds = await context.Movies.WhereGenres(default).CountAsync(TestContext.Current.CancellationToken);

        // assert
        matching.Order().ShouldBe(new[] { "Both", "One" });
        empty.ShouldBe(3);
        defaultIds.ShouldBe(3);
    }

    [Fact]
    public async Task WhereActors_MultipleAndRepeatedIds_ReturnsEachMatchingMovieOnce()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var firstActor = new ActorRecord { Name = "Ada" };
        var secondActor = new ActorRecord { Name = "Bert" };
        var both = MovieCatalogSeed.Create("Both", "actors-both");
        both.Actors.Add(firstActor);
        both.Actors.Add(secondActor);
        var one = MovieCatalogSeed.Create("One", "actors-one");
        one.Actors.Add(firstActor);
        context.Movies.AddRange(both, one, MovieCatalogSeed.Create("None", "actors-none"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var matching = await context.Movies.WhereActors([firstActor.Id, secondActor.Id, firstActor.Id])
            .Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);
        var empty = await context.Movies.WhereActors([]).CountAsync(TestContext.Current.CancellationToken);
        var defaultIds = await context.Movies.WhereActors(default).CountAsync(TestContext.Current.CancellationToken);

        // assert
        matching.Order().ShouldBe(new[] { "Both", "One" });
        empty.ShouldBe(3);
        defaultIds.ShouldBe(3);
    }

    [Theory]
    [MemberData(nameof(RuntimeCases))]
    public async Task WhereRuntime_RangeCondition_ReturnsInclusiveBoundMatches(int? min, int? max, bool includeUnknown, string[] expected)
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var unknown = MovieCatalogSeed.Create("Unknown", "runtime-unknown");
        var low = MovieCatalogSeed.Create("Low", "runtime-low");
        low.RuntimeMinutes = new Runtime(99);
        var edge = MovieCatalogSeed.Create("Edge", "runtime-edge");
        edge.RuntimeMinutes = new Runtime(100);
        var high = MovieCatalogSeed.Create("High", "runtime-high");
        high.RuntimeMinutes = new Runtime(101);
        context.Movies.AddRange(unknown, low, edge, high);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var query = context.Movies.WhereRuntime(new RuntimeRange(min, max, includeUnknown));
        var actual = await query.Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);
        var inactive = await context.Movies.WhereRuntime(null).CountAsync(TestContext.Current.CancellationToken);

        // assert
        actual.Order().ShouldBe(expected.Order());
        inactive.ShouldBe(4);
    }

    public static TheoryData<int?, int?, bool, string[]> RuntimeCases => new()
    {
        { 100, null, false, ["Edge", "High"] },
        { null, 100, false, ["Low", "Edge"] },
        { 100, 100, false, ["Edge"] },
        { 100, null, true, ["Unknown", "Edge", "High"] },
        { null, 100, true, ["Unknown", "Low", "Edge"] },
        { null, null, false, ["Low", "Edge", "High"] },
        { null, null, true, ["Unknown", "Low", "Edge", "High"] }
    };

    [Theory]
    [MemberData(nameof(YearCases))]
    public async Task WhereYear_RangeCondition_ReturnsInclusiveBoundMatches(int? min, int? max, string[] expected)
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var unknown = MovieCatalogSeed.Create("Unknown", "year-unknown");
        var low = MovieCatalogSeed.Create("Low", "year-low");
        low.ReleaseYear = new ReleaseYear(1999, MovieCatalogSeed.FixedTime);
        var edge = MovieCatalogSeed.Create("Edge", "year-edge");
        edge.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var high = MovieCatalogSeed.Create("High", "year-high");
        high.ReleaseYear = new ReleaseYear(2001, MovieCatalogSeed.FixedTime);
        context.Movies.AddRange(unknown, low, edge, high);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var actual = await context.Movies.WhereYear(new YearRange(min, max))
            .Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);
        var inactive = await context.Movies.WhereYear(null).CountAsync(TestContext.Current.CancellationToken);

        // assert
        actual.Order().ShouldBe(expected.Order());
        inactive.ShouldBe(4);
    }

    public static TheoryData<int?, int?, string[]> YearCases => new()
    {
        { 2000, null, ["Edge", "High"] },
        { null, 2000, ["Low", "Edge"] },
        { 2000, 2000, ["Edge"] },
        { null, null, ["Low", "Edge", "High"] }
    };

    [Fact]
    public async Task WhereStatuses_StatusCondition_ReturnsAnyStatusAndLeavesEmptyUnfiltered()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var pending = MovieCatalogSeed.Create("Pending", "status-pending");
        var enriched = MovieCatalogSeed.Create("Enriched", "status-enriched");
        enriched.Status = EnrichmentStatus.Enriched;
        context.Movies.AddRange(pending, enriched);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var selected = await context.Movies.WhereStatuses([EnrichmentStatus.Pending, EnrichmentStatus.Enriched])
            .CountAsync(TestContext.Current.CancellationToken);
        var empty = await context.Movies.WhereStatuses([]).CountAsync(TestContext.Current.CancellationToken);
        var defaultStatuses = await context.Movies.WhereStatuses(default).CountAsync(TestContext.Current.CancellationToken);

        // assert
        selected.ShouldBe(2);
        empty.ShouldBe(2);
        defaultStatuses.ShouldBe(2);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public async Task WhereInWatchlist_WatchlistCondition_ReturnsExpectedMovies(bool? filter, int expectedCount)
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var watchlisted = MovieCatalogSeed.Create("Yes", "watchlist-yes");
        watchlisted.IsInWatchlist = true;
        context.Movies.AddRange(watchlisted, MovieCatalogSeed.Create("No", "watchlist-no"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var count = await context.Movies.WhereInWatchlist(filter).CountAsync(TestContext.Current.CancellationToken);

        // assert
        count.ShouldBe(expectedCount);
    }

    [Fact]
    public async Task BrowseAsync_MultipleFilters_AppliesAndAcrossFilters()
    {
        // arrange
        await using var context = fixture.CreateContext();
        await MovieCatalogSeed.ResetAsync(context, TestContext.Current.CancellationToken);
        var genre = new GenreRecord { Name = "Drama" };
        var actor = new ActorRecord { Name = "Actor" };
        var match = MovieCatalogSeed.Create("Matching Film", "combine-match");
        match.Genres.Add(genre);
        match.Actors.Add(actor);
        match.RuntimeMinutes = new Runtime(110);
        match.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        match.IsInWatchlist = true;
        match.Status = EnrichmentStatus.Enriched;
        var titleOnly = MovieCatalogSeed.Create("Matching Film", "combine-title");
        titleOnly.Genres.Add(genre);
        titleOnly.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        context.Movies.AddRange(match, titleOnly);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);
        var request = new MovieQuery
        {
            Text = "Matching",
            GenreIds = [genre.Id],
            ActorIds = [actor.Id],
            Runtime = new RuntimeRange(100, 120, false),
            Year = new YearRange(1999, 2001),
            Statuses = [EnrichmentStatus.Enriched],
            InWatchlist = true,
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 10)
        };

        // act
        var result = await catalog.BrowseAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.Items.Select(item => item.Title).ShouldBe(["Matching Film"]);
    }
}