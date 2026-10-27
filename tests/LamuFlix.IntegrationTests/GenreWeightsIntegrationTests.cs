using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class GenreWeightsIntegrationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveChanges_ImportedMovieWithGenre_NextReadRebuildsFromUpdatedCounts()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(("Existing", "gw-import-existing", []));
        var cache = new GenreWeightsCache();
        var warm = await ReadAsync(cache);
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var imported = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Imported",
            new LibraryPath("C:/library/gw-imported.mkv"),
            new MediaFormat("mkv"));

        // act
        await repository.AddAsync(imported, ct);
        await repository.SaveChangesAsync(ct);
        imported.MarkEnriched(new MovieMetadata("Imported", genres: ["Drama"]), MovieCatalogSeed.FixedTime);
        await repository.SaveChangesAsync(ct);
        var refreshed = await ReadAsync(cache);

        // assert
        refreshed.ShouldNotBeSameAs(warm);
        warm.GetWeight("Drama").ShouldBe(1.0);
        refreshed.GetWeight("Drama").ShouldBe(Idf(2, 1), 1e-9);
        refreshed.GetWeight("drama").ShouldBe(Idf(2, 1), 1e-9);
    }

    [Fact]
    public async Task SaveChanges_GenreMembershipReplacement_NextReadReflectsNewIdf()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(
            ("Alpha", "gw-replace-alpha", ["Drama"]),
            ("Bravo", "gw-replace-bravo", ["Drama", "Comedy"]),
            ("Charlie", "gw-replace-charlie", []));
        var cache = new GenreWeightsCache();
        var warm = await ReadAsync(cache);
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var alpha = await repository.GetAsync(seeded.Movies[0], ct);
        alpha.ShouldNotBeNull();

        // act
        alpha.MarkEnriched(new MovieMetadata("Alpha", genres: ["Comedy"]), MovieCatalogSeed.FixedTime);
        await repository.SaveChangesAsync(ct);
        var refreshed = await ReadAsync(cache);

        // assert
        warm.GetWeight("Drama").ShouldBe(Idf(3, 2), 1e-9);
        warm.GetWeight("Comedy").ShouldBe(Idf(3, 1), 1e-9);
        refreshed.ShouldNotBeSameAs(warm);
        refreshed.GetWeight("Drama").ShouldBe(Idf(3, 1), 1e-9);
        refreshed.GetWeight("Comedy").ShouldBe(Idf(3, 2), 1e-9);
    }

    [Fact]
    public async Task TryDeleteAsync_ExistingMovie_NextReadRebuildsFromReducedCounts()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(
            ("Alpha", "gw-delete-alpha", ["Drama"]),
            ("Bravo", "gw-delete-bravo", ["Drama"]),
            ("Charlie", "gw-delete-charlie", ["Comedy"]));
        var cache = new GenreWeightsCache();
        var warm = await ReadAsync(cache);
        await using var db = OpenContext();
        var repository = Repository(db, cache);

        // act
        var deleted = await repository.TryDeleteAsync(seeded.Movies[0], ct);
        var refreshed = await ReadAsync(cache);

        // assert
        deleted.ShouldBeTrue();
        warm.GetWeight("Drama").ShouldBe(Idf(3, 2), 1e-9);
        refreshed.ShouldNotBeSameAs(warm);
        refreshed.GetWeight("Drama").ShouldBe(Idf(2, 1), 1e-9);
        refreshed.GetWeight("drama").ShouldBe(Idf(2, 1), 1e-9);
        refreshed.GetWeight("Comedy").ShouldBe(Idf(2, 1), 1e-9);
    }

    [Fact]
    public async Task TryDeleteAsync_AbsentMovie_RetainsWarmCache()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(("Alpha", "gw-absent-alpha", ["Drama"]));
        var cache = new GenreWeightsCache();
        var warm = await ReadAsync(cache);
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var absent = await repository.NextIdentityAsync(ct);

        // act
        var deleted = await repository.TryDeleteAsync(absent, ct);
        var after = await ReadAsync(cache);

        // assert
        deleted.ShouldBeFalse();
        after.ShouldBeSameAs(warm);
    }

    [Fact]
    public async Task SaveChanges_Cancelled_RetainsWarmCache()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(("Alpha", "gw-cancelled-alpha", ["Drama"]));
        var cache = new GenreWeightsCache();
        var warm = await ReadAsync(cache);
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var alpha = await repository.GetAsync(seeded.Movies[0], ct);
        alpha.ShouldNotBeNull();
        alpha.AddToWatchlist();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        // ReSharper disable once AccessToDisposedClosure // the assertion runs before disposal within the test
        var act = () => repository.SaveChangesAsync(cancelled.Token);
        await Should.ThrowAsync<OperationCanceledException>(act);
        var after = await ReadAsync(cache);

        // assert
        after.ShouldBeSameAs(warm);
    }

    [Fact]
    public async Task SaveChanges_DuplicateImdbId_RetainsWarmCache()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = new GenreWeightsCache();
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var holder = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Holder",
            new LibraryPath("C:/library/gw-duplicate-holder.mkv"),
            new MediaFormat("mkv"));
        await repository.AddAsync(holder, ct);
        holder.MarkEnriched(new MovieMetadata("Holder", imdbId: new ImdbId("tt9900100")), MovieCatalogSeed.FixedTime);
        await repository.SaveChangesAsync(ct);
        var loser = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Loser",
            new LibraryPath("C:/library/gw-duplicate-loser.mkv"),
            new MediaFormat("mkv"));
        await repository.AddAsync(loser, ct);
        await repository.SaveChangesAsync(ct);
        var warm = await ReadAsync(cache);
        loser.MarkEnriched(new MovieMetadata("Loser", imdbId: new ImdbId("tt9900100")), MovieCatalogSeed.FixedTime);

        // act
        var act = () => repository.SaveChangesAsync(ct);
        await Should.ThrowAsync<DuplicateImdbIdException>(act);
        var after = await ReadAsync(cache);

        // assert
        after.ShouldBeSameAs(warm);
    }

    [Fact]
    public async Task SaveChanges_ScalarOnlySave_NextReadReloadsEvenWhenWeightsAreEqual()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(
            ("Alpha", "gw-scalar-alpha", ["Drama"]),
            ("Bravo", "gw-scalar-bravo", []));
        var cache = new GenreWeightsCache();
        var warm = await ReadAsync(cache);
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var bravo = await repository.GetAsync(seeded.Movies[1], ct);
        bravo.ShouldNotBeNull();
        bravo.AddToWatchlist();

        // act
        await repository.SaveChangesAsync(ct);
        var refreshed = await ReadAsync(cache);

        // assert
        refreshed.ShouldNotBeSameAs(warm);
        refreshed.GetWeight("Drama").ShouldBe(warm.GetWeight("Drama"));
        refreshed.GetWeight("Drama").ShouldBe(Idf(2, 1), 1e-9);
        refreshed.GetWeight("drama").ShouldBe(Idf(2, 1), 1e-9);
    }

    [Fact]
    public async Task GetOrLoadAsync_SaveDuringInFlightLoad_DiscardsStaleResultAndRetries()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(
            ("Alpha", "gw-race-alpha", []),
            ("Bravo", "gw-race-bravo", []));
        var cache = new GenreWeightsCache();
        await using var readDb = OpenContext();
        var catalog = new EfMovieCatalog(readDb);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        // act
        var read = cache.GetOrLoadAsync(
            async token =>
            {
                var counts = await catalog.GetGenreCountsAsync(token);
                if (Interlocked.Increment(ref calls) == 1)
                {
                    entered.SetResult();
                    await release.Task;
                }

                return GenreWeights.Calculate(counts);
            },
            ct);
        await entered.Task;
        await SaveGenreToFirstMovieAsync(cache, ct);
        release.SetResult();
        var weights = await read;

        // assert
        calls.ShouldBe(2);
        weights.GetWeight("Drama").ShouldBe(Idf(2, 1), 1e-9);
    }

    private async Task SaveGenreToFirstMovieAsync(GenreWeightsCache cache, CancellationToken ct)
    {
        await using var db = OpenContext();
        var repository = Repository(db, cache);
        var id = await db.Movies.OrderBy(movie => movie.Id).Select(movie => movie.Id).FirstAsync(ct);
        id.ShouldNotBeNull();
        var alpha = await repository.GetAsync(id, ct);
        alpha.ShouldNotBeNull();
        alpha.MarkEnriched(new MovieMetadata("Alpha", genres: ["Drama"]), MovieCatalogSeed.FixedTime);
        await repository.SaveChangesAsync(ct);
    }

    private async Task<GenreWeights> ReadAsync(GenreWeightsCache cache)
    {
        await using var db = OpenContext();
        var calculator = new GenreWeightsCalculator(new EfMovieCatalog(db), cache);
        return await calculator.GetAsync(TestContext.Current.CancellationToken);
    }

    private async Task<(IReadOnlyList<MovieId> Movies, IReadOnlyDictionary<string, int> Genres)> SeedAsync(
        params (string Title, string Suffix, string[] Genres)[] movies)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = OpenContext();
        var genres = new Dictionary<string, GenreRecord>(StringComparer.Ordinal);
        var records = new List<MovieRecord>();

        foreach (var (title, suffix, names) in movies)
        {
            var record = MovieCatalogSeed.Create(title, suffix);
            foreach (var name in names)
            {
                if (!genres.TryGetValue(name, out var genre))
                {
                    genre = new GenreRecord { Name = name };
                    genres[name] = genre;
                }

                record.Genres.Add(genre);
            }

            records.Add(record);
        }

        db.Movies.AddRange(records);
        await db.SaveChangesAsync(ct);

        var movieIds = records
            .Select(record => record.Id ?? throw new InvalidOperationException("Seeded movie has no id."))
            .ToArray();
        var genreIds = genres.ToDictionary(pair => pair.Key, pair => pair.Value.Id, StringComparer.Ordinal);
        return (movieIds, genreIds);
    }

    private LamuFlixDbContext OpenContext()
    {
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db, GenreWeightsCache cache) =>
        new(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance,
            cache);

    private static double Idf(long totalMovies, long frequency) =>
        Math.Log((totalMovies + 1.0) / (frequency + 1.0)) + 1.0;
}
