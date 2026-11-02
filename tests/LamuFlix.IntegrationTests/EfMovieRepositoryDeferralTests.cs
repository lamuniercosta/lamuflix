using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieRepositoryDeferralTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Migration_AddEnrichmentDeferredUntil_NullRowsRemainSweeperEligible()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await SeedAsync(db, detail: null, lastAttemptAt: null);

        // act
        var stranded = await repository.FindStrandedMovieIdsAsync(Now, ct);

        // assert
        stranded.ShouldContain(id);
        (await db.Movies.AsNoTracking().Where(movie => movie.Id == id)
                .Select(movie => movie.EnrichmentDeferredUntil)
                .SingleAsync(ct))
            .ShouldBeNull();
    }

    [Fact]
    public async Task TryClaimForEnrichmentAsync_BeforeDeferral_Refuses()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await SeedAsync(db, Now.AddHours(1), lastAttemptAt: null);

        // act
        var claimed = await repository.TryClaimForEnrichmentAsync(id, ct);

        // assert
        claimed.ShouldBeFalse();
        var record = await db.Movies.AsNoTracking().SingleAsync(movie => movie.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(0);
        record.LastAttemptAt.ShouldBeNull();
        record.EnrichmentDeferredUntil.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public async Task TryClaimForEnrichmentAsync_AfterDeferral_ClearsDeadlineAtomicallyWithLastAttemptAt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await SeedAsync(db, Now.AddMinutes(-1), lastAttemptAt: null);

        // act
        var claimed = await repository.TryClaimForEnrichmentAsync(id, ct);

        // assert
        claimed.ShouldBeTrue();
        var record = await db.Movies.AsNoTracking().SingleAsync(movie => movie.Id == id, ct);
        record.EnrichmentDeferredUntil.ShouldBeNull();
        record.LastAttemptAt.ShouldBe(Now);
        record.EnrichmentAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task FindStrandedMovieIdsAsync_FutureDeferral_Excluded()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await SeedAsync(db, Now.AddHours(1), lastAttemptAt: null);

        // act
        var stranded = await repository.FindStrandedMovieIdsAsync(Now, ct);

        // assert
        stranded.ShouldNotContain(id);
    }

    [Fact]
    public async Task FindStrandedMovieIdsAsync_ExpiredDeferral_Included()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await SeedAsync(db, Now.AddMinutes(-1), lastAttemptAt: null);

        // act
        var stranded = await repository.FindStrandedMovieIdsAsync(Now, ct);

        // assert
        stranded.ShouldContain(id);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db, DateTimeOffset now) =>
        new(
            db,
            new FixedTimeProvider(now),
            Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private static async Task<MovieId> SeedAsync(
        LamuFlixDbContext db,
        DateTimeOffset? detail,
        DateTimeOffset? lastAttemptAt)
    {
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        db.Movies.Add(new MovieRecord
        {
            Id = id,
            Title = $"Deferred {id.Value}",
            LibraryPath = new LibraryPath($"C:/library/deferred-{id.Value}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending,
            EnrichmentDeferredUntil = detail,
            LastAttemptAt = lastAttemptAt,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        return id;
    }
}
