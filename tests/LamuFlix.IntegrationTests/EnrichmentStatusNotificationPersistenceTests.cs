using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EnrichmentStatusNotificationPersistenceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveChanges_InsertInPending_NotifiesOnceWithAssignedIdAndStatusName()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Insert", "insert");

        // act
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(1);
        notifier.Notifications[0].MovieId.ShouldBe(id);
        notifier.Notifications[0].Status.ShouldBe(EnrichmentStatus.Pending);
        notifier.Notifications[0].CancellationToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task SaveChanges_EachActualTransition_NotifiesWithTheCommittedStatusOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Transitions", "transitions");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);

        // act
        movie.MarkEnriched(Metadata("Transitions", "tt0000101"), Now);
        await repository.SaveChangesAsync(ct);
        movie.RequestEnrichment();
        await repository.SaveChangesAsync(ct);
        movie.MarkNotFound(Now);
        await repository.SaveChangesAsync(ct);
        movie.RequestEnrichment();
        await repository.SaveChangesAsync(ct);
        movie.MarkFailed(EnrichmentFailureCategory.RateLimited, Now);
        await repository.SaveChangesAsync(ct);
        movie.MatchManually(new ImdbId("tt0000102"));
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Select(notification => notification.Status).ShouldBe(
        [
            EnrichmentStatus.Pending,
            EnrichmentStatus.Enriched,
            EnrichmentStatus.Pending,
            EnrichmentStatus.NotFound,
            EnrichmentStatus.Pending,
            EnrichmentStatus.Failed,
            EnrichmentStatus.Pending,
        ]);
        notifier.Notifications.ShouldAllBe(notification => notification.MovieId == id);
    }

    [Fact]
    public async Task SaveChanges_PendingNoOpAndRepeatedSaves_DoNotDuplicateNotifications()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(PendingMovie(id, "Noop", "noop"), ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);

        // act
        await repository.SaveChangesAsync(ct);
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_UnrelatedUpdate_DoesNotNotify()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Unrelated", "unrelated");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);

        // act
        movie.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_MultipleInMemoryMutations_NotifiesOnlyForTheFinalCommittedStatus()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Mutations", "mutations");
        movie.MarkEnriched(Metadata("Mutations", "tt0000103"), Now);
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);

        // act
        movie.RequestEnrichment();
        movie.MarkNotFound(Now);
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications[1].Status.ShouldBe(EnrichmentStatus.NotFound);
    }

    [Fact]
    public async Task SaveChanges_MultipleMovies_NotifiesOncePerTransitionedMovie()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var firstId = await repository.NextIdentityAsync(ct);
        var secondId = await repository.NextIdentityAsync(ct);
        var unchanged = PendingMovie(secondId, "Second", "second");
        await repository.AddAsync(unchanged, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);

        // act
        var first = PendingMovie(firstId, "First", "first");
        first.MarkEnriched(Metadata("First", "tt0000104"), Now);
        await repository.AddAsync(first, ct);
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications[1].MovieId.ShouldBe(firstId);
        notifier.Notifications[1].Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task SaveChanges_FailedDuplicateImdbSave_NotifiesNothingAndRecoveryNotifiesFailedOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var holder = PendingMovie(await repository.NextIdentityAsync(ct), "Holder", "holder");
        holder.MarkEnriched(Metadata("Holder", "tt0000105"), Now);
        await repository.AddAsync(holder, ct);
        await repository.SaveChangesAsync(ct);
        var loserId = await repository.NextIdentityAsync(ct);
        var loser = PendingMovie(loserId, "Loser", "loser");
        await repository.AddAsync(loser, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(2);

        // act
        loser.MarkEnriched(Metadata("Loser", "tt0000105"), Now);
        await Should.ThrowAsync<DuplicateImdbIdException>(() => repository.SaveChangesAsync(ct));

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        var recovered = await repository.GetAsync(loserId, ct);
        recovered.ShouldNotBeNull();
        recovered.Status.ShouldBe(EnrichmentStatus.Pending);
        recovered.MarkFailed(EnrichmentFailureCategory.DuplicateImdbId, Now);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(3);
        notifier.Notifications[2].MovieId.ShouldBe(loserId);
        notifier.Notifications[2].Status.ShouldBe(EnrichmentStatus.Failed);
    }

    [Fact]
    public async Task SaveChanges_AmbientTransactionRolledBackAfterIntermediateSave_NotifiesNothing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Rolled back", "rolled-back");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);
        movie.MarkEnriched(Metadata("Rolled back", "tt0000106"), Now);

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await repository.SaveChangesAsync(ct);
            notifier.Notifications.Count.ShouldBe(1);
            await transaction.RollbackAsync(ct);
        }

        movie.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_AmbientTransactionCommittedAfterIntermediateSave_NotifiesAfterTheEnclosingCommit()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Committed", "committed");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);
        movie.MarkEnriched(Metadata("Committed", "tt0000107"), Now);

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await repository.SaveChangesAsync(ct);
            notifier.Notifications.Count.ShouldBe(1);
            await transaction.CommitAsync(ct);
        }

        movie.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications[1].MovieId.ShouldBe(id);
        notifier.Notifications[1].Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task SaveChanges_CancelledBeforeCommit_NotifiesNothing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "Cancelled", "cancelled");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        movie.MarkEnriched(Metadata("Cancelled", "tt0000108"), Now);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(() => repository.SaveChangesAsync(cancelled.Token));

        // assert
        notifier.Notifications.Count.ShouldBe(1);
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.SingleAsync(ct)).Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task SaveChanges_ThrowingNotifier_KeepsCommittedSaveAndAttemptsEveryRemainingMovie()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        var attempts = new List<MovieId>();
        notifier.OnNotify = (movieId, _) =>
        {
            attempts.Add(movieId);
            if (attempts.Count == 1)
            {
                throw new InvalidOperationException("notifier exploded");
            }

            return Task.CompletedTask;
        };
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var firstId = await repository.NextIdentityAsync(ct);
        var secondId = await repository.NextIdentityAsync(ct);

        // act
        await repository.AddAsync(PendingMovie(firstId, "First", "throw-first"), ct);
        await repository.AddAsync(PendingMovie(secondId, "Second", "throw-second"), ct);
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        attempts.ShouldBe([firstId, secondId]);
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.CountAsync(ct)).ShouldBe(2);
    }

    [Fact]
    public async Task SaveChanges_CallerTokenCancelledDuringDispatch_StillAttemptsSendWithNoToken()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        using var callerCancellation = new CancellationTokenSource();
        // ReSharper disable once AccessToDisposedClosure deliberate: dispatch finishes before the test disposes the source
        notifier.OnNotify = (_, _) =>
        {
            callerCancellation.Cancel();
            return Task.CompletedTask;
        };
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var firstId = await repository.NextIdentityAsync(ct);
        var secondId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(PendingMovie(firstId, "First", "cancel-first"), ct);
        await repository.AddAsync(PendingMovie(secondId, "Second", "cancel-second"), ct);

        // act
        await repository.SaveChangesAsync(callerCancellation.Token);

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications.ShouldAllBe(notification => notification.CancellationToken == CancellationToken.None);
    }

    [Fact]
    public async Task SaveChanges_DirectRepositoryCallerWithoutHandler_NotifiesOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(PendingMovie(id, "Direct", "direct"), ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(Metadata("Direct", "tt0000109"), Now);
        await repository.SaveChangesAsync(ct);

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications[1].MovieId.ShouldBe(id);
        notifier.Notifications[1].Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task SaveChanges_TasteCacheFailsAfterCommit_PropagatesAndStillNotifies()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier, TasteCache(new InvalidOperationException("cache down")));
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(PendingMovie(id, "CacheFail", "cache-fail"), ct);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => repository.SaveChangesAsync(ct));

        // assert
        failure.Message.ShouldBe("cache down");
        notifier.Notifications.Count.ShouldBe(1);
        notifier.Notifications[0].MovieId.ShouldBe(id);
        notifier.Notifications[0].Status.ShouldBe(EnrichmentStatus.Pending);
        notifier.Notifications[0].CancellationToken.ShouldBe(CancellationToken.None);
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.SingleAsync(ct)).Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task SaveChanges_TasteCacheCancelledAfterCommit_PropagatesAndStillNotifies()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier, TasteCache(new OperationCanceledException()));
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(PendingMovie(id, "CacheCancelled", "cache-cancelled"), ct);

        // act
        await Should.ThrowAsync<OperationCanceledException>(() => repository.SaveChangesAsync(ct));

        // assert
        notifier.Notifications.Count.ShouldBe(1);
        notifier.Notifications[0].MovieId.ShouldBe(id);
        notifier.Notifications[0].Status.ShouldBe(EnrichmentStatus.Pending);
        notifier.Notifications[0].CancellationToken.ShouldBe(CancellationToken.None);
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.SingleAsync(ct)).Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task SaveChanges_TasteCacheFailsWithThrowingNotifier_PropagatesCacheFailureAndAttemptsEveryMovieOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier
        {
            OnNotify = (_, _) => throw new InvalidOperationException("notifier exploded"),
        };
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier, TasteCache(new InvalidOperationException("cache down")));
        var firstId = await repository.NextIdentityAsync(ct);
        var secondId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(PendingMovie(firstId, "CacheNotifierFirst", "cache-notifier-first"), ct);
        await repository.AddAsync(PendingMovie(secondId, "CacheNotifierSecond", "cache-notifier-second"), ct);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => repository.SaveChangesAsync(ct));

        // assert
        failure.Message.ShouldBe("cache down");
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications.Select(notification => notification.MovieId).ShouldBe([firstId, secondId]);
        notifier.Notifications.ShouldAllBe(notification => notification.CancellationToken == CancellationToken.None);
        await using var verification = fixture.CreateMigratedContext();
        (await verification.Movies.CountAsync(ct)).ShouldBe(2);

        // act
        await Should.ThrowAsync<InvalidOperationException>(() => repository.SaveChangesAsync(ct));

        // assert
        notifier.Notifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Dispose_CommittedAmbientTransition_NotifiesWithoutAnotherSave()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "DisposeCommit", "dispose-commit");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);
        movie.MarkEnriched(Metadata("DisposeCommit", "tt0000110"), Now);
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await repository.SaveChangesAsync(ct);
            notifier.Notifications.Count.ShouldBe(1);
            await transaction.CommitAsync(ct);
        }

        // act
        await repository.DisposeAsync();

        // assert
        notifier.Notifications.Count.ShouldBe(2);
        notifier.Notifications[1].MovieId.ShouldBe(id);
        notifier.Notifications[1].Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task Dispose_RolledBackAmbientTransition_NotifiesNothing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var notifier = new RecordingEnrichmentStatusNotifier();
        await using var db = fixture.CreateMigratedContext();
        var repository = CreateRepository(db, notifier);
        var id = await repository.NextIdentityAsync(ct);
        var movie = PendingMovie(id, "DisposeRollback", "dispose-rollback");
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        notifier.Notifications.Count.ShouldBe(1);
        movie.MarkEnriched(Metadata("DisposeRollback", "tt0000111"), Now);
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await repository.SaveChangesAsync(ct);
            notifier.Notifications.Count.ShouldBe(1);
            await transaction.RollbackAsync(ct);
        }

        // act
        await repository.DisposeAsync();

        // assert
        notifier.Notifications.Count.ShouldBe(1);
    }

    private static EfMovieRepository CreateRepository(LamuFlixDbContext db, IEnrichmentStatusNotifier notifier, HybridCachedRecommendationProfileSource? tasteProfileCache = null) =>
        new(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            notifier,
            NullLogger<EfMovieRepository>.Instance,
            tasteProfileCache: tasteProfileCache);

    private static HybridCachedRecommendationProfileSource TasteCache(Exception failure)
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        return new HybridCachedRecommendationProfileSource(
            new FailingHybridCache(failure),
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new TasteProfileCacheOptions { Expiration = TimeSpan.FromMinutes(5) }));
    }

    private sealed class FailingHybridCache(Exception failure) : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => throw failure;

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => throw failure;

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => throw failure;

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => throw failure;
    }

    private static Movie PendingMovie(MovieId id, string title, string pathSuffix) =>
        Movie.Create(id, title, new LibraryPath($"C:/library/{pathSuffix}.mkv"), new MediaFormat("mkv"));

    private static MovieMetadata Metadata(string title, string imdbId) =>
        new(
            title,
            "Synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId(imdbId));
}
