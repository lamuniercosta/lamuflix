using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieRepositoryDeleteTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TryDeleteAsync_RemovesRowAndJoins_WhileSharedPrincipalsOtherMovieAndFileRemain()
    {
        var ct = TestContext.Current.CancellationToken;
        var mediaFile = CreateMediaFile();
        try
        {
            await using var db = fixture.CreateMigratedContext();
            var (alpha, bravo) = await SeedSharedLibraryAsync(db, mediaFile);
            var repository = Repository(db);

            (await repository.TryDeleteAsync(alpha, ct)).ShouldBeTrue();

            await using var verify = OpenSibling(db);
            (await verify.Movies.CountAsync(movie => movie.Id == alpha, ct)).ShouldBe(0);
            var survivor = await verify.Movies.SingleAsync(movie => movie.Id == bravo, ct);
            survivor.Title.ShouldBe("Bravo");
            survivor.IsInWatchlist.ShouldBeTrue();

            await AssertSharedPrincipalSurvivesAsync(verify, bravo, ct);
            await AssertOnlyAlphaPrincipalDetachedAsync(verify, ct);

            File.Exists(mediaFile).ShouldBeTrue();
        }
        finally
        {
            File.Delete(mediaFile);
        }
    }

    [Fact]
    public async Task TryDeleteAsync_RepeatDelete_ReportsAbsentAndPreservesOtherMovieAndFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var mediaFile = CreateMediaFile();
        try
        {
            await using var db = fixture.CreateMigratedContext();
            var (alpha, bravo) = await SeedSharedLibraryAsync(db, mediaFile);
            var repository = Repository(db);

            (await repository.TryDeleteAsync(alpha, ct)).ShouldBeTrue();
            (await repository.TryDeleteAsync(alpha, ct)).ShouldBeFalse();

            await using var verify = OpenSibling(db);
            (await verify.Movies.CountAsync(movie => movie.Id == alpha, ct)).ShouldBe(0);
            (await verify.Movies.CountAsync(movie => movie.Id == bravo, ct)).ShouldBe(1);
            File.Exists(mediaFile).ShouldBeTrue();
        }
        finally
        {
            File.Delete(mediaFile);
        }
    }

    [Fact]
    public async Task TryDeleteAsync_OverlappingSameIdDeletes_ExactlyOneRemovesTheRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var mediaFile = CreateMediaFile();
        try
        {
            await using var setup = fixture.CreateMigratedContext();
            var (alpha, bravo) = await SeedSharedLibraryAsync(setup, mediaFile);

            await using var firstContext = fixture.CreateMigratedContext();
            await using var secondContext = fixture.CreateMigratedContext();
            var first = Repository(firstContext);
            var second = Repository(secondContext);

            var results = await Task.WhenAll(
                first.TryDeleteAsync(alpha, ct),
                second.TryDeleteAsync(alpha, ct));

            results.Count(removed => removed).ShouldBe(1);

            await using var verify = OpenSibling(setup);
            (await verify.Movies.CountAsync(movie => movie.Id == alpha, ct)).ShouldBe(0);
            (await verify.Movies.CountAsync(movie => movie.Id == bravo, ct)).ShouldBe(1);
            File.Exists(mediaFile).ShouldBeTrue();
        }
        finally
        {
            File.Delete(mediaFile);
        }
    }

    private static async Task AssertSharedPrincipalSurvivesAsync(
        LamuFlixDbContext verify, MovieId survivor, CancellationToken ct)
    {
        var actor = await verify.Actors.Include(item => item.Movies)
            .SingleAsync(item => item.Name == "Shared Actor", ct);
        actor.Movies.Select(movie => movie.Id!.Value).ShouldBe([survivor.Value]);

        var director = await verify.Directors.Include(item => item.Movies)
            .SingleAsync(item => item.Name == "Shared Director", ct);
        director.Movies.Select(movie => movie.Id!.Value).ShouldBe([survivor.Value]);

        var genre = await verify.Genres.Include(item => item.Movies)
            .SingleAsync(item => item.Name == "Shared Genre", ct);
        genre.Movies.Select(movie => movie.Id!.Value).ShouldBe([survivor.Value]);
    }

    private static async Task AssertOnlyAlphaPrincipalDetachedAsync(LamuFlixDbContext verify, CancellationToken ct)
    {
        var director = await verify.Directors.Include(item => item.Movies)
            .SingleAsync(item => item.Name == "Only Alpha Director", ct);
        director.Movies.ShouldBeEmpty();

        var genre = await verify.Genres.Include(item => item.Movies)
            .SingleAsync(item => item.Name == "Only Alpha Genre", ct);
        genre.Movies.ShouldBeEmpty();
    }

    private static async Task<(MovieId Alpha, MovieId Bravo)> SeedSharedLibraryAsync(
        LamuFlixDbContext db, string mediaFile)
    {
        var ct = TestContext.Current.CancellationToken;
        var sharedActor = new ActorRecord { Name = "Shared Actor" };
        var sharedDirector = new DirectorRecord { Name = "Shared Director" };
        var sharedGenre = new GenreRecord { Name = "Shared Genre" };

        var alpha = MovieCatalogSeed.Create("Alpha", "delete-alpha");
        alpha.LibraryPath = new LibraryPath(mediaFile);
        alpha.IsInWatchlist = true;
        alpha.MetadataTitle = "Alpha metadata";
        alpha.ReleaseYear = new ReleaseYear(2001, MovieCatalogSeed.FixedTime);
        alpha.Actors.Add(sharedActor);
        alpha.Directors.Add(sharedDirector);
        alpha.Directors.Add(new DirectorRecord { Name = "Only Alpha Director" });
        alpha.Genres.Add(sharedGenre);
        alpha.Genres.Add(new GenreRecord { Name = "Only Alpha Genre" });

        var bravo = MovieCatalogSeed.Create("Bravo", "delete-bravo");
        bravo.IsInWatchlist = true;
        bravo.Actors.Add(sharedActor);
        bravo.Directors.Add(sharedDirector);
        bravo.Genres.Add(sharedGenre);

        db.Movies.AddRange(alpha, bravo);
        await db.SaveChangesAsync(ct);

        alpha.Id.ShouldNotBeNull();
        bravo.Id.ShouldNotBeNull();
        return (alpha.Id, bravo.Id);
    }

    private static string CreateMediaFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lamuflix-delete-{Guid.NewGuid():N}.mkv");
        File.WriteAllText(path, "media");
        return path;
    }

    private static LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        var connectionString = source.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(db, TimeProvider.System, Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }));
}
