using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EnrichmentOutboxPersistenceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions WebDefaults = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset StagedAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Stage_ThenSharedSave_StoresPublisherParityPayloadThatRoundTripsForConsumers()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new EnrichmentRequested(new MovieId(42), 3);
        await using var context = fixture.CreateMigratedContext();
        var outbox = new EfEnrichmentOutbox(context, new FixedTimeProvider(StagedAt));

        // act
        outbox.Stage(request);
        await context.SaveChangesAsync(cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var stored = await verification.OutboxMessages.AsNoTracking().SingleAsync(cancellationToken);
        var publisherBytes = JsonSerializer.SerializeToUtf8Bytes(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        stored.Payload.ShouldBe(publisherBytes);
        var roundTripped = JsonSerializer.Deserialize<EnrichmentRequested>(stored.Payload, WebDefaults);
        roundTripped.ShouldBe(request);
        roundTripped!.MovieId.Value.ShouldBe(42);
        roundTripped.Attempt.ShouldBe(3);
    }

    [Fact]
    public async Task Stage_UsesInjectedTimeProviderAndLeavesProcessedAtNull()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var outbox = new EfEnrichmentOutbox(context, new FixedTimeProvider(StagedAt));

        // act
        outbox.Stage(new EnrichmentRequested(new MovieId(7), 1));
        await context.SaveChangesAsync(cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var stored = await verification.OutboxMessages.AsNoTracking().SingleAsync(cancellationToken);
        stored.CreatedAt.ShouldBe(StagedAt);
        stored.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Stage_RepeatedForTheSameRequest_PersistsDistinctRows()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new EnrichmentRequested(new MovieId(11), 2);
        await using var context = fixture.CreateMigratedContext();
        var outbox = new EfEnrichmentOutbox(context, new FixedTimeProvider(StagedAt));

        // act
        outbox.Stage(request);
        outbox.Stage(request);
        await context.SaveChangesAsync(cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var stored = await verification.OutboxMessages.AsNoTracking()
            .OrderBy(message => message.Id)
            .ToListAsync(cancellationToken);
        stored.Count.ShouldBe(2);
        stored[0].Id.ShouldNotBe(stored[1].Id);
        stored.Select(message => message.Payload)
            .ShouldAllBe(payload => payload.SequenceEqual(stored[0].Payload));
        stored.ShouldAllBe(message => message.ProcessedAt == null);
    }

    [Fact]
    public async Task Stage_BeforeTheSharedSave_LeavesNoDurableRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var outbox = new EfEnrichmentOutbox(context, new FixedTimeProvider(StagedAt));

        // act
        outbox.Stage(new EnrichmentRequested(new MovieId(13), 1));
        await using var beforeSave = fixture.CreateMigratedContext();
        var durableBefore = await beforeSave.OutboxMessages.AsNoTracking().CountAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await using var afterSave = fixture.CreateMigratedContext();
        var durableAfter = await afterSave.OutboxMessages.AsNoTracking().CountAsync(cancellationToken);

        // assert
        durableBefore.ShouldBe(0);
        durableAfter.ShouldBe(1);
    }

    [Fact]
    public async Task Stage_WithSharedMovieWrite_SavesMovieAndOutboxTogetherOnPathA()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateMigratedContext();
        var timeProvider = new FixedTimeProvider(StagedAt);
        var options = Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) });
        var repository = new EfMovieRepository(context, timeProvider, options);
        var outbox = new EfEnrichmentOutbox(context, timeProvider);
        var id = await repository.NextIdentityAsync(cancellationToken);
        var movie = Movie.Create(
            id,
            "Outbox Together",
            new LibraryPath("C:/library/outbox-together.mkv"),
            new MediaFormat("mkv"));
        await repository.AddAsync(movie, cancellationToken);

        // act
        outbox.Stage(new EnrichmentRequested(id, 1));
        await repository.SaveChangesAsync(cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(1);
        var stored = await verification.OutboxMessages.AsNoTracking().SingleAsync(cancellationToken);
        stored.CreatedAt.ShouldBe(StagedAt);
        stored.ProcessedAt.ShouldBeNull();
        var roundTripped = JsonSerializer.Deserialize<EnrichmentRequested>(stored.Payload, WebDefaults);
        roundTripped.ShouldNotBeNull();
        roundTripped.MovieId.ShouldBe(id);
        roundTripped.Attempt.ShouldBe(1);
    }
}
