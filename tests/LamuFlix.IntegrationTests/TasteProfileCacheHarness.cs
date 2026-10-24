using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Shouldly;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pgvector;

namespace LamuFlix.IntegrationTests;

internal sealed class TasteProfileCacheHarness : IAsyncDisposable
{
    private static readonly DateTimeOffset FirstLaunch = new(2026, 9, 1, 20, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider provider;
    private readonly PostgresFixture fixture;

    private TasteProfileCacheHarness(
        PostgresFixture fixture,
        ServiceProvider provider,
        ManualTimeProvider time,
        TestCommandInterceptor commands,
        CountingScopeFactory scopes,
        HybridCachedRecommendationProfileSource adapter,
        TimeSpan expiration)
    {
        this.fixture = fixture;
        this.provider = provider;
        Time = time;
        Commands = commands;
        Scopes = scopes;
        Adapter = adapter;
        Expiration = expiration;
    }

    public HybridCachedRecommendationProfileSource Adapter { get; }

    public TestCommandInterceptor Commands { get; }

    public CountingScopeFactory Scopes { get; }

    public ManualTimeProvider Time { get; }

    public TimeSpan Expiration { get; }

    public IServiceProvider Services => provider;

    public static TasteProfileCacheHarness Create(PostgresFixture fixture, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var duration = expiration ?? TimeSpan.FromMinutes(5);
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
        var commands = new TestCommandInterceptor();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(new RecommendationOptions()));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<GenreWeightsCache>();
        services.AddScoped<IMovieCatalog, EfMovieCatalog>();
        services.AddScoped<GenreWeightsCalculator>();
        services.AddDbContext<LamuFlixDbContext>(options => options
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.UseVector())
            .AddInterceptors(commands));
        services.AddScoped<EfRecommendationProfileSource>();
        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = duration,
            LocalCacheExpiration = duration,
        });

        var provider = services.BuildServiceProvider();
        var scopes = new CountingScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());
        var adapter = new HybridCachedRecommendationProfileSource(
            provider.GetRequiredService<HybridCache>(),
            scopes,
            time,
            Options.Create(new TasteProfileCacheOptions { Expiration = duration }));

        return new TasteProfileCacheHarness(fixture, provider, time, commands, scopes, adapter, duration);
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();

    public Task ResetAsync(CancellationToken ct) => fixture.ResetAsync(ct);

    public async Task<MovieId> SeedMovieAsync(
        string title,
        bool isWatched,
        CancellationToken ct,
        params string[] genres)
    {
        await using var db = fixture.CreateMigratedContext();
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{Guid.NewGuid():N}.mkv"),
            Format = new MediaFormat("mkv"),
            IsWatched = isWatched,
            Status = EnrichmentStatus.Enriched,
        };

        foreach (var genre in genres)
        {
            record.Genres.Add(new GenreRecord { Name = genre });
        }

        db.Movies.Add(record);
        await db.SaveChangesAsync(ct);

        return record.Id
            ?? throw new InvalidOperationException("The database did not assign a movie id.");
    }

    public async Task SeedLaunchAsync(MovieId movieId, DateTimeOffset playedAt, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        db.PlaybackEvents.Add(new PlaybackEventRecord { MovieId = movieId, PlayedAt = playedAt });
        await db.SaveChangesAsync(ct);
    }

    public async Task SeedVectorAsync(MovieId movieId, float[] vector, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        db.MovieFeatures.Add(new MovieFeaturesRecord
        {
            MovieId = movieId,
            Vector = new Vector(vector),
            ComputedAt = FirstLaunch,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkWatchedAsync(MovieId movieId, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        var record = await db.Movies.SingleAsync(movie => movie.Id == movieId, ct);
        record.IsWatched = true;
        await db.SaveChangesAsync(ct);
    }

    public static float[] VariedVector(int offset)
    {
        var vector = new float[EmbeddingDimensions.Value];

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = ((index + offset) % 37) / 37f;
        }

        vector[0] = 1f;
        return vector;
    }
}

internal sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private readonly object gate = new();
    private DateTimeOffset now = initial;

    public override DateTimeOffset GetUtcNow()
    {
        lock (gate)
        {
            return now;
        }
    }

    public void Advance(TimeSpan delta)
    {
        lock (gate)
        {
            now += delta;
        }
    }
}

internal sealed class TestCommandInterceptor : DbCommandInterceptor
{
    private readonly object gate = new();
    private TaskCompletionSource? reached;
    private TaskCompletionSource? release;
    private string? blockFragment;
    private bool blockArmed;
    private int readerCount;
    private int failNext;

    public int ReaderCount => Volatile.Read(ref readerCount);

    public void ResetCount() => Interlocked.Exchange(ref readerCount, 0);

    public void FailNextReader() => Interlocked.Exchange(ref failNext, 1);

    public void BlockNextReader(string? commandTextFragment = null)
    {
        lock (gate)
        {
            reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            blockFragment = commandTextFragment;
            blockArmed = true;
        }
    }

    public Task WaitUntilBlockedAsync()
    {
        lock (gate)
        {
            return reached?.Task ?? Task.CompletedTask;
        }
    }

    public void ReleaseBlockedReader()
    {
        TaskCompletionSource? pending;
        lock (gate)
        {
            pending = release;
            blockArmed = false;
        }

        pending?.TrySetResult();
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref readerCount);

        if (Interlocked.CompareExchange(ref failNext, 0, 1) == 1)
        {
            throw new InvalidOperationException("Injected reader failure.");
        }

        Task blocked;
        TaskCompletionSource? signalled;
        lock (gate)
        {
            if (!blockArmed
                || reached is null
                || release is null
                || (blockFragment is { } fragment && !command.CommandText.Contains(fragment, StringComparison.Ordinal)))
            {
                return result;
            }

            blockArmed = false;
            blockFragment = null;
            blocked = release.Task;
            signalled = reached;
        }

        signalled?.TrySetResult();
        await blocked.WaitAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }
}

internal static class TasteProfileAssertions
{
    public static void ShouldMatchProfile(this TasteProfile expected, TasteProfile actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        actual.IsEmpty.ShouldBe(expected.IsEmpty);
        actual.GenrePreferences.Count.ShouldBe(expected.GenrePreferences.Count);
        foreach (var preference in expected.GenrePreferences)
        {
            actual.GenrePreferences[preference.Key].ShouldBe(preference.Value);
        }

        actual.PlotEmbedding.SequenceEqual(expected.PlotEmbedding).ShouldBeTrue();
    }
}

internal sealed class CountingScopeFactory(IServiceScopeFactory inner) : IServiceScopeFactory
{
    private int created;
    private int disposed;

    public int Created => Volatile.Read(ref created);

    public int Disposed => Volatile.Read(ref disposed);

    public IServiceScope CreateScope()
    {
        Interlocked.Increment(ref created);
        return new CountingScope(inner.CreateScope(), this);
    }

    private sealed class CountingScope(IServiceScope inner, CountingScopeFactory owner) : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider => inner.ServiceProvider;

        public void Dispose()
        {
            inner.Dispose();
            Interlocked.Increment(ref owner.disposed);
        }

        public async ValueTask DisposeAsync()
        {
            if (inner is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                inner.Dispose();
            }

            Interlocked.Increment(ref owner.disposed);
        }
    }
}
