using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
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
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));
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
}
