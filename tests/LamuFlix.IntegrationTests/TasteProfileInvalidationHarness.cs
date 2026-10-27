using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.ServiceDefaults;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.IntegrationTests;

internal sealed class TasteProfileInvalidationHarness : IAsyncDisposable
{
    private readonly PostgresFixture fixture;
    private readonly ServiceProvider provider;

    private TasteProfileInvalidationHarness(PostgresFixture fixture, ServiceProvider provider)
    {
        this.fixture = fixture;
        this.provider = provider;
    }

    public string ConnectionString => fixture.ConnectionString
        ?? throw new InvalidOperationException("The PostgreSQL fixture has no connection string.");

    public HybridCachedRecommendationProfileSource CachedProfileSource =>
        provider.GetRequiredService<HybridCachedRecommendationProfileSource>();

    public GenreWeightsCache GenreWeights => provider.GetRequiredService<GenreWeightsCache>();

    public static TasteProfileInvalidationHarness Create(PostgresFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var connectionString = fixture.ConnectionString
            ?? throw new InvalidOperationException("The PostgreSQL fixture has no connection string.");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = "C:/library",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = "not-a-real-key",
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://example.invalid/",
                [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
                [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        services.AddLogging();
        services.AddSingleton<IEnrichmentStatusNotifier, RecordingEnrichmentStatusNotifier>();
        services.AddLamuFlixPersistence(configuration);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        return new TasteProfileInvalidationHarness(fixture, provider);
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();

    public AsyncServiceScope CreateScope() => provider.CreateAsyncScope();

    public Task<TasteProfile> ReadProfileAsync(CancellationToken ct) =>
        provider.GetRequiredService<IRecommendationProfileSource>().GetTasteProfileAsync(ct);

    public async Task<GenreWeights> ReadGenreWeightsAsync(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<GenreWeightsCalculator>().GetAsync(ct);
    }

    public async Task RecordPlaybackAsync(MovieId movieId, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPlaybackHistory>()
            .RecordAsync(new PlaybackEvent(movieId, TimeProvider.System.GetUtcNow()), ct);
    }

    public async Task UpdateMovieAsync(MovieId movieId, Action<Movie> mutate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMovieRepository>();
        var movie = await repository.GetAsync(movieId, ct)
            ?? throw new InvalidOperationException($"Movie {movieId.Value} was not found.");
        mutate(movie);
        await repository.SaveChangesAsync(ct);
    }

    public async Task<MovieId> SeedMovieAsync(
        string title,
        bool isWatched,
        bool isNotInterested,
        string[] genres,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(genres);

        await using var db = fixture.CreateMigratedContext();
        var record = new MovieRecord
        {
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/{Guid.NewGuid():N}.mkv"),
            Format = new MediaFormat("mkv"),
            IsWatched = isWatched,
            IsNotInterested = isNotInterested,
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

    public async Task SeedPlaybackAsync(MovieId movieId, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        db.PlaybackEvents.Add(new PlaybackEventRecord
        {
            MovieId = movieId,
            PlayedAt = TimeProvider.System.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteMovieRowAsync(MovieId movieId, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        var record = await db.Movies.SingleAsync(movie => movie.Id == movieId, ct);
        db.Movies.Remove(record);
        await db.SaveChangesAsync(ct);
    }
}

internal sealed class CancelAfterSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        cancellation.Cancel();
        return ValueTask.FromResult(result);
    }
}
