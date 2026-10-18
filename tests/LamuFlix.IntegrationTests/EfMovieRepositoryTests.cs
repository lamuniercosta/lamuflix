using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfMovieRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AddAndGet_RoundTripAggregateAndReturnIdentityMappedInstance()
    {
        TestContext.Current.TestOutputHelper?.WriteLine($"[postgres-container-id] {fixture.Container.Id}");
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var metadata = new MovieMetadata(
            "Metadata title", "Synopsis", new ReleaseYear(2024, Now), new Runtime(121),
            new ImdbRating(8.1m), new ImdbId("tt1234567"));
        var movie = Movie.Rehydrate(
            id, "Library title", new LibraryPath("C:/library/movie.mkv"), new MediaFormat("mkv"),
            true, metadata, EnrichmentStatus.Failed, Now.AddDays(-1), 2,
            EnrichmentFailureCategory.RateLimited, Now.AddHours(-1));

        await repository.AddAsync(movie, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await repository.GetAsync(id, TestContext.Current.CancellationToken)).ShouldBeSameAs(movie);
        await using var read = OpenSibling(db);
        var loaded = await Repository(read).GetAsync(id, TestContext.Current.CancellationToken);
        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(id);
        loaded.Title.ShouldBe(movie.Title);
        loaded.Path.ShouldBe(movie.Path);
        loaded.Format.ShouldBe(movie.Format);
        loaded.IsInWatchlist.ShouldBeTrue();
        loaded.Metadata.ShouldBe(metadata);
        loaded.Status.ShouldBe(EnrichmentStatus.Failed);
        loaded.EnrichedAt.ShouldBe(Now.AddDays(-1));
        loaded.EnrichmentAttempts.ShouldBe(2);
        loaded.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        loaded.LastAttemptAt.ShouldBe(Now.AddHours(-1));
    }

    [Fact]
    public async Task GetAsync_AbsentId_ReturnsNull()
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        (await repository.GetAsync(id, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var movie = Movie.Create(id, "Movie", new LibraryPath("C:/library/movie.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, TestContext.Current.CancellationToken);
        await Should.ThrowAsync<InvalidOperationException>(() =>
            repository.AddAsync(movie, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_PreservesRecordOnlyData_AndPersistsDomainTransitions()
    {
        await using var db = fixture.CreateMigratedContext();
        var id = await Repository(db).NextIdentityAsync(TestContext.Current.CancellationToken);
        var record = new MovieRecord
        {
            Id = id,
            Title = "Library title",
            LibraryPath = new LibraryPath("C:/library/preserve.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending,
            RottenTomatoesRating = 91,
            MetaScore = 82,
            PosterUrl = "https://example.test/poster.jpg",
        };
        record.Actors.Add(new ActorRecord { Name = "Ada Actor" });
        record.Directors.Add(new DirectorRecord { Name = "Dana Director" });
        record.Genres.Add(new GenreRecord { Name = "Drama" });
        db.Movies.Add(record);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = Repository(db);
        var movie = await repository.GetAsync(id, TestContext.Current.CancellationToken);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            new MovieMetadata(
                "Metadata", "Plot", runtime: new Runtime(98), metaScore: 82,
                directors: ["Dana Director"], actors: ["Ada Actor"], genres: ["Drama"]),
            Now);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        movie.AddToWatchlist();
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verify = OpenSibling(db);
        var loaded = await verify.Movies.Include(item => item.Actors).Include(item => item.Directors)
            .Include(item => item.Genres).Include(item => item.Writers)
            .SingleAsync(item => item.Id == id, TestContext.Current.CancellationToken);
        loaded.Status.ShouldBe(EnrichmentStatus.Enriched);
        loaded.IsInWatchlist.ShouldBeTrue();
        loaded.RottenTomatoesRating.ShouldBe((short)91);
        loaded.MetaScore.ShouldBe((short)82);
        loaded.PosterUrl.ShouldBeNull();
        loaded.Plot.ShouldBe("Plot");
        loaded.Actors.ShouldContain(actor => actor.Name == "Ada Actor");
        loaded.Directors.ShouldContain(director => director.Name == "Dana Director");
        loaded.Genres.ShouldContain(genre => genre.Name == "Drama");
        loaded.Writers.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAndGet_EnrichedMetadataWithPoster_PersistsEveryMetadataField()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var metadata = Metadata("Full metadata", "https://example.test/full.jpg");
        var movie = Movie.Rehydrate(
            id, "Library title", new LibraryPath("C:/library/full.mkv"), new MediaFormat("mkv"),
            false, metadata, EnrichmentStatus.Enriched, Now.AddDays(-1), 1, null, Now.AddHours(-1));

        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);

        await using var read = OpenSibling(db);
        var loaded = await Repository(read).GetAsync(id, ct);
        loaded.ShouldNotBeNull();
        loaded.Metadata.ShouldBe(metadata);

        var record = await read.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.MetadataTitle.ShouldBe("Full metadata");
        record.Plot.ShouldBe("Synopsis");
        record.ReleaseYear.ShouldNotBeNull().Value.ShouldBe(2024);
        record.RuntimeMinutes.ShouldNotBeNull().Minutes.ShouldBe(121);
        record.ImdbRating.ShouldNotBeNull().Value.ShouldBe(8.1m);
        record.ImdbId.ShouldNotBeNull().Value.ShouldBe("tt1234567");
        record.PosterUrl.ShouldBe("https://example.test/full.jpg");
    }

    [Fact]
    public async Task SaveChanges_MetadataPosterUpdate_PersistsTheStoredPoster()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Poster update", "poster-update");

        movie.MarkEnriched(Metadata("Version one", "https://example.test/v1.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == movie.Id, ct);
        record.PosterUrl.ShouldBe("https://example.test/v1.jpg");
        record.MetadataTitle.ShouldBe("Version one");
    }

    [Fact]
    public async Task SaveChanges_PosterOnlyReplacement_PersistsTheNewPoster()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Poster replacement", "poster-replacement");
        movie.MarkEnriched(Metadata("Shared title", "https://example.test/old.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        movie.RequestEnrichment();
        movie.MarkEnriched(Metadata("Shared title", "https://example.test/new.jpg"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == movie.Id, ct);
        record.PosterUrl.ShouldBe("https://example.test/new.jpg");
    }

    [Fact]
    public async Task SaveChanges_ReEnrichmentWithoutPoster_ReplacesStoredPosterWithNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Null replacement", "null-replacement");
        movie.MarkEnriched(Metadata("Shared title", "https://example.test/old.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        movie.RequestEnrichment();
        movie.MarkEnriched(Metadata("Shared title", null), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == movie.Id, ct);
        record.PosterUrl.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_UnrelatedUpdate_KeepsTheStoredPoster()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Poster preserved", "poster-preserved");
        movie.MarkEnriched(Metadata("Shared title", "https://example.test/kept.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        var loaded = await repository.GetAsync(movie.Id, ct);
        loaded.ShouldNotBeNull();
        loaded.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == movie.Id, ct);
        record.PosterUrl.ShouldBe("https://example.test/kept.jpg");
        record.IsInWatchlist.ShouldBeTrue();
    }

    [Fact]
    public async Task ClearMetadata_PersistedPoster_ClearsEveryMetadataFieldAndPreservesUnrelatedState()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Clearable", "clearable");
        movie.MarkEnriched(Metadata("Clearable metadata", "https://example.test/clear.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        await using (var seeded = OpenSibling(db))
        {
            var record = await seeded.Movies.AsNoTracking().SingleAsync(item => item.Id == movie.Id, ct);
            record.PosterUrl.ShouldBe("https://example.test/clear.jpg");
            record.MetadataTitle.ShouldBe("Clearable metadata");
        }

        var loaded = await repository.GetAsync(movie.Id, ct);
        loaded.ShouldNotBeNull();
        loaded.ClearMetadata();
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var cleared = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == movie.Id, ct);
        cleared.MetadataTitle.ShouldBeNull();
        cleared.Plot.ShouldBeNull();
        cleared.ReleaseYear.ShouldBeNull();
        cleared.RuntimeMinutes.ShouldBeNull();
        cleared.ImdbRating.ShouldBeNull();
        cleared.ImdbId.ShouldBeNull();
        cleared.PosterUrl.ShouldBeNull();
        cleared.Status.ShouldBe(EnrichmentStatus.Enriched);
        cleared.EnrichedAt.ShouldBe(Now);
        cleared.EnrichmentAttempts.ShouldBe(0);
        cleared.LastAttemptAt.ShouldBe(Now);
        cleared.IsInWatchlist.ShouldBeFalse();
        cleared.Title.ShouldBe("Clearable");
        cleared.LibraryPath.Value.ShouldBe("C:/library/clearable.mkv");
    }

    [Fact]
    public async Task TryClaim_ExpiredLeasePersistsAndSequentialDoubleClaimDoesNotIncrementTwice()
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(TestContext.Current.CancellationToken),
            "Claimable", new LibraryPath("C:/library/claimable.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await repository.TryClaimForEnrichmentAsync(movie.Id, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await repository.TryClaimForEnrichmentAsync(movie.Id, TestContext.Current.CancellationToken)).ShouldBeFalse();

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == movie.Id, TestContext.Current.CancellationToken);
        record.EnrichmentAttempts.ShouldBe(1);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_RejectsMissingTerminalUnexpiredAndBoundaryCases()
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var absentId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var enrichedId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var notFoundId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var failedId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var unexpiredId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var boundaryId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        var expiredId = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        db.Movies.AddRange(
            CreateRecord(enrichedId, EnrichmentStatus.Enriched, null),
            CreateRecord(notFoundId, EnrichmentStatus.NotFound, null),
            CreateRecord(failedId, EnrichmentStatus.Failed, null),
            CreateRecord(unexpiredId, EnrichmentStatus.Pending, Now.AddMinutes(-1)),
            CreateRecord(boundaryId, EnrichmentStatus.Pending, Now.AddMinutes(-5)),
            CreateRecord(expiredId, EnrichmentStatus.Pending, Now.AddMinutes(-5).AddMicroseconds(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await repository.TryClaimForEnrichmentAsync(absentId, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(enrichedId, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(notFoundId, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(failedId, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(unexpiredId, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(boundaryId, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(expiredId, TestContext.Current.CancellationToken)).ShouldBeTrue();

        var expired = await db.Movies.AsNoTracking()
            .SingleAsync(movie => movie.Id == expiredId, TestContext.Current.CancellationToken);
        expired.EnrichmentAttempts.ShouldBe(1);
        expired.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_ClaimStateSurvivesWatchlistAndMarkSaves()
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(TestContext.Current.CancellationToken),
            "Claim seam", new LibraryPath("C:/library/seam.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        var loaded = await repository.GetAsync(movie.Id, TestContext.Current.CancellationToken);
        loaded.ShouldNotBeNull();

        (await repository.TryClaimForEnrichmentAsync(movie.Id, TestContext.Current.CancellationToken)).ShouldBeTrue();
        loaded.AddToWatchlist();
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using (var seam = OpenSibling(db))
        {
            var claimed = await seam.Movies.SingleAsync(item => item.Id == movie.Id, TestContext.Current.CancellationToken);
            claimed.LastAttemptAt.ShouldBe(Now);
            claimed.EnrichmentAttempts.ShouldBe(1);
            claimed.IsInWatchlist.ShouldBeTrue();
        }

        loaded.MarkEnriched(new MovieMetadata("Enriched"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == movie.Id, TestContext.Current.CancellationToken);
        record.EnrichmentAttempts.ShouldBe(1);
        record.LastAttemptAt.ShouldBe(Now.AddMinutes(1));
        record.IsInWatchlist.ShouldBeTrue();
        record.Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task TryClaim_FromPersistedNonZeroCount_IncrementsExactlyOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null);

        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaimThenOutcome_FreshAggregateSuccess_PersistsClaimedCountOnly() =>
        await AssertFreshAggregateOutcomePersistsClaimedCount(Outcome.Success);

    [Fact]
    public async Task TryClaimThenOutcome_FreshAggregateNotFound_PersistsClaimedCountOnly() =>
        await AssertFreshAggregateOutcomePersistsClaimedCount(Outcome.NotFound);

    [Fact]
    public async Task TryClaimThenOutcome_FreshAggregateTerminalFailure_PersistsClaimedCountOnly() =>
        await AssertFreshAggregateOutcomePersistsClaimedCount(Outcome.TerminalFailure);

    [Fact]
    public async Task TryClaimThenOutcome_PreloadedAggregateSuccess_PersistsClaimedCountOnly() =>
        await AssertPreloadedAggregateOutcomePersistsClaimedCount(Outcome.Success);

    [Fact]
    public async Task TryClaimThenOutcome_PreloadedAggregateNotFound_PersistsClaimedCountOnly() =>
        await AssertPreloadedAggregateOutcomePersistsClaimedCount(Outcome.NotFound);

    [Fact]
    public async Task TryClaimThenOutcome_PreloadedAggregateTerminalFailure_PersistsClaimedCountOnly() =>
        await AssertPreloadedAggregateOutcomePersistsClaimedCount(Outcome.TerminalFailure);

    private async Task AssertFreshAggregateOutcomePersistsClaimedCount(Outcome outcome)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null);

        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();

        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.EnrichmentAttempts.ShouldBe(4);
        ApplyOutcome(movie, outcome);
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.Status.ShouldBe(StatusOf(outcome));
        record.LastAttemptAt.ShouldBe(Now.AddMinutes(1));
    }

    private async Task AssertPreloadedAggregateOutcomePersistsClaimedCount(Outcome outcome)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null);

        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.EnrichmentAttempts.ShouldBe(3);

        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();

        ApplyOutcome(movie, outcome);
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.Status.ShouldBe(StatusOf(outcome));
    }

    [Fact]
    public async Task TryClaimThenWatchlistSave_PreservesClaimedCountExactly()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null);

        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();
        movie.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.IsInWatchlist.ShouldBeTrue();
        record.Status.ShouldBe(EnrichmentStatus.Pending);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_RefusedDoubleAndLosingClaims_AddZeroBeyondTheWinningClaim()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var claimableId = await repository.NextIdentityAsync(ct);
        var terminalId = await repository.NextIdentityAsync(ct);
        var absentId = await repository.NextIdentityAsync(ct);
        await SeedAsync(claimableId, EnrichmentStatus.Pending, 3, null);
        await SeedAsync(terminalId, EnrichmentStatus.Enriched, 5, Now.AddDays(-1));

        (await repository.TryClaimForEnrichmentAsync(claimableId, ct)).ShouldBeTrue();
        (await repository.TryClaimForEnrichmentAsync(claimableId, ct)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(terminalId, ct)).ShouldBeFalse();
        (await repository.TryClaimForEnrichmentAsync(absentId, ct)).ShouldBeFalse();

        await using (var secondClaimer = OpenSibling(db))
        {
            var other = Repository(secondClaimer, Now);
            (await other.TryClaimForEnrichmentAsync(claimableId, ct)).ShouldBeFalse();
        }

        await using var verify = OpenSibling(db);
        var winner = await verify.Movies.SingleAsync(item => item.Id == claimableId, ct);
        winner.EnrichmentAttempts.ShouldBe(4);
        var terminal = await verify.Movies.SingleAsync(item => item.Id == terminalId, ct);
        terminal.EnrichmentAttempts.ShouldBe(5);
    }

    [Fact]
    public async Task RetryDecision_WithoutOutcomeLeavesCountNextClaimAddsOneFinalOutcomeAddsNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var clock = new LeaseClock(Now);
        var repository = RepositoryAt(db, clock);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 2, null);

        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();

        await using (var afterFirstClaim = OpenSibling(db))
        {
            var firstClaim = await afterFirstClaim.Movies.SingleAsync(item => item.Id == id, ct);
            firstClaim.EnrichmentAttempts.ShouldBe(3);
        }

        clock.Advance(TimeSpan.FromMinutes(10));
        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();

        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.EnrichmentAttempts.ShouldBe(4);
        movie.MarkNotFound(Now.AddMinutes(20));
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.Status.ShouldBe(EnrichmentStatus.NotFound);
    }

    [Fact]
    public async Task ManualRetry_SavesAndEnqueuesWireOneAtSameCountNextClaimAddsOneFinalOutcomeAddsNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Failed, 3, Now.AddDays(-1));

        var queue = new RecordingQueue();
        var handler = new RequestEnrichmentCommandHandler(repository, queue);
        await handler.HandleAsync(new RequestEnrichmentCommand(id), ct);

        queue.Messages.Count.ShouldBe(1);
        queue.Messages[0].ShouldBe(new EnrichmentRequested(id, 1));

        await using (var afterRequest = OpenSibling(db))
        {
            var requested = await afterRequest.Movies.SingleAsync(item => item.Id == id, ct);
            requested.EnrichmentAttempts.ShouldBe(3);
            requested.Status.ShouldBe(EnrichmentStatus.Pending);
        }

        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();

        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(new MovieMetadata("Recovered"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task RequeueAlone_EnqueuesWireOneWithoutChangingPersistedCount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db, Now);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null);

        var queue = new RecordingQueue();
        var handler = new RequeueStrandedMoviesCommandHandler(queue);
        (await handler.HandleAsync(new RequeueStrandedMoviesCommand([id]), ct)).ShouldBe(1);

        queue.Messages.Count.ShouldBe(1);
        queue.Messages[0].ShouldBe(new EnrichmentRequested(id, 1));

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(3);
        record.LastAttemptAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_FullSnapshot_FreshContextReloadReturnsCompleteSnapshot()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Snapshot", "snapshot");
        var metadata = SnapshotMetadata(
            "Snapshot title", "tt0000001", ["Drama", "Thriller"], ["Dana Director"], ["Wanda Writer"], ["Ada Actor"], 66);

        movie.MarkEnriched(metadata, Now);
        await repository.SaveChangesAsync(ct);

        await using var read = OpenSibling(db);
        var loaded = await Repository(read).GetAsync(movie.Id, ct);
        loaded.ShouldNotBeNull();
        var restored = loaded.Metadata;
        restored.ShouldNotBeNull();
        restored.Synopsis.ShouldBe("Full synopsis");
        restored.MetaScore.ShouldBe((short)66);
        restored.Genres.OrderBy(genre => genre, StringComparer.Ordinal).ShouldBe(["Drama", "Thriller"]);
        restored.Directors.ShouldBe(["Dana Director"]);
        restored.Actors.ShouldBe(["Ada Actor"]);
        restored.Writers.ShouldBe(["Wanda Writer"]);

        (await read.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Wanda Writer"]);
        (await read.Movies.Where(item => item.Id == movie.Id).SelectMany(item => item.Writers).CountAsync(ct))
            .ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_ReEnrichment_ReplacesAssociationsAndKeepsSharedLookups()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Replace", "replace");
        movie.MarkEnriched(
            SnapshotMetadata("First", "tt0000002", ["Drama"], ["Old Director"], ["Old Writer"], ["Old Actor"], 50), Now);
        await repository.SaveChangesAsync(ct);

        movie.RequestEnrichment();
        movie.MarkEnriched(
            SnapshotMetadata("Second", "tt0000002", ["Comedy"], ["New Director"], ["New Writer"], ["New Actor"], 70),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking()
            .Include(item => item.Actors).Include(item => item.Directors)
            .Include(item => item.Genres).Include(item => item.Writers)
            .SingleAsync(item => item.Id == movie.Id, ct);
        record.MetaScore.ShouldBe((short)70);
        record.Writers.Select(writer => writer.Name).ShouldBe(["New Writer"]);
        record.Genres.Select(genre => genre.Name).ShouldBe(["Comedy"]);
        record.Directors.Select(director => director.Name).ShouldBe(["New Director"]);
        record.Actors.Select(actor => actor.Name).ShouldBe(["New Actor"]);

        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Old Writer", "New Writer"], ignoreOrder: true);
        (await verify.Genres.Select(genre => genre.Name).ToArrayAsync(ct)).ShouldBe(["Drama", "Comedy"], ignoreOrder: true);
    }

    [Fact]
    public async Task SaveChanges_RepeatedSnapshotWithSameNames_DoesNotDuplicateRows()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Idempotent", "idempotent");
        movie.MarkEnriched(
            SnapshotMetadata("First", "tt0000003", ["Drama"], ["Shared Director"], ["Shared Writer"], ["Shared Actor"], 60), Now);
        await repository.SaveChangesAsync(ct);

        movie.RequestEnrichment();
        movie.MarkEnriched(
            SnapshotMetadata("Second", "tt0000003", ["Drama"], ["Shared Director"], ["Shared Writer"], ["Shared Actor"], 61),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        (await verify.Genres.CountAsync(ct)).ShouldBe(1);
        (await verify.Directors.CountAsync(ct)).ShouldBe(1);
        (await verify.Writers.CountAsync(ct)).ShouldBe(1);
        (await verify.Actors.CountAsync(ct)).ShouldBe(1);
        (await verify.Movies.SelectMany(item => item.Genres).CountAsync(ct)).ShouldBe(1);
        (await verify.Movies.SelectMany(item => item.Writers).CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_LookupReuse_KeepsStoredCasing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using (var seed = fixture.CreateMigratedContext())
        {
            seed.Writers.Add(new WriterRecord { Name = "WANDA WRITER" });
            seed.Genres.Add(new GenreRecord { Name = "drama" });
            await seed.SaveChangesAsync(ct);
        }

        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Casing", "casing");
        movie.MarkEnriched(
            SnapshotMetadata("Casing", "tt0000004", ["Drama"], [], ["wanda writer"], [], 60), Now);
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["WANDA WRITER"]);
        (await verify.Genres.Select(genre => genre.Name).ToArrayAsync(ct)).ShouldBe(["drama"]);
        var record = await verify.Movies.AsNoTracking().Include(item => item.Writers).Include(item => item.Genres)
            .SingleAsync(item => item.Id == movie.Id, ct);
        record.Writers.Select(writer => writer.Name).ShouldBe(["WANDA WRITER"]);
        record.Genres.Select(genre => genre.Name).ShouldBe(["drama"]);
    }

    [Fact]
    public async Task ClearMetadata_RemovesAssociationsAndKeepsSharedLookups()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var (repository, movie) = await SeedPendingMovieAsync(db, "Clearable snapshot", "clearable-snapshot");
        movie.MarkEnriched(
            SnapshotMetadata("Clear", "tt0000005", ["Drama"], ["Dana Director"], ["Wanda Writer"], ["Ada Actor"], 60), Now);
        await repository.SaveChangesAsync(ct);

        var loaded = await repository.GetAsync(movie.Id, ct);
        loaded.ShouldNotBeNull();
        loaded.ClearMetadata();
        await repository.SaveChangesAsync(ct);

        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking()
            .Include(item => item.Actors).Include(item => item.Directors)
            .Include(item => item.Genres).Include(item => item.Writers)
            .SingleAsync(item => item.Id == movie.Id, ct);
        record.MetadataTitle.ShouldBeNull();
        record.Plot.ShouldBeNull();
        record.MetaScore.ShouldBeNull();
        record.Actors.ShouldBeEmpty();
        record.Directors.ShouldBeEmpty();
        record.Genres.ShouldBeEmpty();
        record.Writers.ShouldBeEmpty();

        (await verify.Writers.CountAsync(ct)).ShouldBe(1);
        (await verify.Genres.CountAsync(ct)).ShouldBe(1);
        (await verify.Directors.CountAsync(ct)).ShouldBe(1);
        (await verify.Actors.CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task ConcurrentEnrichment_TwoMoviesSharingNewNames_CreateSingleLookupPerRole()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var contextA = fixture.CreateMigratedContext();
        await using var contextB = fixture.CreateMigratedContext();
        var repositoryA = Repository(contextA);
        var repositoryB = Repository(contextB);
        var movieA = PendingMovie(await repositoryA.NextIdentityAsync(ct), "Alpha", "concurrent-alpha");
        var movieB = PendingMovie(await repositoryB.NextIdentityAsync(ct), "Bravo", "concurrent-bravo");
        await repositoryA.AddAsync(movieA, ct);
        await repositoryB.AddAsync(movieB, ct);
        movieA.MarkEnriched(
            SnapshotMetadata("Alpha", "tt0000011", ["Drama"], ["Shared Director"], ["Shared Writer"], ["Shared Actor"], 60), Now);
        movieB.MarkEnriched(
            SnapshotMetadata("Bravo", "tt0000012", ["Drama"], ["Shared Director"], ["Shared Writer"], ["Shared Actor"], 61), Now);

        await using var gate = fixture.CreateMigratedContext();
        await using (var gateTransaction = await gate.Database.BeginTransactionAsync(ct))
        {
            await gate.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0}, hashtext({1}))", [1, "drama"], ct);

            var saveA = repositoryA.SaveChangesAsync(ct);
            var saveB = repositoryB.SaveChangesAsync(ct);
            await Task.Delay(300, ct);
            saveA.IsCompleted.ShouldBeFalse();
            saveB.IsCompleted.ShouldBeFalse();

            await gateTransaction.RollbackAsync(ct);
            await Task.WhenAll(saveA, saveB);
        }

        await using var verify = fixture.CreateMigratedContext();
        (await verify.Genres.Select(genre => genre.Name).ToArrayAsync(ct)).ShouldBe(["Drama"]);
        (await verify.Directors.Select(director => director.Name).ToArrayAsync(ct)).ShouldBe(["Shared Director"]);
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Shared Writer"]);
        (await verify.Actors.Select(actor => actor.Name).ToArrayAsync(ct)).ShouldBe(["Shared Actor"]);
        (await verify.Movies.SelectMany(item => item.Writers).CountAsync(ct)).ShouldBe(2);
        (await verify.Movies.SelectMany(item => item.Genres).CountAsync(ct)).ShouldBe(2);
        (await verify.Movies.SelectMany(item => item.Directors).CountAsync(ct)).ShouldBe(2);
        (await verify.Movies.SelectMany(item => item.Actors).CountAsync(ct)).ShouldBe(2);
    }

    [Fact]
    public async Task MetadataSave_RetryableConflict_ReplaysAndSucceedsWithinThreeAttempts()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new RetryableConflictInterceptor(2);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = PendingMovie(await repository.NextIdentityAsync(ct), "Retry", "retry");
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Retry", "tt0000021", ["Retry Genre"], [], ["Retry Writer"], [], 60), Now);

        await repository.SaveChangesAsync(ct);

        interceptor.FailureCount.ShouldBe(2);
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Retry Writer"]);
        (await verify.Movies.SelectMany(item => item.Writers).CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task MetadataSave_RetryableConflictOnEveryAttempt_ExhaustsAndLeavesNoPartialRows()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new RetryableConflictInterceptor(int.MaxValue);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = PendingMovie(await repository.NextIdentityAsync(ct), "Exhaust", "exhaust");
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Exhaust", "tt0000022", ["Exhaust Genre"], [], ["Exhaust Writer"], [], 60), Now);

        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));
        FindPostgres(failure).ShouldNotBeNull().SqlState.ShouldBe("40001");

        interceptor.FailureCount.ShouldBe(3);
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Writers.CountAsync(ct)).ShouldBe(0);
        (await verify.Genres.CountAsync(ct)).ShouldBe(0);
        (await verify.Movies.CountAsync(item => item.Id == movie.Id, ct)).ShouldBe(0);
    }

    [Fact]
    public async Task MetadataSave_AmbientTransaction_RetryableConflict_DoesNotRetryAndRethrows()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new RetryableConflictInterceptor(5);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = PendingMovie(await repository.NextIdentityAsync(ct), "Ambient", "ambient");
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Ambient", "tt0000023", ["Ambient Genre"], [], ["Ambient Writer"], [], 60), Now);

        await using var ambient = await db.Database.BeginTransactionAsync(ct);
        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));
        FindPostgres(failure).ShouldNotBeNull().SqlState.ShouldBe("40001");
        interceptor.FailureCount.ShouldBe(1);
        await ambient.RollbackAsync(ct);
    }

    private static PostgresException? FindPostgres(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private static Movie PendingMovie(MovieId id, string title, string pathSuffix) =>
        Movie.Create(id, title, new LibraryPath($"C:/library/{pathSuffix}.mkv"), new MediaFormat("mkv"));

    private static MovieMetadata SnapshotMetadata(
        string title,
        string imdbId,
        IEnumerable<string> genres,
        IEnumerable<string> directors,
        IEnumerable<string> writers,
        IEnumerable<string> actors,
        short? metaScore) =>
        new(
            title,
            "Full synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId(imdbId),
            "https://example.test/poster.jpg",
            null,
            metaScore,
            directors,
            actors,
            genres,
            writers);

    private static LamuFlixDbContext OpenInstrumentedContext(string connectionString, DbCommandInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(connectionString, o => o.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        return new LamuFlixDbContext(options);
    }

    private sealed class RetryableConflictInterceptor(int allowedFailures) : DbCommandInterceptor
    {
        private int remaining = allowedFailures;

        public int FailureCount { get; private set; }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (ShouldInject(command.CommandText))
            {
                result.Dispose();
                ThrowInjected();
            }

            return new ValueTask<DbDataReader>(result);
        }

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            if (ShouldInject(command.CommandText))
            {
                result.Dispose();
                ThrowInjected();
            }

            return result;
        }

        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            if (ShouldInject(command.CommandText))
            {
                ThrowInjected();
            }

            return result;
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (ShouldInject(command.CommandText))
            {
                ThrowInjected();
            }

            return new ValueTask<int>(result);
        }

        private bool ShouldInject(string commandText)
        {
            var isWriterInsert = commandText.Contains("INSERT INTO writers", StringComparison.Ordinal)
                || commandText.Contains("INSERT INTO \"writers\"", StringComparison.Ordinal);
            if (remaining <= 0 || !isWriterInsert)
            {
                return false;
            }

            remaining--;
            FailureCount++;
            return true;
        }

        private static void ThrowInjected() =>
            throw new PostgresException("synthetic retryable conflict", "ERROR", "ERROR", "40001");
    }

    private async Task SeedAsync(
        MovieId id, EnrichmentStatus status, int enrichmentAttempts, DateTimeOffset? lastAttemptAt)
    {
        await using var seed = fixture.CreateMigratedContext();
        seed.Movies.Add(CreateRecord(id, status, lastAttemptAt, enrichmentAttempts));
        await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static void ApplyOutcome(Movie movie, Outcome outcome)
    {
        var at = Now.AddMinutes(1);
        switch (outcome)
        {
            case Outcome.Success:
                movie.MarkEnriched(new MovieMetadata("Seam outcome"), at);
                return;
            case Outcome.NotFound:
                movie.MarkNotFound(at);
                return;
            case Outcome.TerminalFailure:
                movie.MarkFailed(EnrichmentFailureCategory.InvalidResponse, at);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome.");
        }
    }

    private static EnrichmentStatus StatusOf(Outcome outcome) =>
        outcome switch
        {
            Outcome.Success => EnrichmentStatus.Enriched,
            Outcome.NotFound => EnrichmentStatus.NotFound,
            Outcome.TerminalFailure => EnrichmentStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome."),
        };

    private enum Outcome
    {
        Success,
        NotFound,
        TerminalFailure,
    }

    private static LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        var connectionString = source.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db, DateTimeOffset? now = null) =>
        new(db, new FixedTimeProvider(now ?? Now), Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

    private static EfMovieRepository RepositoryAt(LamuFlixDbContext db, LeaseClock clock) =>
        new(db, clock, Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

    private static async Task<(EfMovieRepository Repository, Movie Movie)> SeedPendingMovieAsync(
        LamuFlixDbContext db, string title, string pathSuffix)
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var movie = Movie.Create(id, title, new LibraryPath($"C:/library/{pathSuffix}.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        return (repository, movie);
    }

    private static MovieMetadata Metadata(string title, string? posterUrl) =>
        new(
            title,
            "Synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId("tt1234567"),
            posterUrl);

    private static MovieRecord CreateRecord(
        MovieId id, EnrichmentStatus status, DateTimeOffset? lastAttemptAt, int enrichmentAttempts = 0) =>
        new()
        {
            Id = id,
            Title = $"Movie {id.Value}",
            LibraryPath = new LibraryPath($"C:/library/movie-{id.Value}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = status,
            EnrichmentAttempts = enrichmentAttempts,
            LastAttemptAt = lastAttemptAt,
        };

    private sealed class LeaseClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset utcNow = start;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }

    private sealed class RecordingQueue : IEnrichmentQueue
    {
        public List<EnrichmentRequested> Messages { get; } = [];

        public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

}