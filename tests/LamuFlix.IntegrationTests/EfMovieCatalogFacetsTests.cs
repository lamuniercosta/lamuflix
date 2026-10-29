using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieCatalogFacetsTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetGenresAsync_StoredGenres_ReturnsAllRowsSortedNameThenId()
    {
        // arrange
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
        facets.Select(facet => facet.MovieCount).ShouldBe([0, 0, 0]);
        facets.Select(facet => facet.Id).Distinct().Count().ShouldBe(facets.Count);
    }

    [Fact]
    public async Task GetGenresAsync_EmptyTable_ReturnsEmpty()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetGenresAsync_SharedMovie_CountsOncePerGenre()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        context.Genres.AddRange(drama, comedy);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var movie = new MovieRecord
        {
            Title = "Shared",
            LibraryPath = new LibraryPath("C:/library/shared.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        movie.Genres.Add(drama);
        movie.Genres.Add(comedy);
        context.Movies.Add(movie);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldContain(f => f.Name == "Drama" && f.MovieCount == 1);
        facets.ShouldContain(f => f.Name == "Comedy" && f.MovieCount == 1);
    }

    [Fact]
    public async Task GetGenresAsync_DistinctTotals_ReturnsPerGenreCounts()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        var unused = new GenreRecord { Name = "Unused" };
        context.Genres.AddRange(drama, comedy, unused);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var first = new MovieRecord
        {
            Title = "One",
            LibraryPath = new LibraryPath("C:/library/one.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        first.Genres.Add(drama);
        first.Genres.Add(comedy);
        var second = new MovieRecord
        {
            Title = "Two",
            LibraryPath = new LibraryPath("C:/library/two.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        second.Genres.Add(drama);
        context.Movies.AddRange(first, second);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Single(f => f.Name == "Drama").MovieCount.ShouldBe(2);
        facets.Single(f => f.Name == "Comedy").MovieCount.ShouldBe(1);
        facets.Single(f => f.Name == "Unused").MovieCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetGenresAsync_GenresPersistedThroughRepository_ReturnsNewlyStoredGenres()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var repository = new EfMovieRepository(
            context,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var movie = Movie.Create(id, "Facet persisted", new LibraryPath("C:/library/facet-persisted.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        movie.MarkEnriched(
            new MovieMetadata("Facet persisted", "Synopsis", genres: ["Drama", "Comedy"]),
            TimeProvider.System.GetUtcNow());
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();
        await using var readContext = LamuFlixDbContextFactory.OpenContext(connectionString);
        var catalog = new EfMovieCatalog(readContext);

        // act
        var facets = await catalog.GetGenresAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldContain(facet => facet.Name == "Drama" && facet.MovieCount == 1);
        facets.ShouldContain(facet => facet.Name == "Comedy" && facet.MovieCount == 1);
    }

    [Fact]
    public async Task GetGenreCountsAsync_NoMoviesOrGenres_ReturnsZeroTotalAndEmptyMap()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        counts.TotalMovies.ShouldBe(0);
        counts.MoviesWithGenre.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetGenreCountsAsync_PersistedGenresWithoutMovies_ReturnsZeroCountForEveryGenre()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        context.Genres.AddRange(drama, comedy);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        counts.TotalMovies.ShouldBe(0);
        counts.MoviesWithGenre.Count.ShouldBe(2);
        counts.MoviesWithGenre[drama.Name].ShouldBe(0);
        counts.MoviesWithGenre[comedy.Name].ShouldBe(0);
        counts.MoviesWithGenre["drama"].ShouldBe(0);
        counts.MoviesWithGenre["COMEDY"].ShouldBe(0);
    }

    [Fact]
    public async Task GetGenreCountsAsync_MoviesWithoutGenres_IncludesThemInTotal()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        context.Genres.Add(drama);
        context.Movies.AddRange(
            MovieCatalogSeed.Create("No genre one", "counts-no-genre-1"),
            MovieCatalogSeed.Create("No genre two", "counts-no-genre-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        counts.TotalMovies.ShouldBe(2);
        counts.MoviesWithGenre[drama.Name].ShouldBe(0);
    }

    [Fact]
    public async Task GetGenreCountsAsync_SharedAndMultipleMemberships_CountsDistinctMoviesWithoutJoinMultiplication()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        var unused = new GenreRecord { Name = "Unused" };
        context.Genres.AddRange(drama, comedy, unused);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var shared = MovieCatalogSeed.Create("Shared", "counts-shared");
        shared.Genres.Add(drama);
        shared.Genres.Add(comedy);
        var dramaOnly = MovieCatalogSeed.Create("Drama only", "counts-drama-only");
        dramaOnly.Genres.Add(drama);
        context.Movies.AddRange(
            shared,
            dramaOnly,
            MovieCatalogSeed.Create("No genre one", "counts-no-genre-1"),
            MovieCatalogSeed.Create("No genre two", "counts-no-genre-2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        counts.TotalMovies.ShouldBe(4);
        counts.MoviesWithGenre[drama.Name].ShouldBe(2);
        counts.MoviesWithGenre[comedy.Name].ShouldBe(1);
        counts.MoviesWithGenre[unused.Name].ShouldBe(0);
        counts.MoviesWithGenre["DRAMA"].ShouldBe(2);
        counts.MoviesWithGenre.Values.Sum().ShouldNotBe(counts.TotalMovies);
    }

    [Fact]
    public async Task GetGenreCountsAsync_SeededLibrary_ExecutesOneGroupedSqlCommand()
    {
        // arrange
        await using var seed = fixture.CreateMigratedContext();
        var drama = new GenreRecord { Name = "Drama" };
        var comedy = new GenreRecord { Name = "Comedy" };
        seed.Genres.AddRange(drama, comedy);
        var shared = MovieCatalogSeed.Create("Shared", "counts-single-command");
        shared.Genres.Add(drama);
        shared.Genres.Add(comedy);
        seed.Movies.AddRange(shared, MovieCatalogSeed.Create("No genre", "counts-single-command-no-genre"));
        await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(connectionString, o => o.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new LamuFlixDbContext(options);
        interceptor.Commands.Clear();
        var catalog = new EfMovieCatalog(context);

        // act
        var counts = await catalog.GetGenreCountsAsync(TestContext.Current.CancellationToken);

        // assert
        interceptor.Commands.Count.ShouldBe(1);
        interceptor.Commands[0].ShouldContain("COUNT");
        interceptor.Commands[0].ShouldContain("GROUP BY");
        counts.TotalMovies.ShouldBe(2);
        counts.MoviesWithGenre[drama.Name].ShouldBe(1);
        counts.MoviesWithGenre[comedy.Name].ShouldBe(1);
    }

    private static MovieRecord Member(string title, string pathSuffix, int collectionId)
    {
        var record = MovieCatalogSeed.Create(title, pathSuffix);
        record.CollectionId = collectionId;
        return record;
    }

    [Fact]
    public async Task GetPeopleAsync_StoredActors_ReturnsActorRowsSortedNameThenId()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var zoe = new ActorRecord { Name = "Zoe" };
        var ada = new ActorRecord { Name = "Ada" };
        var adaAgain = new ActorRecord { Name = "Ada" };
        var director = new DirectorRecord { Name = "Ann" };
        context.Actors.AddRange(zoe, ada, adaAgain);
        context.Directors.Add(director);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetPeopleAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Ada", "Ada", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([ada.Id, adaAgain.Id, zoe.Id]);
        facets.Select(facet => facet.Id).Distinct().Count().ShouldBe(facets.Count);
    }

    [Fact]
    public async Task GetPeopleAsync_EmptyTable_ReturnsEmpty()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetPeopleAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCollectionsAsync_MembersAndOrphans_ReturnsOnlyMemberCollectionsOrderedByNameThenId()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var alien = new CollectionRecord { Id = 10, Name = "Alien" };
        var alienAgain = new CollectionRecord { Id = 11, Name = "Alien" };
        var orphan = new CollectionRecord { Id = 20, Name = "Drama" };
        var zoe = new CollectionRecord { Id = 30, Name = "Zoe" };
        context.Collections.AddRange(alien, alienAgain, orphan, zoe);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.Movies.AddRange(
            Member("Alien one", "collections-alien-1", alien.Id),
            Member("Alien two", "collections-alien-2", alien.Id),
            Member("Alien again", "collections-alien-again", alienAgain.Id),
            Member("Zoe one", "collections-zoe-1", zoe.Id),
            MovieCatalogSeed.Create("No collection", "collections-none"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetCollectionsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Alien", "Alien", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([alien.Id, alienAgain.Id, zoe.Id]);
        facets.Select(facet => facet.MovieCount).ShouldBe([2, 1, 1]);
        facets.ShouldNotContain(facet => facet.Id == orphan.Id);
    }

    [Fact]
    public async Task GetCollectionsAsync_EmptyTable_ReturnsEmpty()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetCollectionsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCollectionsAsync_CollectionLeftWithoutMembers_IsOmittedAfterReassignment()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var first = new CollectionRecord { Id = 10, Name = "First" };
        var second = new CollectionRecord { Id = 20, Name = "Second" };
        context.Collections.AddRange(first, second);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var moved = Member("Moved", "collections-moved", first.Id);
        var stays = Member("Stays", "collections-stays", second.Id);
        context.Movies.AddRange(moved, stays);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        moved.CollectionId = second.Id;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetCollectionsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldHaveSingleItem().ShouldBe(new CollectionFacet(second.Id, "Second", 2));
    }

    [Fact]
    public async Task GetDirectorsAsync_DirectorWithoutMovies_IsExcluded()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var linked = new DirectorRecord { Name = "Linked" };
        var orphan = new DirectorRecord { Name = "Orphan" };
        context.Directors.AddRange(linked, orphan);
        var movie = MovieCatalogSeed.Create("Directed", "directors-orphan");
        movie.Directors.Add(linked);
        context.Movies.Add(movie);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetDirectorsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Linked"]);
        facets.ShouldNotContain(facet => facet.Id == orphan.Id);
    }

    [Fact]
    public async Task GetDirectorsAsync_EmptyTable_ReturnsEmpty()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetDirectorsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDirectorsAsync_SharedMovie_CountsOncePerDirector()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var ann = new DirectorRecord { Name = "Ann" };
        var bob = new DirectorRecord { Name = "Bob" };
        context.Directors.AddRange(ann, bob);
        var shared = MovieCatalogSeed.Create("Shared", "directors-shared");
        shared.Directors.Add(ann);
        shared.Directors.Add(bob);
        context.Movies.Add(shared);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetDirectorsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Single(facet => facet.Id == ann.Id).MovieCount.ShouldBe(1);
        facets.Single(facet => facet.Id == bob.Id).MovieCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetDirectorsAsync_SameNameDirectors_ReturnsSeparateIdentitiesWithOwnCounts()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var first = new DirectorRecord { Name = "Alex Doe" };
        var second = new DirectorRecord { Name = "Alex Doe" };
        context.Directors.AddRange(first, second);
        var firstMovie = MovieCatalogSeed.Create("First", "directors-same-name-1");
        firstMovie.Directors.Add(first);
        var secondMovie = MovieCatalogSeed.Create("Second", "directors-same-name-2");
        secondMovie.Directors.Add(second);
        var thirdMovie = MovieCatalogSeed.Create("Third", "directors-same-name-3");
        thirdMovie.Directors.Add(second);
        context.Movies.AddRange(firstMovie, secondMovie, thirdMovie);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetDirectorsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Count.ShouldBe(2);
        facets.Single(facet => facet.Id == first.Id).MovieCount.ShouldBe(1);
        facets.Single(facet => facet.Id == second.Id).MovieCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetDirectorsAsync_StoredDirectors_ReturnsRowsSortedNameThenId()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var zoe = new DirectorRecord { Name = "Zoe" };
        var ann = new DirectorRecord { Name = "Ann" };
        var annAgain = new DirectorRecord { Name = "Ann" };
        context.Directors.AddRange(zoe, ann, annAgain);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var zoeMovie = MovieCatalogSeed.Create("Zoe film", "directors-order-zoe");
        zoeMovie.Directors.Add(zoe);
        var annMovie = MovieCatalogSeed.Create("Ann film", "directors-order-ann");
        annMovie.Directors.Add(ann);
        var annAgainMovie = MovieCatalogSeed.Create("Ann again", "directors-order-ann-again");
        annAgainMovie.Directors.Add(annAgain);
        context.Movies.AddRange(zoeMovie, annMovie, annAgainMovie);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetDirectorsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Select(facet => facet.Name).ShouldBe(["Ann", "Ann", "Zoe"]);
        facets.Select(facet => facet.Id).ShouldBe([ann.Id, annAgain.Id, zoe.Id]);
    }

    [Fact]
    public async Task GetDirectorsAsync_WatchlistedAndPendingMovies_CountsFullLibrary()
    {
        // arrange
        await using var context = fixture.CreateMigratedContext();
        var ann = new DirectorRecord { Name = "Ann" };
        context.Directors.Add(ann);
        var watchlisted = MovieCatalogSeed.Create("Watchlisted", "directors-full-library-1");
        watchlisted.Directors.Add(ann);
        watchlisted.IsInWatchlist = true;
        var pending = MovieCatalogSeed.Create("Pending", "directors-full-library-2");
        pending.Directors.Add(ann);
        context.Movies.AddRange(watchlisted, pending);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var catalog = new EfMovieCatalog(context);

        // act
        var facets = await catalog.GetDirectorsAsync(TestContext.Current.CancellationToken);

        // assert
        facets.Single(facet => facet.Id == ann.Id).MovieCount.ShouldBe(2);
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        internal List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
