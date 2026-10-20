using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;

namespace LamuFlix.UnitTests.Persistence;

[Collection(nameof(CatalogMutationCollection))]
public sealed class EfMovieCatalogSurvivorTests(PostgresFixture fixture)
{
    [Fact]
    public async Task BrowseAsync_SecondPageOfTitleSort_SkipsFirstPageInAscendingOrder()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(
            Movie("Delta", "delta"),
            Movie("Alpha", "alpha"),
            Movie("Charlie", "charlie"),
            Movie("Bravo", "bravo"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var page = await catalog.BrowseAsync(Query(2, 2), TestContext.Current.CancellationToken);

        // assert
        page.TotalCount.ShouldBe(4);
        page.Items.Select(item => item.Title).ShouldBe(["Charlie", "Delta"]);
    }

    [Fact]
    public async Task GetDetailsAsync_TwoMovies_ReturnsTheRequestedMovie()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        context.Movies.AddRange(Movie("First", "first"), Movie("Second", "second"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var requested = context.Movies.Single(movie => movie.Title == "Second").Id;
        requested.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(requested, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Id.ShouldBe(requested);
        details.Title.ShouldBe("Second");
    }

    [Fact]
    public async Task GetDetailsAsync_MissingId_ReturnsNull()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(new MovieId(1), TestContext.Current.CancellationToken);

        // assert
        details.ShouldBeNull();
    }

    private static MovieQuery Query(int number, int size) => new()
    {
        Sort = MovieSort.Title,
        Direction = SortDirection.Ascending,
        Page = new Page(number, size)
    };

    [Fact]
    public async Task BrowseAsync_Projection_ReturnsPopulatedAndNullGridScalars()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var populated = Movie("Populated", "unit-projection-populated");
        populated.PosterUrl = "https://example.test/poster.jpg";
        populated.RuntimeMinutes = new Runtime(110);
        populated.ReleaseYear = new ReleaseYear(2000, new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero));
        populated.ImdbRating = new ImdbRating(8.3m);
        populated.Status = EnrichmentStatus.Failed;
        var empty = Movie("Empty", "unit-projection-empty");
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

    [Fact]
    public async Task GetDetailsAsync_NoMetadata_ReturnsNullMetadata()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("No metadata", "unit-details-none");
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_MetadataTitleOnly_ReturnsMetadataTitle()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-title-only");
        movie.MetadataTitle = "Metadata";
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("Metadata"));
    }

    [Fact]
    public async Task GetDetailsAsync_PlotOnly_FallsBackToFileTitle()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-plot-only");
        movie.Plot = "Plot only";
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("File", "Plot only"));
    }

    [Fact]
    public async Task GetDetailsAsync_ReleaseYearOnly_ReturnsMetadata()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-year-only");
        movie.ReleaseYear = new ReleaseYear(2020, new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero));
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("File", null, movie.ReleaseYear));
    }

    [Fact]
    public async Task GetDetailsAsync_RuntimeOnly_ReturnsMetadata()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-runtime-only");
        movie.RuntimeMinutes = new Runtime(120);
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("File", null, null, new Runtime(120)));
    }

    [Fact]
    public async Task GetDetailsAsync_ImdbRatingOnly_ReturnsMetadata()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-rating-only");
        movie.ImdbRating = new ImdbRating(8.2m);
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("File", null, null, null, new ImdbRating(8.2m)));
    }

    [Fact]
    public async Task GetDetailsAsync_ImdbIdOnly_ReturnsMetadata()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-imdbid-only");
        movie.ImdbId = new ImdbId("tt1234567");
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldBe(new MovieMetadata("File", null, null, null, null, new ImdbId("tt1234567")));
    }

    [Fact]
    public async Task GetGenresAsync_StoredGenres_ReturnsAllRowsSortedNameThenId()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var zoe = new GenreRecord { Name = "Zoe" };
        var drama = new GenreRecord { Name = "Drama" };
        var dramaAgain = new GenreRecord { Name = "Drama" };
        context.Genres.AddRange(zoe, drama, dramaAgain);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Drama", "Drama", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([drama.Id, dramaAgain.Id, zoe.Id]);
    }

    [Fact]
    public async Task GetPeopleAsync_StoredActors_ReturnsActorRowsSortedNameThenId()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var zoe = new ActorRecord { Name = "Zoe" };
        var ada = new ActorRecord { Name = "Ada" };
        var adaAgain = new ActorRecord { Name = "Ada" };
        context.Actors.AddRange(zoe, ada, adaAgain);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetPeopleAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Ada", "Ada", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([ada.Id, adaAgain.Id, zoe.Id]);
    }

    [Fact]
    public async Task GetDetailsAsync_DirectorsOnly_ReturnsMetadataWithDirector()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-directors-only");
        movie.Directors.Add(new DirectorRecord { Name = "Dana Director" });
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldNotBeNull();
        details.Metadata.Directors.ShouldBe(new[] { "Dana Director" });
    }

    [Fact]
    public async Task GetDetailsAsync_ActorsOnly_ReturnsMetadataWithActor()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-actors-only");
        movie.Actors.Add(new ActorRecord { Name = "Ada Actor" });
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldNotBeNull();
        details.Metadata.Actors.ShouldBe(new[] { "Ada Actor" });
    }

    [Fact]
    public async Task GetDetailsAsync_GenresWritersAndScore_ReturnsFullCollectionsMetadata()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var movie = Movie("File", "unit-details-collections-score");
        movie.MetadataTitle = "Metadata";
        movie.MetaScore = 82;
        movie.Genres.Add(new GenreRecord { Name = "Drama" });
        movie.Writers.Add(new WriterRecord { Name = "Wanda Writer" });
        await context.Movies.AddAsync(movie, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = movie.Id;
        id.ShouldNotBeNull();
        var catalog = new EfMovieCatalog(context);

        // act
        var details = await catalog.GetDetailsAsync(id, TestContext.Current.CancellationToken);

        // assert
        details.ShouldNotBeNull();
        details.Metadata.ShouldNotBeNull();
        details.Metadata.MetaScore.ShouldBe((short)82);
        details.Metadata.Genres.ShouldBe(["Drama"]);
        details.Metadata.Writers.ShouldBe(["Wanda Writer"]);
    }

    [Fact]
    public async Task GetGenreCountsAsync_SeededLibrary_ReturnsTotalAndPerGenreCounts()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        var unused = new GenreRecord { Name = "Unused" };
        context.Genres.AddRange(drama, comedy, unused);
        var shared = Movie("Shared", "unit-counts-shared");
        shared.Genres.Add(drama);
        shared.Genres.Add(comedy);
        var dramaOnly = Movie("Drama only", "unit-counts-drama-only");
        dramaOnly.Genres.Add(drama);
        context.Movies.AddRange(shared, dramaOnly, Movie("No genre", "unit-counts-no-genre"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        counts.TotalMovies.ShouldBe(3);
        counts.MoviesWithGenre[drama.Id].ShouldBe(2);
        counts.MoviesWithGenre[comedy.Id].ShouldBe(1);
        counts.MoviesWithGenre[unused.Id].ShouldBe(0);
        counts.MoviesWithGenre.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetGenreCountsAsync_EmptyLibrary_ReturnsZeroTotalAndEmptyMap()
    {
        // arrange
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        counts.TotalMovies.ShouldBe(0);
        counts.MoviesWithGenre.ShouldBeEmpty();
    }

    private static MovieRecord Movie(string title, string suffix) => new()
    {
        Title = title,
        LibraryPath = new LibraryPath($"C:/library/{suffix}.mkv"),
        Format = new MediaFormat("mkv"),
        Status = EnrichmentStatus.Pending
    };
}

[CollectionDefinition(nameof(CatalogMutationCollection))]
public sealed class CatalogMutationCollection : ICollectionFixture<PostgresFixture>;
