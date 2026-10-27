using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using LamuFlix.UnitTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T012: live-broker redelivery through the real EfMovieFeaturesRepository upsert leaves
/// exactly one movie_features row and never mutates the metadata enrichment state.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class FeaturesConsumerPersistenceTests(PostgresFixture postgres, RabbitMqFixture rabbitMq)
    : IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset ComputedAt = new(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan PollDeadline = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private const string StoredPlot = "A persisted plot for the redelivery proof.";

    private readonly RabbitMqProbe probe = new(rabbitMq);

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        await rabbitMq.ResetTopologyAsync(ct);
        await probe.InitializeAsync();
    }

    public async ValueTask DisposeAsync() => await probe.DisposeAsync();

    [Fact]
    public async Task FeaturesConsumer_ConcurrentRedeliveryOfTheSameMovie_LeavesExactlyOneRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var movie = await SeedMovieAsync("features-concurrent-redelivery", ct);
        var movies = new FakeEmbeddingGenerator();
        await using var host = NewHost(movies);

        await host.StartAsync(ct);
        await Task.WhenAll(
            host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct),
            host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 2), ct));
        var stored = await WaitForSnapshotAsync(host, movie, ct);
        await WaitForRowCountAsync(1, ct);
        await host.StopAsync(CancellationToken.None);

        stored.MovieId.ShouldBe(movie);
        stored.ComputedAt.ShouldBe(ComputedAt);
        var expected = (await movies.GenerateAsync([StoredPlot], cancellationToken: ct))[0].Vector.ToArray();
        stored.Vector.ToArray().ShouldBe(expected);
        await AssertMetadataStateUnchangedAsync(movie, ct);
    }

    [Fact]
    public async Task FeaturesConsumer_SequentialRedeliveryOfTheSameMovie_LeavesExactlyOneRowAndRunsAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        var movie = await SeedMovieAsync("features-sequential-redelivery", ct);
        var embeddings = new FakeEmbeddingGenerator();
        await using var host = NewHost(embeddings);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
        await WaitForSnapshotAsync(host, movie, ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 2), ct);
        await WaitForRowCountAsync(1, ct);
        await host.StopAsync(CancellationToken.None);

        await using var context = postgres.CreateMigratedContext();
        (await context.MovieFeatures.CountAsync(ct)).ShouldBe(1);
        var stored = await new EfMovieFeaturesRepository(context).GetByMovieIdAsync(movie, ct);
        stored.ShouldNotBeNull();
        stored.ComputedAt.ShouldBe(ComputedAt);
        await AssertMetadataStateUnchangedAsync(movie, ct);
    }

    private async Task<MovieId> SeedMovieAsync(string pathSuffix, CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        var record = new MovieRecord
        {
            Title = "Redelivery Owner",
            LibraryPath = new LibraryPath($"C:/library/{pathSuffix}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending,
            Plot = StoredPlot,
        };
        await MovieCatalogSeed.AddAsync(context, record, cancellationToken);
        return record.Id ?? throw new InvalidOperationException("The seeded movie has no id.");
    }

    private static async Task<MovieFeaturesSnapshot> WaitForSnapshotAsync(
        PersistenceHost host,
        MovieId movie,
        CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + PollDeadline;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            await using var context = host.CreateContext();
            var stored = await new EfMovieFeaturesRepository(context).GetByMovieIdAsync(movie, cancellationToken);
            if (stored is not null)
            {
                return stored;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new InvalidOperationException($"The features consumer did not persist movie {movie.Value} in time.");
    }

    private async Task WaitForRowCountAsync(int expected, CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + PollDeadline;
        var count = 0;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            await using var context = postgres.CreateMigratedContext();
            count = await context.MovieFeatures.CountAsync(cancellationToken);
            if (count == expected)
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        count.ShouldBe(expected);
    }

    private async Task AssertMetadataStateUnchangedAsync(MovieId movie, CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        var persisted = await context.Movies.AsNoTracking()
            .Where(candidate => candidate.Id == movie)
            .Select(candidate => new ValueTuple<EnrichmentStatus, int>(candidate.Status, candidate.EnrichmentAttempts))
            .SingleAsync(cancellationToken);
        persisted.Item1.ShouldBe(EnrichmentStatus.Pending);
        persisted.Item2.ShouldBe(0);
    }

    private PersistenceHost NewHost(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        var options = rabbitMq.Options with { Prefetch = 2 };
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var enrichment = Options.Create(new EnrichmentOptions
        {
            MaxAttempts = 3,
            ClaimLease = TimeSpan.FromSeconds(5),
        });
        var topology = new RabbitMqTopology(owner, Options.Create(options), enrichment);
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        var services = new ServiceCollection();
        services.AddDbContext<LamuFlixDbContext>(builder => builder.UseNpgsql(
            postgres.ConnectionString,
            database => database.UseVector()));
        services.AddLogging();
        services.AddSingleton(enrichment);
        services.AddSingleton(enrichment.Value);
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(ComputedAt));
        services.AddSingleton<IEnrichmentStatusNotifier, RecordingEnrichmentStatusNotifier>();
        services.AddScoped<IMovieRepository, EfMovieRepository>();
        services.AddScoped<IMovieFeaturesRepository, EfMovieFeaturesRepository>();
        services.AddSingleton(generator);
        services.AddSingleton<RabbitMqConnectionOwner>(_ => owner);
        services.AddSingleton(topology);
        services.AddSingleton(publisher);
        services.AddHandler<ProcessFeaturesCommandHandler, ProcessFeaturesCommand, ProcessFeaturesOutcome>();

        var built = services.BuildServiceProvider();
        var consumer = new FeaturesConsumer(
            owner,
            topology,
            publisher,
            built.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<FeaturesConsumer>.Instance,
            new FeaturesConsumerLiveness(),
            built.GetRequiredService<TimeProvider>());
        var host = new PersistenceHost(owner, publisher, consumer, built, postgres);
        return host;
    }

    private sealed class PersistenceHost(
        RabbitMqConnectionOwner owner,
        RabbitMqEnrichmentQueuePublisher publisher,
        FeaturesConsumer consumer,
        ServiceProvider services,
        PostgresFixture postgres) : IAsyncDisposable
    {
        public RabbitMqEnrichmentQueuePublisher Publisher { get; } = publisher;

        public Task StartAsync(CancellationToken cancellationToken) => consumer.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => consumer.StopAsync(cancellationToken);

        public LamuFlixDbContext CreateContext() => postgres.CreateMigratedContext();

        public ValueTask DisposeAsync()
        {
            services.Dispose();
            return owner.DisposeAsync();
        }
    }
}
