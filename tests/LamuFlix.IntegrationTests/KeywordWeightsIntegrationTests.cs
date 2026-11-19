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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class KeywordWeightsIntegrationTests(PostgresFixture fixture, ITestOutputHelper output) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetKeywordCounts_EmptyAndDistinctMemberships_ReturnsLibraryPopulationCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = new KeywordWeightsCache();
        var empty = await ReadAsync(cache);
        empty.TotalMovies.ShouldBe(0);
        empty.MoviesWithKeyword.ShouldBeEmpty();

        await SeedAsync(("Alpha", "kw-count-alpha", ["Drama", "Mystery"]),
            ("Bravo", "kw-count-bravo", ["Drama"]),
            ("Charlie", "kw-count-charlie", []));
        await using var db = OpenContext();
        var counts = await new EfMovieCatalog(db).GetKeywordCountsAsync(ct);

        counts.TotalMovies.ShouldBe(3);
        counts.MoviesWithKeyword.ShouldBe(new Dictionary<string, long> { ["Drama"] = 2, ["Mystery"] = 1 });
    }

    [Fact]
    public void AddMovieCatalog_UsesSingletonCacheAndScopedCalculator()
    {
        var services = new ServiceCollection();

        services.AddMovieCatalog();

        services.Single(descriptor => descriptor.ServiceType == typeof(KeywordWeightsCache)).Lifetime
            .ShouldBe(ServiceLifetime.Singleton);
        services.Single(descriptor => descriptor.ServiceType == typeof(KeywordWeightsCalculator)).Lifetime
            .ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task GetKeywordCounts_RepresentativeQueryPlan_ReportsGroupingAndTableAccess()
    {
        var movies = Enumerable.Range(0, 1000)
            .Select(index => ($"Plan movie {index}", $"kw-plan-{index}", new[] { "Drama" }))
            .ToArray();
        await SeedAsync(movies);
        await using var db = OpenContext();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
            WITH library AS (
                SELECT COUNT(*)::bigint AS total_movies FROM movies
            ), keyword_counts AS (
                SELECT keywords.name AS keyword_name, COUNT(DISTINCT movie_keywords.movie_id)::bigint AS movie_count
                FROM movie_keywords
                JOIN keywords ON keywords.id = movie_keywords.keyword_id
                JOIN movies ON movies.id = movie_keywords.movie_id
                GROUP BY keywords.name
            )
            SELECT library.total_movies AS "TotalMovies", keyword_counts.keyword_name AS "KeywordName",
                COALESCE(keyword_counts.movie_count, 0)::bigint AS "MovieCount"
            FROM library LEFT JOIN keyword_counts ON TRUE
            """;
        var plan = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken))
        {
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                var line = reader.GetString(0);
                plan.Add(line);
                output.WriteLine(line);
            }
        }

        string.Join(Environment.NewLine, plan).ShouldContain("movie_keywords");
        plan.Any(line => line.Contains("Aggregate", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_KeywordMembershipMutation_InvalidatesOnlyAfterCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(("Alpha", "kw-write-alpha", ["Drama"]), ("Bravo", "kw-write-bravo", []));
        var keywordCache = new KeywordWeightsCache();
        var warm = await ReadAsync(keywordCache);
        await using var db = OpenContext();
        var repository = Repository(db, new GenreWeightsCache(), keywordCache);
        var movie = await repository.GetAsync(seeded[1], ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(new MovieMetadata("Bravo", signals: new MovieSignalSnapshot(
            keywordAvailability: SignalAvailability.Available,
            keywords: [new MovieKeyword(1, "Drama")])), MovieCatalogSeed.FixedTime);

        await repository.SaveChangesAsync(ct);
        var refreshed = await ReadAsync(keywordCache);

        refreshed.ShouldNotBeSameAs(warm);
        refreshed.TotalMovies.ShouldBe(2);
        refreshed.MoviesWithKeyword["Drama"].ShouldBe(2);
    }

    [Fact]
    public async Task SaveChanges_CancelledWrite_RetainsWarmKeywordCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(("Alpha", "kw-cancel-alpha", ["Drama"]));
        var keywordCache = new KeywordWeightsCache();
        var warm = await ReadAsync(keywordCache);
        await using var db = OpenContext();
        var repository = Repository(db, new GenreWeightsCache(), keywordCache);
        var movie = await repository.GetAsync(seeded[0], ct);
        movie.ShouldNotBeNull();
        movie.AddToWatchlist();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => repository.SaveChangesAsync(cancelled.Token));

        (await ReadAsync(keywordCache)).ShouldBeSameAs(warm);
    }

    [Fact]
    public async Task TryDeleteAsync_ExistingMovie_InvalidatesKeywordCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(("Alpha", "kw-delete-alpha", ["Drama"]), ("Bravo", "kw-delete-bravo", []));
        var keywordCache = new KeywordWeightsCache();
        var warm = await ReadAsync(keywordCache);
        await using var db = OpenContext();
        var repository = Repository(db, new GenreWeightsCache(), keywordCache);

        (await repository.TryDeleteAsync(seeded[0], ct)).ShouldBeTrue();
        var refreshed = await ReadAsync(keywordCache);

        refreshed.ShouldNotBeSameAs(warm);
        refreshed.TotalMovies.ShouldBe(1);
        refreshed.MoviesWithKeyword.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetOrLoadAsync_CommitDuringRead_RetriesToOneCoherentSnapshot()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeedAsync(("Alpha", "kw-race-alpha", []), ("Bravo", "kw-race-bravo", []));
        var keywordCache = new KeywordWeightsCache();
        await using var readDb = OpenContext();
        var catalog = new EfMovieCatalog(readDb);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;

        var read = keywordCache.GetOrLoadAsync(async token =>
        {
            var counts = await catalog.GetKeywordCountsAsync(token);
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
            }

            return counts;
        }, ct);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        await AddKeywordToFirstMovieAsync(seeded[0], keywordCache, ct);
        release.SetResult();
        var snapshot = await read;

        calls.ShouldBe(2);
        snapshot.TotalMovies.ShouldBe(2);
        snapshot.MoviesWithKeyword["Drama"].ShouldBe(1);
    }

    private async Task AddKeywordToFirstMovieAsync(MovieId id, KeywordWeightsCache keywordCache, CancellationToken ct)
    {
        await using var db = OpenContext();
        var repository = Repository(db, new GenreWeightsCache(), keywordCache);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(new MovieMetadata("Alpha", signals: new MovieSignalSnapshot(
            keywordAvailability: SignalAvailability.Available,
            keywords: [new MovieKeyword(1, "Drama")])), MovieCatalogSeed.FixedTime);
        await repository.SaveChangesAsync(ct);
    }

    private async Task<KeywordCounts> ReadAsync(KeywordWeightsCache cache)
    {
        await using var db = OpenContext();
        return await new KeywordWeightsCalculator(new EfMovieCatalog(db), cache).GetAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IReadOnlyList<MovieId>> SeedAsync(params (string Title, string Suffix, string[] Keywords)[] movies)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = OpenContext();
        var keywords = new Dictionary<string, KeywordRecord>(StringComparer.Ordinal);
        var records = new List<MovieRecord>();
        foreach (var (title, suffix, names) in movies)
        {
            var record = MovieCatalogSeed.Create(title, suffix);
            foreach (var name in names)
            {
                if (!keywords.TryGetValue(name, out var keyword))
                {
                    keyword = new KeywordRecord { Id = keywords.Count + 1, Name = name };
                    keywords[name] = keyword;
                }

                record.Keywords.Add(keyword);
            }

            records.Add(record);
        }

        db.Movies.AddRange(records);
        await db.SaveChangesAsync(ct);
        return [.. records.Select(record => record.Id ?? throw new InvalidOperationException("Seeded movie has no id."))];
    }

    private LamuFlixDbContext OpenContext()
    {
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db, GenreWeightsCache genreCache, KeywordWeightsCache keywordCache) =>
        new(db, TimeProvider.System, Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            new RecordingEnrichmentStatusNotifier(), NullLogger<EfMovieRepository>.Instance, genreCache,
            keywordWeightsCache: keywordCache);
}