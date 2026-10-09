using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class StrandedMovieSweeperTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindStrandedMovieIdsAsync_SelectsOnlyNullOrStrictlyExpiredPendingRowsWithoutMutation()
    {
        var now = MovieCatalogSeed.FixedTime;
        var cutoff = now - TimeSpan.FromMinutes(5);
        await using (var context = fixture.CreateMigratedContext())
        {
            var strandedNull = MovieCatalogSeed.Create("null attempt", "sweeper-null");
            var strandedExpired = MovieCatalogSeed.Create("expired", "sweeper-expired");
            strandedExpired.LastAttemptAt = cutoff.AddTicks(-1);
            var fresh = MovieCatalogSeed.Create("fresh", "sweeper-fresh");
            fresh.LastAttemptAt = cutoff.AddTicks(1);
            var boundary = MovieCatalogSeed.Create("boundary", "sweeper-boundary");
            boundary.LastAttemptAt = cutoff;
            var failed = MovieCatalogSeed.Create("failed", "sweeper-failed");
            failed.Status = EnrichmentStatus.Failed;
            var enriched = MovieCatalogSeed.Create("enriched", "sweeper-enriched");
            enriched.Status = EnrichmentStatus.Enriched;
            enriched.LastAttemptAt = cutoff.AddTicks(-1);
            context.Movies.AddRange(strandedNull, strandedExpired, fresh, boundary, failed, enriched);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var expected = new[] { RequiredMovieId.From(strandedNull), RequiredMovieId.From(strandedExpired) };
            var originalAttempts = context.Movies.AsNoTracking().ToDictionary(movie => RequiredMovieId.From(movie), movie => movie.EnrichmentAttempts);
            var originalTimes = context.Movies.AsNoTracking().ToDictionary(movie => RequiredMovieId.From(movie), movie => movie.LastAttemptAt);
            var originalStatuses = context.Movies.AsNoTracking().ToDictionary(movie => RequiredMovieId.From(movie), movie => movie.Status);
            var repository = new EfMovieRepository(
                context,
                new FixedTimeProvider(now),
                Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

            var actual = await repository.FindStrandedMovieIdsAsync(cutoff, TestContext.Current.CancellationToken);

            Assert.Equal(expected.OrderBy(id => id.Value), actual.OrderBy(id => id.Value));
            var persisted = await context.Movies.AsNoTracking().ToArrayAsync(TestContext.Current.CancellationToken);
            Assert.All(persisted, movie => Assert.Equal(originalAttempts[RequiredMovieId.From(movie)], movie.EnrichmentAttempts));
            Assert.All(persisted, movie => Assert.Equal(originalTimes[RequiredMovieId.From(movie)], movie.LastAttemptAt));
            Assert.All(persisted, movie => Assert.Equal(originalStatuses[RequiredMovieId.From(movie)], movie.Status));
        }
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class StrandedMovieRecoveryTests(PostgresFixture postgres, RabbitMqFixture rabbitMq)
    : IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private readonly RabbitMqProbe probe = new(rabbitMq);

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync(TestContext.Current.CancellationToken);
        await rabbitMq.ResetTopologyAsync(TestContext.Current.CancellationToken);
        await probe.InitializeAsync();
    }

    public async ValueTask DisposeAsync() => await probe.DisposeAsync();

    [Fact]
    public async Task LostDualWriteGap_ActualSweeperPublishesAttemptOneWithoutMutatingBeforeClaim()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new ManualTimeProvider(MovieCatalogSeed.FixedTime);
        var movie = MovieCatalogSeed.Create("lost enqueue", "sweeper-lost-gap");
        await using (var context = postgres.CreateMigratedContext())
        {
            context.Movies.Add(movie);
            await context.SaveChangesAsync(ct);
        }

        var beforeSweep = await SnapshotAsync(RequiredMovieId.From(movie), ct);
        await using var factory = new ApiHostFactory(
            retainSweeper: true,
            additionalSettings: RealInfrastructureSettings(rabbitMq.Options, postgres.ConnectionString),
            timeProvider: clock,
            disableEnrichmentConsumer: true);

        _ = factory.Services;

        var message = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == RequiredMovieId.From(movie),
            ct);
        Assert.NotNull(message);
        Assert.Equal(1, Read(message).Attempt);
        Assert.Equal(beforeSweep, await SnapshotAsync(RequiredMovieId.From(movie), ct));

        await using var claimContext = postgres.CreateMigratedContext();
        var repository = new EfMovieRepository(
            claimContext,
            clock,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromSeconds(10) }));
        Assert.True(await repository.TryClaimForEnrichmentAsync(RequiredMovieId.From(movie), ct));
    }

    [Fact]
    public async Task StaleClaim_ActualSweeperPublishesAttemptOneWithoutAdditionalMutationBeforeClaim()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new ManualTimeProvider(MovieCatalogSeed.FixedTime);
        var movie = MovieCatalogSeed.Create("stale claim", "sweeper-stale-claim");
        await using (var context = postgres.CreateMigratedContext())
        {
            context.Movies.Add(movie);
            await context.SaveChangesAsync(ct);
        }

        await using (var claimContext = postgres.CreateMigratedContext())
        {
            var repository = new EfMovieRepository(
                claimContext,
                clock,
                Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromSeconds(10) }));
            Assert.True(await repository.TryClaimForEnrichmentAsync(RequiredMovieId.From(movie), ct));
        }

        clock.Advance(TimeSpan.FromSeconds(11));
        var beforeSweep = await SnapshotAsync(RequiredMovieId.From(movie), ct);
        await using var factory = new ApiHostFactory(
            retainSweeper: true,
            additionalSettings: RealInfrastructureSettings(rabbitMq.Options, postgres.ConnectionString),
            timeProvider: clock,
            disableEnrichmentConsumer: true);

        _ = factory.Services;

        var message = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == RequiredMovieId.From(movie),
            ct);
        Assert.NotNull(message);
        Assert.Equal(1, Read(message).Attempt);
        Assert.Equal(beforeSweep, await SnapshotAsync(RequiredMovieId.From(movie), ct));

        await using var nextClaimContext = postgres.CreateMigratedContext();
        var nextClaim = new EfMovieRepository(
            nextClaimContext,
            clock,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromSeconds(10) }));
        Assert.True(await nextClaim.TryClaimForEnrichmentAsync(RequiredMovieId.From(movie), ct));
    }

    private async Task<(EnrichmentStatus Status, DateTimeOffset? LastAttemptAt, int Attempts)> SnapshotAsync(
        MovieId id,
        CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        return await context.Movies.AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => new ValueTuple<EnrichmentStatus, DateTimeOffset?, int>(
                movie.Status, movie.LastAttemptAt, movie.EnrichmentAttempts))
            .SingleAsync(cancellationToken);
    }

    private static EnrichmentRequested Read(BasicGetResult message) =>
        JsonSerializer.Deserialize<EnrichmentRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Enrichment request payload was empty.");

    private static IEnumerable<KeyValuePair<string, string?>> RealInfrastructureSettings(
        RabbitMqOptions rabbit,
        string connectionString) =>
    [
        new("ConnectionStrings:DefaultConnection", connectionString),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}", rabbit.HostName),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Port)}", rabbit.Port.ToString()),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.UserName)}", rabbit.UserName),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Password)}", rabbit.Password),
        new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.RetryDelay)}", rabbit.RetryDelay.ToString("c")),
        new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}", "00:00:01"),
        new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}", "00:01:00"),
    ];
}

file static class RequiredMovieId
{
    public static MovieId From(MovieRecord movie) =>
        movie.Id ?? throw new InvalidOperationException("Persisted movie is missing an id.");
}