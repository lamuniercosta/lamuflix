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

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieCatalogBrowseTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(TextCases))]
    public async Task WhereText_TextCondition_ReturnsLiteralCaseInsensitiveMatches(string? text, string[] titles)
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
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
        await using var context = fixture.CreateMigratedContext();
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
        await using var context = fixture.CreateMigratedContext();
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
        await using var context = fixture.CreateMigratedContext();
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
        await using var context = fixture.CreateMigratedContext();
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

    [Theory]
    [MemberData(nameof(RatingCases))]
    public async Task WhereRating_RangeCondition_ReturnsInclusiveRatedMatches(decimal? min, decimal? max, string[] expected)
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(
            MovieCatalogSeed.Create("Unrated", "rating-unrated"),
            Rated("Zero", "rating-zero", 0.0m),
            Rated("Low", "rating-low", 6.9m),
            Rated("Edge", "rating-edge", 7.0m),
            Rated("Fraction", "rating-fraction", 7.1m),
            Rated("High", "rating-high", 8.5m),
            Rated("Top", "rating-top", 10.0m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var actual = await context.Movies.WhereRating(new RatingRange(min, max))
            .Select(movie => movie.Title).ToArrayAsync(TestContext.Current.CancellationToken);
        var inactive = await context.Movies.WhereRating(null).CountAsync(TestContext.Current.CancellationToken);

        // assert
        actual.Order().ShouldBe(expected.Order());
        inactive.ShouldBe(7);
    }

    public static TheoryData<decimal?, decimal?, string[]> RatingCases => new()
    {
        { 7m, null, ["Edge", "Fraction", "High", "Top"] },
        { null, 7m, ["Zero", "Low", "Edge"] },
        { 7m, 7m, ["Edge"] },
        { 7.1m, 7.1m, ["Fraction"] },
        { 0m, 10m, ["Zero", "Low", "Edge", "Fraction", "High", "Top"] },
        { null, null, ["Zero", "Low", "Edge", "Fraction", "High", "Top"] },
        { 9m, 10m, ["Top"] },
        { 0m, 0m, ["Zero"] },
        { 7.05m, 8m, [] },
        { -0.1m, null, [] }
    };

    [Theory]
    [MemberData(nameof(InvalidRatingCases))]
    public async Task WhereRating_InvalidOrInvertedBounds_ReturnsEmpty(decimal? min, decimal? max)
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(
            Rated("Low", "rating-invalid-low", 6.9m),
            Rated("Edge", "rating-invalid-edge", 7.0m),
            Rated("High", "rating-invalid-high", 8.5m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // act
        var actual = await context.Movies.WhereRating(new RatingRange(min, max))
            .CountAsync(TestContext.Current.CancellationToken);

        // assert
        actual.ShouldBe(0);
    }

    public static TheoryData<decimal?, decimal?> InvalidRatingCases => new()
    {
        { -0.1m, null },
        { null, 10.1m },
        { 7.05m, null },
        { null, 6.99m },
        { 8m, 7m },
        { 5m, 5.5m },
        { 10.1m, 10.1m }
    };

    [Fact]
    public async Task BrowseAsync_RatingPresent_ExcludesUnratedAndRetainsThemWhenAbsent()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(
            MovieCatalogSeed.Create("Unrated", "rating-unrated-browse"),
            Rated("Rated", "rating-rated-browse", 8.0m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var withRange = await catalog.BrowseAsync(
            new MovieQuery { Rating = new RatingRange(7m, null), Sort = MovieSort.Title, Direction = SortDirection.Ascending, Page = new Page(1, 10) },
            TestContext.Current.CancellationToken);
        var withoutRange = await catalog.BrowseAsync(
            new MovieQuery { Sort = MovieSort.Title, Direction = SortDirection.Ascending, Page = new Page(1, 10) },
            TestContext.Current.CancellationToken);

        // assert
        var ratedOnly = withRange.Items.ShouldHaveSingleItem();
        ratedOnly.Title.ShouldBe("Rated");
        withRange.TotalCount.ShouldBe(1);
        withoutRange.TotalCount.ShouldBe(2);
        withoutRange.Items.Select(item => item.Title).ShouldBe(["Rated", "Unrated"]);
    }

    [Fact]
    public async Task BrowseAsync_InvalidRatingBounds_ReturnsDefensiveEmptyPage()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(Rated("High", "rating-defensive-high", 8.5m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var result = await catalog.BrowseAsync(
            new MovieQuery { Rating = new RatingRange(9m, 8m), Sort = MovieSort.Title, Direction = SortDirection.Ascending, Page = new Page(1, 10) },
            TestContext.Current.CancellationToken);

        // assert
        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseAsync_RatingWithOtherFilters_AppliesAndAcrossFilters()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var genre = new GenreRecord { Name = "Drama" };
        var match = Rated("Matching Film", "rating-combine-match", 8.1m);
        match.Genres.Add(genre);
        var ratingDecoy = Rated("Matching Film", "rating-combine-rating", 6.5m);
        ratingDecoy.Genres.Add(genre);
        var genreDecoy = Rated("Matching Film", "rating-combine-genre", 8.1m);
        var unratedDecoy = MovieCatalogSeed.Create("Matching Film", "rating-combine-unrated");
        unratedDecoy.Genres.Add(genre);
        context.Movies.AddRange(match, ratingDecoy, genreDecoy, unratedDecoy);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);
        var request = new MovieQuery
        {
            Text = "Matching",
            GenreIds = [genre.Id],
            Rating = new RatingRange(8m, 9m),
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 10)
        };

        // act
        var result = await catalog.BrowseAsync(request, TestContext.Current.CancellationToken);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Title.ShouldBe("Matching Film");
        item.ImdbRating.ShouldBe(8.1m);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task BrowseAsync_RatingRange_CountsFilteredSetBeforePaging()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(
            Rated("Alpha", "rating-paging-alpha", 7.0m),
            Rated("Beta", "rating-paging-beta", 8.0m),
            Rated("Gamma", "rating-paging-gamma", 9.0m),
            Rated("Delta", "rating-paging-delta", 6.0m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);
        var request = new MovieQuery
        {
            Rating = new RatingRange(7m, null),
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 2)
        };

        // act
        var result = await catalog.BrowseAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.TotalCount.ShouldBe(3);
        result.Items.Select(item => item.Title).ShouldBe(["Alpha", "Beta"]);
    }

    private static MovieRecord Rated(string title, string pathSuffix, decimal rating)
    {
        var movie = MovieCatalogSeed.Create(title, pathSuffix);
        movie.ImdbRating = new ImdbRating(rating);
        return movie;
    }

    [Fact]
    public async Task WhereStatuses_StatusCondition_ReturnsAnyStatusAndLeavesEmptyUnfiltered()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
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
        await using var context = fixture.CreateMigratedContext();
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
        await using var context = fixture.CreateMigratedContext();
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
        var item = result.Items.ShouldHaveSingleItem();
        item.Title.ShouldBe("Matching Film");
        item.EnrichmentStatus.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task BrowseAsync_TextGenreYearOnly_AppliesAndAcrossActiveFilters()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var genre = new GenreRecord { Name = "Drama" };
        var match = MovieCatalogSeed.Create("Matching Film", "combine2-match");
        match.Genres.Add(genre);
        match.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var textDecoy = MovieCatalogSeed.Create("Unrelated", "combine2-text");
        textDecoy.Genres.Add(genre);
        textDecoy.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var genreDecoy = MovieCatalogSeed.Create("Matching Film", "combine2-genre");
        genreDecoy.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        var yearDecoy = MovieCatalogSeed.Create("Matching Film", "combine2-year");
        yearDecoy.Genres.Add(genre);
        yearDecoy.ReleaseYear = new ReleaseYear(1990, MovieCatalogSeed.FixedTime);
        context.Movies.AddRange(match, textDecoy, genreDecoy, yearDecoy);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);
        var request = new MovieQuery
        {
            Text = "Matching",
            GenreIds = [genre.Id],
            Year = new YearRange(1999, 2001),
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 10)
        };

        // act
        var result = await catalog.BrowseAsync(request, TestContext.Current.CancellationToken);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Title.ShouldBe("Matching Film");
        item.EnrichmentStatus.ShouldBe(EnrichmentStatus.Pending);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task BrowseAsync_Projection_ReturnsPopulatedAndNullGridScalars()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var populated = MovieCatalogSeed.Create("Populated", "browse-projection-populated");
        populated.PosterUrl = "https://example.test/poster.jpg";
        populated.RuntimeMinutes = new Runtime(110);
        populated.ReleaseYear = new ReleaseYear(2000, MovieCatalogSeed.FixedTime);
        populated.ImdbRating = new ImdbRating(8.3m);
        populated.Status = EnrichmentStatus.Failed;
        var empty = MovieCatalogSeed.Create("Empty", "browse-projection-empty");
        empty.Status = EnrichmentStatus.Pending;
        context.Movies.AddRange(populated, empty);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);
        var request = new MovieQuery
        {
            Sort = MovieSort.Title,
            Direction = SortDirection.Ascending,
            Page = new Page(1, 10)
        };

        // act
        var result = await catalog.BrowseAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.TotalCount.ShouldBe(2);
        var pop = result.Items.Single(item => item.Title == "Populated");
        pop.Id.ShouldBe(populated.Id);
        pop.PosterUrl.ShouldBe("https://example.test/poster.jpg");
        pop.ReleaseYear.ShouldBe(2000);
        pop.Runtime.ShouldBe(110);
        pop.ImdbRating.ShouldBe(8.3m);
        pop.EnrichmentStatus.ShouldBe(EnrichmentStatus.Failed);
        var emptyItem = result.Items.Single(item => item.Title == "Empty");
        emptyItem.PosterUrl.ShouldBeNull();
        emptyItem.ReleaseYear.ShouldBeNull();
        emptyItem.Runtime.ShouldBeNull();
        emptyItem.ImdbRating.ShouldBeNull();
        emptyItem.EnrichmentStatus.ShouldBe(EnrichmentStatus.Pending);
    }
}