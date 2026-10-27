using System;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
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
        roundTripped.ShouldNotBeNull();
        roundTripped.MovieId.Value.ShouldBe(42);
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
        var repository = new EfMovieRepository(
            context,
            timeProvider,
            options,
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
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

    [Fact]
    public async Task ImportMovieFolderHandler_OnPathA_CommitsMovieAndOutboxTogether()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = new LibraryPath("C:/library/atomic-import");
        var scanned = new ScannedMovie(
            new LibraryPath("C:/library/atomic-import/film.mkv"),
            "Atomic Import",
            new MediaFormat("mkv"),
            null,
            2048L);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.Scan(folder).Returns(scanned);
        var timeProvider = new FixedTimeProvider(StagedAt);
        await using var context = fixture.CreateMigratedContext();
        var handler = new ImportMovieFolderCommandHandler(
            scanner,
            CreateRepository(context, timeProvider),
            new EfEnrichmentOutbox(context, timeProvider));

        // act
        var id = await handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var storedMovie = await verification.Movies.AsNoTracking().SingleAsync(cancellationToken);
        storedMovie.Id.ShouldBe(id);
        storedMovie.Title.ShouldBe(scanned.Title);
        storedMovie.Status.ShouldBe(EnrichmentStatus.Pending);
        var stored = await verification.OutboxMessages.AsNoTracking().SingleAsync(cancellationToken);
        stored.CreatedAt.ShouldBe(StagedAt);
        stored.ProcessedAt.ShouldBeNull();
        var roundTripped = JsonSerializer.Deserialize<EnrichmentRequested>(stored.Payload, WebDefaults);
        roundTripped.ShouldNotBeNull();
        roundTripped.MovieId.ShouldBe(id);
        roundTripped.Attempt.ShouldBe(1);
    }

    [Fact]
    public async Task RequestEnrichmentHandler_OnPathA_CommitsMovieStateAndOutboxTogether()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FixedTimeProvider(StagedAt);
        var id = await SeedNotFoundMovieAsync(timeProvider, "Request Atomic", cancellationToken);
        await using var context = fixture.CreateMigratedContext();
        var handler = new RequestEnrichmentCommandHandler(
            CreateRepository(context, timeProvider),
            new EfEnrichmentOutbox(context, timeProvider));

        // act
        await handler.HandleAsync(new RequestEnrichmentCommand(id), cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var storedMovie = await CreateRepository(verification, timeProvider).GetAsync(id, cancellationToken);
        storedMovie.ShouldNotBeNull();
        storedMovie.Status.ShouldBe(EnrichmentStatus.Pending);
        var stored = await verification.OutboxMessages.AsNoTracking().SingleAsync(cancellationToken);
        stored.ProcessedAt.ShouldBeNull();
        var roundTripped = JsonSerializer.Deserialize<EnrichmentRequested>(stored.Payload, WebDefaults);
        roundTripped.ShouldNotBeNull();
        roundTripped.MovieId.ShouldBe(id);
        roundTripped.Attempt.ShouldBe(1);
    }

    [Fact]
    public async Task ImportMovieFolderHandler_WhenTheOutboxInsertFails_RollsBackTheMovieWrite()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = new LibraryPath("C:/library/rollback-import");
        var scanned = new ScannedMovie(
            new LibraryPath("C:/library/rollback-import/film.mkv"),
            "Rolled Back Import",
            new MediaFormat("mkv"),
            null,
            1024L);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.Scan(folder).Returns(scanned);
        await using var context = CreateContextWithOutboxInsertFailure();
        var handler = new ImportMovieFolderCommandHandler(
            scanner,
            CreateRepository(context, TimeProvider.System),
            new EfEnrichmentOutbox(context, TimeProvider.System));

        // act
        var failure = await Should.ThrowAsync<DbUpdateException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken));

        // assert
        failure.InnerException.ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldBe(OutboxInsertFailureInterceptor.FailureMessage);
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(0);
        (await verification.OutboxMessages.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task ImportMovieFolderHandler_WhenAnAmbientTransactionRollsBack_LeavesNoDurableChanges()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = new LibraryPath("C:/library/ambient-rollback");
        var scanned = new ScannedMovie(
            new LibraryPath("C:/library/ambient-rollback/film.mkv"),
            "Ambient Rollback",
            new MediaFormat("mkv"),
            null,
            512L);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.Scan(folder).Returns(scanned);
        await using var context = fixture.CreateMigratedContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var handler = new ImportMovieFolderCommandHandler(
            scanner,
            CreateRepository(context, TimeProvider.System),
            new EfEnrichmentOutbox(context, TimeProvider.System));
        await handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken);

        // act
        await transaction.RollbackAsync(cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(0);
        (await verification.OutboxMessages.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task RequestEnrichmentHandler_WhenAnAmbientTransactionRollsBack_PreservesThePreExistingMovie()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FixedTimeProvider(StagedAt);
        var id = await SeedNotFoundMovieAsync(timeProvider, "Request Rollback", cancellationToken);
        await using var context = fixture.CreateMigratedContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var handler = new RequestEnrichmentCommandHandler(
            CreateRepository(context, timeProvider),
            new EfEnrichmentOutbox(context, timeProvider));
        await handler.HandleAsync(new RequestEnrichmentCommand(id), cancellationToken);

        // act
        await transaction.RollbackAsync(cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var storedMovie = await CreateRepository(verification, timeProvider).GetAsync(id, cancellationToken);
        storedMovie.ShouldNotBeNull();
        storedMovie.Status.ShouldBe(EnrichmentStatus.NotFound);
        storedMovie.EnrichmentAttempts.ShouldBe(0);
        storedMovie.LastAttemptAt.ShouldBe(StagedAt);
        (await verification.Movies.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(1);
        (await verification.OutboxMessages.AsNoTracking().CountAsync(cancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task ImportMovieFolderHandler_WhenTheBrokerIsUnavailable_StillCommitsMovieAndUnprocessedOutbox()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var folder = new LibraryPath("C:/library/broker-failure");
        var scanned = new ScannedMovie(
            new LibraryPath("C:/library/broker-failure/film.mkv"),
            "Broker Failure",
            new MediaFormat("mkv"),
            null,
            4096L);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.Scan(folder).Returns(scanned);
        var timeProvider = new FixedTimeProvider(StagedAt);
        await using var context = fixture.CreateMigratedContext();
        var handler = new ImportMovieFolderCommandHandler(
            scanner,
            CreateRepository(context, timeProvider),
            new EfEnrichmentOutbox(context, timeProvider));

        // act
        var id = await handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken);

        // assert
        await using var verification = fixture.CreateMigratedContext();
        var storedMovie = await verification.Movies.AsNoTracking().SingleAsync(cancellationToken);
        storedMovie.Id.ShouldBe(id);
        storedMovie.Title.ShouldBe(scanned.Title);
        var stored = await verification.OutboxMessages.AsNoTracking().SingleAsync(cancellationToken);
        stored.CreatedAt.ShouldBe(StagedAt);
        stored.ProcessedAt.ShouldBeNull();
    }

    private static EfMovieRepository CreateRepository(LamuFlixDbContext context, TimeProvider timeProvider) =>
        new(
            context,
            timeProvider,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private async Task<MovieId> SeedNotFoundMovieAsync(
        TimeProvider timeProvider,
        string title,
        CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var repository = CreateRepository(context, timeProvider);
        var id = await repository.NextIdentityAsync(cancellationToken);
        var movie = Movie.Create(
            id,
            title,
            new LibraryPath($"C:/library/{title.Replace(' ', '-').ToLowerInvariant()}.mkv"),
            new MediaFormat("mkv"));
        movie.MarkNotFound(StagedAt);
        await repository.AddAsync(movie, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return id;
    }

    private LamuFlixDbContext CreateContextWithOutboxInsertFailure()
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.MaxBatchSize(1);
            })
            .AddInterceptors(new OutboxInsertFailureInterceptor())
            .Options;
        return new LamuFlixDbContext(options);
    }

    private sealed class OutboxInsertFailureInterceptor : DbCommandInterceptor
    {
        public const string FailureMessage = "controlled outbox insert failure";

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ThrowWhenOutboxInsert(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowWhenOutboxInsert(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            ThrowWhenOutboxInsert(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowWhenOutboxInsert(command);
            return ValueTask.FromResult(result);
        }

        private static void ThrowWhenOutboxInsert(DbCommand command)
        {
            if (command.CommandText.Contains("outbox_messages", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(FailureMessage);
            }
        }
    }
}
