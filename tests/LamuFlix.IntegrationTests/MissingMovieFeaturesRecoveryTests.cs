using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using LamuFlix.UnitTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T018: live PostgreSQL and RabbitMQ proof of the independent missing-features recovery pass
/// and the dual-pass <see cref="StrandedMovieSweeper"/> behavior.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class MissingMovieFeaturesRecoveryTests(PostgresFixture postgres, RabbitMqFixture rabbitMq)
    : IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ClaimLease = TimeSpan.FromSeconds(10);

    private readonly ManualTimeProvider clock = new(MovieCatalogSeed.FixedTime);
    private readonly RabbitMqProbe probe = new(rabbitMq);

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync(TestContext.Current.CancellationToken);
        await rabbitMq.ResetTopologyAsync(TestContext.Current.CancellationToken);
        await probe.InitializeAsync();
    }

    public async ValueTask DisposeAsync() => await probe.DisposeAsync();

    [Fact]
    public async Task Handler_SelectsOnlyNonblankPlotWithoutFeaturesRegardlessOfEnrichmentStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = postgres.CreateMigratedContext();
        var pending = Seed(context, "pending plot", "A pending movie plot.");
        var enriched = Seed(context, "enriched plot", "An enriched movie plot.", EnrichmentStatus.Enriched);
        var failed = Seed(context, "failed plot", "A failed movie plot.", EnrichmentStatus.Failed);
        Seed(context, "empty plot", string.Empty);
        Seed(context, "spaces plot", "   \t\r\n ");
        Seed(context, "unicode blank plot", "\u00A0\u2003\u2009");
        Seed(context, "null plot", null);
        var existing = Seed(context, "existing features", "Already has features.");
        await context.SaveChangesAsync(ct);
        context.MovieFeatures.Add(new MovieFeaturesRecord
        {
            MovieId = Key(existing),
            Vector = new Vector(Enumerable.Repeat(0.1f, 384).ToArray()),
            ComputedAt = MovieCatalogSeed.FixedTime,
        });
        await context.SaveChangesAsync(ct);

        var expected = new[] { Key(pending), Key(enriched), Key(failed) };
        var before = await SnapshotAsync(context, ct);

        await using var owner = new RabbitMqConnectionOwner(Options.Create(rabbitMq.Options));
        var handler = new SweepMissingMovieFeaturesCommandHandler(
            new EfMovieFeaturesRepository(context),
            CreatePublisher(owner));

        var count = await handler.HandleAsync(new SweepMissingMovieFeaturesCommand(), ct);

        count.ShouldBe(expected.Length);
        var found = await probe.PollGetAllAsync(
            RabbitMqTopology.FeaturesRequestedQueue,
            expected.Select(id => id.Value.ToString(CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal),
            message => ReadFeatures(message).MovieId.Value.ToString(CultureInfo.InvariantCulture),
            ct);
        found.Keys.ShouldBe(
            expected.Select(id => id.Value.ToString(CultureInfo.InvariantCulture)),
            ignoreOrder: true);
        found.Values.ShouldAllBe(message => ReadFeatures(message).Attempt == 1);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.FeaturesRequestedQueue, ct)).MessageCount.ShouldBe(0U);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, ct)).MessageCount.ShouldBe(0U);

        context.ChangeTracker.Clear();
        (await context.MovieFeatures.AsNoTracking().CountAsync(ct)).ShouldBe(1);
        (await SnapshotAsync(context, ct)).ShouldBe(before);
    }

    [Fact]
    public async Task Handler_AfterAPriorFeatureDeadLetter_EnqueuesFreshAttemptOneWithoutDrainingTheDeadLetterQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = postgres.CreateMigratedContext();
        var movie = Seed(context, "prior dead letter", "A movie whose prior feature chain dead-lettered.");
        await context.SaveChangesAsync(ct);

        await using var owner = new RabbitMqConnectionOwner(Options.Create(rabbitMq.Options));
        var publisher = CreatePublisher(owner);
        await publisher.PublishAsync(
            new FeaturesRequested(Key(movie), 3),
            EnrichmentRouting.FeaturesDeadLetterRoutingKey,
            new Dictionary<string, object?>(),
            ct);
        (await probe.PollMessageCountAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).ShouldBe(1U);

        var handler = new SweepMissingMovieFeaturesCommandHandler(
            new EfMovieFeaturesRepository(context),
            publisher);
        var count = await handler.HandleAsync(new SweepMissingMovieFeaturesCommand(), ct);

        count.ShouldBe(1);
        var requested = await probe.PollGetAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
        requested.ShouldNotBeNull();
        ReadFeatures(requested).MovieId.ShouldBe(Key(movie));
        ReadFeatures(requested).Attempt.ShouldBe(1);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).MessageCount.ShouldBe(1U);
    }

    [Fact]
    public async Task Handler_WhenEnqueueFails_LeavesTheMovieEligibleForTheNextSweep()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = postgres.CreateMigratedContext();
        var movie = Seed(context, "enqueue failure", "A movie whose first recovery enqueue fails.");
        await context.SaveChangesAsync(ct);

        var failing = new SweepMissingMovieFeaturesCommandHandler(
            new EfMovieFeaturesRepository(context),
            new ThrowingQueue());
        await Should.ThrowAsync<InvalidOperationException>(
            () => failing.HandleAsync(new SweepMissingMovieFeaturesCommand(), ct));

        context.ChangeTracker.Clear();
        (await context.MovieFeatures.AsNoTracking().CountAsync(ct)).ShouldBe(0);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.FeaturesRequestedQueue, ct)).MessageCount.ShouldBe(0U);

        await using var owner = new RabbitMqConnectionOwner(Options.Create(rabbitMq.Options));
        var retry = new SweepMissingMovieFeaturesCommandHandler(
            new EfMovieFeaturesRepository(context),
            CreatePublisher(owner));
        (await retry.HandleAsync(new SweepMissingMovieFeaturesCommand(), ct)).ShouldBe(1);

        var requested = await probe.PollGetAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
        requested.ShouldNotBeNull();
        ReadFeatures(requested).MovieId.ShouldBe(Key(movie));
        ReadFeatures(requested).Attempt.ShouldBe(1);
    }

    [Fact]
    public async Task Sweeper_RunsTheFeaturePassWhenTheEnrichmentPassHasNoCandidates()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var context = postgres.CreateMigratedContext())
        {
            Seed(context, "not stranded", "An enriched movie still needs features.", EnrichmentStatus.Enriched);
            await context.SaveChangesAsync(ct);
        }

        await using var composition = CreateComposition();

        await composition.Sweeper.StartAsync(ct);
        var requested = await probe.PollGetAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);

        requested.ShouldNotBeNull();
        ReadFeatures(requested).Attempt.ShouldBe(1);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, ct)).MessageCount.ShouldBe(0U);
    }

    [Fact]
    public async Task Sweeper_AnEnrichmentPassFailureDoesNotSuppressTheFeaturePass()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var context = postgres.CreateMigratedContext())
        {
            Seed(context, "feature pass survives", "Feature recovery still runs after a failure.");
            await context.SaveChangesAsync(ct);
        }

        await using var composition = CreateComposition(faultingEnrichment: true);

        await composition.Sweeper.StartAsync(ct);
        var requested = await probe.PollGetAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);

        requested.ShouldNotBeNull();
        ReadFeatures(requested).Attempt.ShouldBe(1);
    }

    [Fact]
    public async Task Sweeper_AFeaturePassFailureDoesNotSuppressTheEnrichmentPass()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var context = postgres.CreateMigratedContext())
        {
            Seed(context, "stranded enrichment", "Enrichment recovery still runs after a feature failure.");
            await context.SaveChangesAsync(ct);
        }

        await using var composition = CreateComposition(faultingFeature: true);

        await composition.Sweeper.StartAsync(ct);
        var requested = await probe.PollGetAsync(RabbitMqTopology.RequestedQueue, ct);

        requested.ShouldNotBeNull();
        ReadEnrichment(requested).Attempt.ShouldBe(1);
    }

    private Composition CreateComposition(bool faultingEnrichment = false, bool faultingFeature = false)
    {
        var enrichment = new EnrichmentOptions
        {
            MaxAttempts = 3,
            ClaimLease = ClaimLease,
            SweepInterval = SweepInterval,
        };
        var rabbit = rabbitMq.Options;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(Options.Create(enrichment));
        services.AddSingleton(Options.Create(rabbit));

        var owner = new RabbitMqConnectionOwner(Options.Create(rabbit));
        services.AddSingleton(owner);
        var topology = new RabbitMqTopology(owner, Options.Create(rabbit), Options.Create(enrichment));
        services.AddSingleton(topology);
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        services.AddSingleton(publisher);
        services.AddSingleton<IEnrichmentQueue>(publisher);

        services.AddScoped(_ => postgres.CreateMigratedContext());
        services.AddScoped<IMovieRepository>(provider => new EfMovieRepository(
            provider.GetRequiredService<LamuFlixDbContext>(),
            clock,
            Options.Create(enrichment)));
        services.AddScoped<IMovieFeaturesRepository>(provider =>
            new EfMovieFeaturesRepository(provider.GetRequiredService<LamuFlixDbContext>()));

        if (faultingEnrichment)
        {
            services.AddScoped<ICommandHandler<SweepStrandedMoviesCommand, int>>(_ => new ThrowingSweepHandler());
        }
        else
        {
            services.AddScoped<ICommandHandler<SweepStrandedMoviesCommand, int>, SweepStrandedMoviesCommandHandler>();
            services.AddScoped<ICommandHandler<RequeueStrandedMoviesCommand, int>, RequeueStrandedMoviesCommandHandler>();
        }

        if (faultingFeature)
        {
            services.AddScoped<ICommandHandler<SweepMissingMovieFeaturesCommand, int>>(_ =>
                new ThrowingFeatureSweepHandler());
        }
        else
        {
            services.AddScoped<ICommandHandler<SweepMissingMovieFeaturesCommand, int>,
                SweepMissingMovieFeaturesCommandHandler>();
        }

        var provider = services.BuildServiceProvider();
        var sweeper = new StrandedMovieSweeper(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(enrichment),
            clock,
            provider.GetRequiredService<ILogger<StrandedMovieSweeper>>());
        return new Composition(provider, owner, sweeper);
    }

    private RabbitMqEnrichmentQueuePublisher CreatePublisher(RabbitMqConnectionOwner owner) =>
        new(
            new RabbitMqTopology(
                owner,
                Options.Create(rabbitMq.Options),
                Options.Create(new EnrichmentOptions { MaxAttempts = 3 })),
            owner);

    // ReSharper disable once NullableWarningSuppressionIsUsed - seeded rows always carry an assigned key.
    private static MovieId Key(MovieRecord movie) => movie.Id!;

    private static MovieRecord Seed(
        LamuFlixDbContext context,
        string title,
        string? plot,
        EnrichmentStatus? status = null,
        DateTimeOffset? lastAttemptAt = null)
    {
        var movie = MovieCatalogSeed.Create(title, title.Replace(' ', '-'));
        movie.Plot = plot;
        movie.Status = status ?? EnrichmentStatus.Pending;
        movie.LastAttemptAt = lastAttemptAt;
        context.Movies.Add(movie);
        return movie;
    }

    private static async Task<string> SnapshotAsync(LamuFlixDbContext context, CancellationToken cancellationToken)
    {
        var rows = await context.Movies.AsNoTracking().ToArrayAsync(cancellationToken);
        return string.Join(
            "|",
            rows.OrderBy(row => Key(row).Value).Select(row => string.Create(
                CultureInfo.InvariantCulture,
                $"{Key(row).Value}:{row.Status.Name}:{row.EnrichmentAttempts}:{row.LastAttemptAt?.UtcTicks}")));
    }

    private static FeaturesRequested ReadFeatures(RabbitMQ.Client.BasicGetResult message) =>
        JsonSerializer.Deserialize<FeaturesRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            SerializerOptions)
        ?? throw new InvalidOperationException("Feature request payload was empty.");

    private static EnrichmentRequested ReadEnrichment(RabbitMQ.Client.BasicGetResult message) =>
        JsonSerializer.Deserialize<EnrichmentRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            SerializerOptions)
        ?? throw new InvalidOperationException("Enrichment request payload was empty.");

    private sealed class Composition(
        ServiceProvider provider,
        RabbitMqConnectionOwner owner,
        StrandedMovieSweeper sweeper) : IAsyncDisposable
    {
        public StrandedMovieSweeper Sweeper { get; } = sweeper;

        public async ValueTask DisposeAsync()
        {
            await Sweeper.StopAsync(CancellationToken.None);
            Sweeper.Dispose();
            await provider.DisposeAsync();
            await owner.DisposeAsync();
        }
    }

    private sealed class ThrowingQueue : IEnrichmentQueue
    {
        public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct) =>
            throw new InvalidOperationException("Enqueue failed for the recovery test.");

        public Task EnqueueAsync(FeaturesRequested message, CancellationToken ct) =>
            throw new InvalidOperationException("Enqueue failed for the recovery test.");
    }

    private sealed class ThrowingSweepHandler : ICommandHandler<SweepStrandedMoviesCommand, int>
    {
        public Task<int> HandleAsync(SweepStrandedMoviesCommand command, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Enrichment pass failed for the recovery test.");
    }

    private sealed class ThrowingFeatureSweepHandler : ICommandHandler<SweepMissingMovieFeaturesCommand, int>
    {
        public Task<int> HandleAsync(SweepMissingMovieFeaturesCommand command, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Feature pass failed for the recovery test.");
    }
}
