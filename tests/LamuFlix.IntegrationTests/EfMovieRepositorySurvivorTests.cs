using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RepositoryMutationCollection))]
public sealed class EfMovieRepositorySurvivorTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_PositiveLease_CreatesRepository()
    {
        // arrange
        using var db = fixture.CreateMigratedContext();

        // act
        var repository = Repository(db);

        // assert
        repository.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NonPositiveLease_ThrowsNamingOptionsWithLeaseMessage()
    {
        // arrange
        using var db = fixture.CreateMigratedContext();
        var options = Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.Zero });

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(
            () => new EfMovieRepository(db, TimeProvider.System, options));

        // assert
        failure.ParamName.ShouldBe("options");
        failure.Message.ShouldContain("Claim lease must be positive.");
    }

    [Fact]
    public async Task SaveChangesAsync_WarmedCache_InvalidatesSoNextReadRebuilds()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var cache = new GenreWeightsCache();
        var warm = GenreWeights.Calculate(new GenreCounts(1, new Dictionary<string, long> { ["7"] = 1 }));
        await cache.GetOrLoadAsync(_ => Task.FromResult(warm), ct);
        var repository = Repository(db, cache);
        var fresh = GenreWeights.Calculate(new GenreCounts(2, new Dictionary<string, long> { ["7"] = 1 }));
        var reloaded = false;

        // act
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(Movie.Create(id, "Cached", new LibraryPath("C:/library/cached.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);
        var after = await cache.GetOrLoadAsync(
            _ =>
            {
                reloaded = true;
                return Task.FromResult(fresh);
            },
            ct);

        // assert
        after.ShouldBeSameAs(fresh);
        reloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_DefaultCache_FallsBackAndPersists()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = new EfMovieRepository(
            db,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

        // act
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(Movie.Create(id, "Fallback", new LibraryPath("C:/library/fallback.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);

        // assert
        (await repository.GetAsync(id, ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task TryDeleteAsync_ExistingMovie_InvalidatesWarmCache()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var cache = new GenreWeightsCache();
        var repository = Repository(db, cache);
        var id = await AddMovieAsync(repository, "Doomed", "unit-cache-doomed", ct);
        var warm = GenreWeights.Calculate(new GenreCounts(1, new Dictionary<string, long> { ["7"] = 1 }));
        await cache.GetOrLoadAsync(_ => Task.FromResult(warm), ct);
        var fresh = GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));
        var reloaded = false;

        // act
        var deleted = await repository.TryDeleteAsync(id, ct);
        var after = await cache.GetOrLoadAsync(
            _ =>
            {
                reloaded = true;
                return Task.FromResult(fresh);
            },
            ct);

        // assert
        deleted.ShouldBeTrue();
        after.ShouldBeSameAs(fresh);
        reloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task TryDeleteAsync_AbsentMovie_RetainsWarmCache()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var cache = new GenreWeightsCache();
        var repository = Repository(db, cache);
        var warm = GenreWeights.Calculate(new GenreCounts(1, new Dictionary<string, long> { ["7"] = 1 }));
        await cache.GetOrLoadAsync(_ => Task.FromResult(warm), ct);
        var absent = await repository.NextIdentityAsync(ct);

        // act
        var deleted = await repository.TryDeleteAsync(absent, ct);
        var after = await cache.GetOrLoadAsync(
            _ => Task.FromResult(GenreWeights.Calculate(new GenreCounts(9, new Dictionary<string, long>()))),
            ct);

        // assert
        deleted.ShouldBeFalse();
        after.ShouldBeSameAs(warm);
    }

    [Fact]
    public async Task AddAndGet_RoundTripsAggregateAndReturnsTrackedInstance()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var metadata = FullMetadata("https://example.test/full.jpg");
        var movie = Movie.Rehydrate(
            id, "Library title", new LibraryPath("C:/library/movie.mkv"), new MediaFormat("mkv"),
            true, metadata, EnrichmentStatus.Failed, Now.AddDays(-1), 2,
            EnrichmentFailureCategory.RateLimited, Now.AddHours(-1));

        // act
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);

        // assert
        (await repository.GetAsync(id, ct)).ShouldBeSameAs(movie);
        await using var read = OpenSibling(db);
        var loaded = await Repository(read).GetAsync(id, ct);
        loaded.ShouldNotBeNull();
        loaded.Title.ShouldBe("Library title");
        loaded.Path.ShouldBe(new LibraryPath("C:/library/movie.mkv"));
        loaded.Format.ShouldBe(new MediaFormat("mkv"));
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
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);

        // act
        var loaded = await repository.GetAsync(id, ct);

        // assert
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateId_ThrowsNamingTheMovieId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var movie = Movie.Create(id, "Movie", new LibraryPath("C:/library/movie.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => repository.AddAsync(movie, ct));

        // assert
        failure.Message.ShouldContain(id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AddAsync_CancelledToken_ThrowsWithoutTracking()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var movie = Movie.Create(id, "Movie", new LibraryPath("C:/library/cancelled.mkv"), new MediaFormat("mkv"));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => repository.AddAsync(movie, cancelled.Token));

        // assert
        (await repository.GetAsync(id, ct)).ShouldBeNull();
    }

    [Fact]
    public async Task TryDelete_PresentId_RemovesRowAndSiblingConfirms()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var alpha = await AddMovieAsync(repository, "Alpha", "delete-alpha", ct);
        var bravo = await AddMovieAsync(repository, "Bravo", "delete-bravo", ct);

        // act
        var removed = await repository.TryDeleteAsync(alpha, ct);

        // assert
        removed.ShouldBeTrue();
        await using var verify = OpenSibling(db);
        (await verify.Movies.CountAsync(movie => movie.Id == alpha, ct)).ShouldBe(0);
        (await verify.Movies.CountAsync(movie => movie.Id == bravo, ct)).ShouldBe(1);
    }

    [Fact]
    public async Task TryDelete_AbsentId_ReturnsFalse()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);

        // act
        var removed = await repository.TryDeleteAsync(id, ct);

        // assert
        removed.ShouldBeFalse();
    }

    [Fact]
    public async Task TryDelete_RepeatDelete_FirstRemovesSecondReportsAbsent()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Repeat", "delete-repeat", ct);

        // act
        var first = await repository.TryDeleteAsync(id, ct);
        var second = await repository.TryDeleteAsync(id, ct);

        // assert
        first.ShouldBeTrue();
        second.ShouldBeFalse();
    }

    [Fact]
    public async Task TryDelete_TrackedMovie_ForgetsTheTrackedInstanceAndDetaches()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Tracked", "delete-tracked", ct);
        (await repository.GetAsync(id, ct)).ShouldNotBeNull();

        // act
        (await repository.TryDeleteAsync(id, ct)).ShouldBeTrue();

        // assert
        (await repository.GetAsync(id, ct)).ShouldBeNull();
        db.ChangeTracker.Entries<MovieRecord>().ShouldBeEmpty();
        await repository.SaveChangesAsync(ct);
        await using var verify = OpenSibling(db);
        (await verify.Movies.CountAsync(movie => movie.Id == id, ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Add_WithFullMetadata_PersistsEveryMetadataColumn()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var movie = Movie.Rehydrate(
            id, "Library title", new LibraryPath("C:/library/full.mkv"), new MediaFormat("mkv"),
            false, FullMetadata("https://example.test/full.jpg"), EnrichmentStatus.Enriched,
            Now.AddDays(-1), 1, null, Now.AddHours(-1));
        await repository.AddAsync(movie, ct);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.MetadataTitle.ShouldBe("Full metadata");
        record.Plot.ShouldBe("Synopsis");
        record.ReleaseYear.ShouldNotBeNull().Value.ShouldBe(2024);
        record.RuntimeMinutes.ShouldNotBeNull().Minutes.ShouldBe(121);
        record.ImdbRating.ShouldNotBeNull().Value.ShouldBe(8.1m);
        record.ImdbId.ShouldNotBeNull().Value.ShouldBe("tt1234567");
        record.PosterUrl.ShouldBe("https://example.test/full.jpg");
    }

    [Fact]
    public async Task Add_WithGenreWriterAndScoreCollections_RoundTripsThroughSiblingReload()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Collections", "collections", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            new MovieMetadata(
                "Full metadata",
                "Synopsis",
                new ReleaseYear(2024, Now),
                new Runtime(121),
                new ImdbRating(8.1m),
                new ImdbId("tt1234567"),
                "https://example.test/full.jpg",
                null,
                82,
                ["Ridley Scott"],
                ["Harrison Ford"],
                ["Drama", "Sci-Fi"],
                ["Wanda Writer"]),
            Now);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        await using var read = OpenSibling(db);
        var loaded = await Repository(read).GetAsync(id, ct);
        loaded.ShouldNotBeNull();
        loaded.Metadata.ShouldNotBeNull();
        loaded.Metadata.MetaScore.ShouldBe((short)82);
        loaded.Metadata.Genres.ShouldBe(["Drama", "Sci-Fi"]);
        loaded.Metadata.Writers.ShouldBe(["Wanda Writer"]);
        loaded.Metadata.Directors.ShouldBe(["Ridley Scott"]);
        loaded.Metadata.Actors.ShouldBe(["Harrison Ford"]);
    }

    [Fact]
    public async Task Add_WithoutMetadata_StoresNullMetadataColumns()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(id, "Plain", new LibraryPath("C:/library/plain.mkv"), new MediaFormat("mkv")), ct);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.MetadataTitle.ShouldBeNull();
        record.Plot.ShouldBeNull();
        record.ReleaseYear.ShouldBeNull();
        record.RuntimeMinutes.ShouldBeNull();
        record.ImdbRating.ShouldBeNull();
        record.ImdbId.ShouldBeNull();
        record.PosterUrl.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_ClearMetadata_ClearsEveryStoredMetadataColumn()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        var movie = Movie.Rehydrate(
            id, "Clearable", new LibraryPath("C:/library/clearable.mkv"), new MediaFormat("mkv"),
            false, FullMetadata("https://example.test/clear.jpg"), EnrichmentStatus.Pending,
            null, 0, null, null);
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        var loaded = await repository.GetAsync(id, ct);
        loaded.ShouldNotBeNull();

        // act
        loaded.ClearMetadata();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var cleared = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        cleared.MetadataTitle.ShouldBeNull();
        cleared.Plot.ShouldBeNull();
        cleared.ReleaseYear.ShouldBeNull();
        cleared.RuntimeMinutes.ShouldBeNull();
        cleared.ImdbRating.ShouldBeNull();
        cleared.ImdbId.ShouldBeNull();
        cleared.PosterUrl.ShouldBeNull();
        cleared.Title.ShouldBe("Clearable");
    }

    [Fact]
    public async Task SaveChanges_WatchlistAddition_PersistsTrueWhileLeavingMetadataAlone()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Watchlist", "watchlist-add", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.IsInWatchlist.ShouldBeTrue();
        record.Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task SaveChanges_UnrelatedSave_PreservesStoredWatchlistAndStatus()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Untouched", "untouched", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.IsInWatchlist.ShouldBeFalse();
        record.Status.ShouldBe(EnrichmentStatus.Pending);
        record.EnrichedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_MarkEnriched_PersistsStatusEnrichedAtAndMetadata()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Enrich", "enrich", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(FullMetadata("https://example.test/v1.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.Status.ShouldBe(EnrichmentStatus.Enriched);
        record.EnrichedAt.ShouldBe(Now);
        record.PosterUrl.ShouldBe("https://example.test/v1.jpg");
        record.MetadataTitle.ShouldBe("Full metadata");
    }

    [Fact]
    public async Task SaveChanges_MetadataReplacement_PersistsTheNewPoster()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Poster", "poster", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(FullMetadata("https://example.test/old.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(FullMetadata("https://example.test/new.jpg"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.PosterUrl.ShouldBe("https://example.test/new.jpg");
    }

    [Fact]
    public async Task SaveChanges_MarkFailed_PersistsFailureCategory()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Failed", "failed", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkFailed(EnrichmentFailureCategory.InvalidResponse, Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.Status.ShouldBe(EnrichmentStatus.Failed);
        record.EnrichmentFailureCategory.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task SaveChanges_MarkNotFound_PersistsStatusAndAttemptTime()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Missing", "missing", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkNotFound(Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.Status.ShouldBe(EnrichmentStatus.NotFound);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_NeverClaimedPending_ReturnsTrueAndIncrementsExactlyOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Claimable", new LibraryPath("C:/library/claimable.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);

        // act
        var first = await repository.TryClaimForEnrichmentAsync(movie.Id, ct);
        var second = await repository.TryClaimForEnrichmentAsync(movie.Id, ct);

        // assert
        first.ShouldBeTrue();
        second.ShouldBeFalse();
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == movie.Id, ct);
        record.EnrichmentAttempts.ShouldBe(1);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_AbsentTerminalUnexpiredAndBoundaryIds_ReturnsFalseExceptExpired()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var absentId = await repository.NextIdentityAsync(ct);
        var enrichedId = await repository.NextIdentityAsync(ct);
        var notFoundId = await repository.NextIdentityAsync(ct);
        var failedId = await repository.NextIdentityAsync(ct);
        var unexpiredId = await repository.NextIdentityAsync(ct);
        var boundaryId = await repository.NextIdentityAsync(ct);
        var expiredId = await repository.NextIdentityAsync(ct);
        db.Movies.AddRange(
            Record(enrichedId, EnrichmentStatus.Enriched, null),
            Record(notFoundId, EnrichmentStatus.NotFound, null),
            Record(failedId, EnrichmentStatus.Failed, null),
            Record(unexpiredId, EnrichmentStatus.Pending, Now.AddMinutes(-1)),
            Record(boundaryId, EnrichmentStatus.Pending, Now.AddMinutes(-5)),
            Record(expiredId, EnrichmentStatus.Pending, Now.AddMinutes(-5).AddMicroseconds(-1)));
        await db.SaveChangesAsync(ct);

        // act
        var absent = await repository.TryClaimForEnrichmentAsync(absentId, ct);
        var enriched = await repository.TryClaimForEnrichmentAsync(enrichedId, ct);
        var notFound = await repository.TryClaimForEnrichmentAsync(notFoundId, ct);
        var failed = await repository.TryClaimForEnrichmentAsync(failedId, ct);
        var unexpired = await repository.TryClaimForEnrichmentAsync(unexpiredId, ct);
        var boundary = await repository.TryClaimForEnrichmentAsync(boundaryId, ct);
        var expired = await repository.TryClaimForEnrichmentAsync(expiredId, ct);

        // assert
        absent.ShouldBeFalse();
        enriched.ShouldBeFalse();
        notFound.ShouldBeFalse();
        failed.ShouldBeFalse();
        unexpired.ShouldBeFalse();
        boundary.ShouldBeFalse();
        expired.ShouldBeTrue();
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == expiredId, ct);
        record.EnrichmentAttempts.ShouldBe(1);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_FromPersistedNonZeroCount_IncrementsExactlyOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null, ct);

        // act
        var claimed = await repository.TryClaimForEnrichmentAsync(id, ct);

        // assert
        claimed.ShouldBeTrue();
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaim_PreloadThenClaimAndEnrich_PersistsClaimedCountWithOutcome()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(ct);
        await SeedAsync(id, EnrichmentStatus.Pending, 3, null, ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.EnrichmentAttempts.ShouldBe(3);

        // act
        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();
        var reread = await repository.GetAsync(id, ct);
        reread.ShouldNotBeNull();
        reread.MarkEnriched(new MovieMetadata("Recovered"), Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(4);
        record.Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task TryClaim_ClaimThenWatchlistSave_PreservesClaimedCount()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Seam", new LibraryPath("C:/library/seam.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        var loaded = await repository.GetAsync(movie.Id, ct);
        loaded.ShouldNotBeNull();

        // act
        (await repository.TryClaimForEnrichmentAsync(movie.Id, ct)).ShouldBeTrue();
        loaded.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == movie.Id, ct);
        record.LastAttemptAt.ShouldBe(Now);
        record.EnrichmentAttempts.ShouldBe(1);
        record.IsInWatchlist.ShouldBeTrue();
    }

    [Fact]
    public async Task TryClaim_SecondClaimerAfterFirstClaim_IsRefused()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Raced", "raced", ct);

        // act
        (await repository.TryClaimForEnrichmentAsync(id, ct)).ShouldBeTrue();
        await using var secondContext = OpenSibling(db);
        var refused = await Repository(secondContext).TryClaimForEnrichmentAsync(id, ct);

        // assert
        refused.ShouldBeFalse();
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.SingleAsync(item => item.Id == id, ct);
        record.EnrichmentAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task FindStranded_ReturnsOnlyPendingExpiredIds()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var expiredId = await repository.NextIdentityAsync(ct);
        var freshId = await repository.NextIdentityAsync(ct);
        var nullAttemptId = await repository.NextIdentityAsync(ct);
        var enrichedId = await repository.NextIdentityAsync(ct);
        await using (var seed = fixture.CreateMigratedContext())
        {
            seed.Movies.AddRange(
                Record(expiredId, EnrichmentStatus.Pending, Now.AddHours(-1)),
                Record(freshId, EnrichmentStatus.Pending, Now),
                Record(nullAttemptId, EnrichmentStatus.Pending, null),
                Record(enrichedId, EnrichmentStatus.Enriched, Now.AddHours(-1)));
            await seed.SaveChangesAsync(ct);
        }

        // act
        var stranded = await repository.FindStrandedMovieIdsAsync(Now.AddMinutes(-5), ct);

        // assert
        stranded.Select(item => item.Value).ShouldBe(
            [expiredId.Value, nullAttemptId.Value], ignoreOrder: true);
    }

    [Fact]
    public async Task SaveChanges_ClearMetadata_ClearsCollectionsAndKeepsLookups()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Clear Collections", "clear-collections", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            SnapshotMetadata("Clear", "tt0000005", ["Drama"], ["Dana Director"], ["Wanda Writer"], ["Ada Actor"], 60),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        var loaded = await repository.GetAsync(id, ct);
        loaded.ShouldNotBeNull();
        loaded.ClearMetadata();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking()
            .Include(item => item.Actors).Include(item => item.Directors)
            .Include(item => item.Genres).Include(item => item.Writers)
            .SingleAsync(item => item.Id == id, ct);
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
    public async Task SaveChanges_RepeatedSnapshot_DoesNotDuplicateRows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Idempotent", "mutation-idempotent", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            SnapshotMetadata("First", "tt0000003", ["Drama"], ["Shared Director"], ["Shared Writer"], ["Shared Actor"], 60),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(
            SnapshotMetadata("Second", "tt0000003", ["Drama"], ["Shared Director"], ["Shared Writer"], ["Shared Actor"], 61),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
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
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using (var seed = fixture.CreateMigratedContext())
        {
            seed.Writers.Add(new WriterRecord { Name = "WANDA WRITER" });
            seed.Genres.Add(new GenreRecord { Name = "drama" });
            await seed.SaveChangesAsync(ct);
        }

        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Casing", "mutation-casing", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(
            SnapshotMetadata("Casing", "tt0000004", ["Drama"], [], ["wanda writer"], [], 60),
            Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["WANDA WRITER"]);
        (await verify.Genres.Select(genre => genre.Name).ToArrayAsync(ct)).ShouldBe(["drama"]);
        var record = await verify.Movies.AsNoTracking().Include(item => item.Writers).Include(item => item.Genres)
            .SingleAsync(item => item.Id == id, ct);
        record.Writers.Select(writer => writer.Name).ShouldBe(["WANDA WRITER"]);
        record.Genres.Select(genre => genre.Name).ShouldBe(["drama"]);
    }

    [Fact]
    public async Task SaveChanges_ReEnrichment_ReplacesAssociationsAndKeepsSharedLookups()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Replace", "mutation-replace", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            SnapshotMetadata("First", "tt0000002", ["Drama"], ["Old Director"], ["Old Writer"], ["Old Actor"], 50),
            Now);
        await repository.SaveChangesAsync(ct);

        // act
        movie.RequestEnrichment();
        movie.MarkEnriched(
            SnapshotMetadata("Second", "tt0000002", ["Comedy"], ["New Director"], ["New Writer"], ["New Actor"], 70),
            Now.AddMinutes(1));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking()
            .Include(item => item.Actors).Include(item => item.Directors)
            .Include(item => item.Genres).Include(item => item.Writers)
            .SingleAsync(item => item.Id == id, ct);
        record.MetaScore.ShouldBe((short)70);
        record.Writers.Select(writer => writer.Name).ShouldBe(["New Writer"]);
        record.Genres.Select(genre => genre.Name).ShouldBe(["Comedy"]);
        record.Directors.Select(director => director.Name).ShouldBe(["New Director"]);
        record.Actors.Select(actor => actor.Name).ShouldBe(["New Actor"]);
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Old Writer", "New Writer"], ignoreOrder: true);
        (await verify.Genres.Select(genre => genre.Name).ToArrayAsync(ct)).ShouldBe(["Drama", "Comedy"], ignoreOrder: true);
    }

    [Fact]
    public async Task FindStranded_BoundaryExactlyAtCutoff_Excludes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var cutoff = Now.AddMinutes(-5);
        var boundaryId = await repository.NextIdentityAsync(ct);
        var expiredId = await repository.NextIdentityAsync(ct);
        db.Movies.AddRange(
            Record(boundaryId, EnrichmentStatus.Pending, cutoff),
            Record(expiredId, EnrichmentStatus.Pending, cutoff.AddTicks(-1)));
        await db.SaveChangesAsync(ct);

        // act
        var stranded = await repository.FindStrandedMovieIdsAsync(cutoff, ct);

        // assert
        stranded.Select(item => item.Value).ShouldBe([expiredId.Value]);
    }

    [Fact]
    public async Task SaveChanges_DoubleSaveWithNoChanges_SecondIsNoOp()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Double", "mutation-double", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(FullMetadata("https://example.test/double.jpg"), Now);
        await repository.SaveChangesAsync(ct);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.PosterUrl.ShouldBe("https://example.test/double.jpg");
        record.Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task SaveChanges_WhitespaceAndDuplicateNames_DedupesAndSkipsBlanks()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Dedup", "mutation-dedup", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(
            new MovieMetadata(
                "Dedup metadata",
                "Synopsis",
                new ReleaseYear(2024, Now),
                new Runtime(121),
                new ImdbRating(8.1m),
                new ImdbId("tt1234567"),
                "https://example.test/dedup.jpg",
                null,
                82,
                ["  Dana Director  ", "dana director", "   ", ""],
                ["Ada Actor", "ADA ACTOR"],
                ["Drama", " drama ", "N/A"],
                ["Wanda Writer", "wanda writer"]),
            Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var read = OpenSibling(db);
        var loaded = await Repository(read).GetAsync(id, ct);
        loaded.ShouldNotBeNull();
        loaded.Metadata.ShouldNotBeNull();
        loaded.Metadata.Directors.ShouldBe(["Dana Director"]);
        loaded.Metadata.Actors.ShouldBe(["Ada Actor"]);
    }

    [Fact]
    public async Task MetadataSave_RetryableConflict_ReplaysAndSucceeds()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("40001", 2);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Retry", new LibraryPath("C:/library/mutation-retry.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Retry", "tt0000021", ["Retry Genre"], [], ["Retry Writer"], [], 60),
            Now);

        // act
        var elapsed = Stopwatch.StartNew();
        await repository.SaveChangesAsync(ct);
        elapsed.Stop();

        // assert
        interceptor.FailureCount.ShouldBe(2);
        elapsed.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(40));
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Retry Writer"]);
        (await verify.Movies.SelectMany(item => item.Writers).CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task MetadataSave_DeadlockConflict_ReplaysAndSucceeds()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("40P01", 1);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Deadlock", new LibraryPath("C:/library/mutation-deadlock.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Deadlock", "tt0000024", ["Deadlock Genre"], [], ["Deadlock Writer"], [], 60),
            Now);

        // act
        var elapsed = Stopwatch.StartNew();
        await repository.SaveChangesAsync(ct);
        elapsed.Stop();

        // assert
        interceptor.FailureCount.ShouldBe(1);
        elapsed.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(10));
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Deadlock Writer"]);
    }

    [Fact]
    public async Task MetadataSave_RetryableConflictOnEveryAttempt_ExhaustsAndLeavesNoPartialRows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("40001", int.MaxValue);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Exhaust", new LibraryPath("C:/library/mutation-exhaust.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Exhaust", "tt0000022", ["Exhaust Genre"], [], ["Exhaust Writer"], [], 60),
            Now);

        // act
        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));

        // assert
        FindPostgres(failure).ShouldNotBeNull().SqlState.ShouldBe("40001");
        interceptor.FailureCount.ShouldBe(3);
        db.ChangeTracker.Entries().ShouldBeEmpty();
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Writers.CountAsync(ct)).ShouldBe(0);
        (await verify.Genres.CountAsync(ct)).ShouldBe(0);
        (await verify.Movies.CountAsync(item => item.Id == movie.Id, ct)).ShouldBe(0);
    }

    [Fact]
    public async Task MetadataSave_NonRetryableUniqueViolation_DoesNotRetryAndRethrows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("23505", 5);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Unique", new LibraryPath("C:/library/mutation-unique.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Unique", "tt0000025", ["Unique Genre"], [], ["Unique Writer"], [], 60),
            Now);

        // act
        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));

        // assert
        FindPostgres(failure).ShouldNotBeNull().SqlState.ShouldBe("23505");
        interceptor.FailureCount.ShouldBe(1);
    }

    [Fact]
    public async Task MetadataSave_NonPostgresFailure_DoesNotRetryAndRethrows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new NonPostgresInterceptor();
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "NonPostgres", new LibraryPath("C:/library/mutation-nonpostgres.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("NonPostgres", "tt0000026", ["NonPostgres Genre"], [], ["NonPostgres Writer"], [], 60),
            Now);

        // act
        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));

        // assert
        FindPostgres(failure).ShouldBeNull();
        failure.ShouldBeOfType<DbUpdateException>();
        interceptor.FailureCount.ShouldBe(1);
    }

    [Fact]
    public async Task MetadataSave_AmbientTransaction_DoesNotRetryAndRethrows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("40001", 5);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Ambient", new LibraryPath("C:/library/mutation-ambient.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Ambient", "tt0000023", ["Ambient Genre"], [], ["Ambient Writer"], [], 60),
            Now);

        // act
        await using var ambient = await db.Database.BeginTransactionAsync(ct);
        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));

        // assert
        FindPostgres(failure).ShouldNotBeNull().SqlState.ShouldBe("40001");
        interceptor.FailureCount.ShouldBe(1);
        db.ChangeTracker.Entries().ShouldBeEmpty();
        await ambient.RollbackAsync(ct);
    }

    [Fact]
    public async Task ConcurrentEnrichment_TwoMoviesSharingNewNames_CreateSingleLookupPerRole()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var tag = Guid.NewGuid().ToString("N");
        var genre = $"Drama {tag}";
        var director = $"Shared Director {tag}";
        var writer = $"Shared Writer {tag}";
        var actor = $"Shared Actor {tag}";
        await using var contextA = fixture.CreateMigratedContext();
        await using var contextB = fixture.CreateMigratedContext();
        var repositoryA = Repository(contextA);
        var repositoryB = Repository(contextB);
        var movieA = Movie.Create(await repositoryA.NextIdentityAsync(ct), "Alpha", new LibraryPath($"C:/library/mutation-concurrent-alpha-{tag}.mkv"), new MediaFormat("mkv"));
        var movieB = Movie.Create(await repositoryB.NextIdentityAsync(ct), "Bravo", new LibraryPath($"C:/library/mutation-concurrent-bravo-{tag}.mkv"), new MediaFormat("mkv"));
        await repositoryA.AddAsync(movieA, ct);
        await repositoryB.AddAsync(movieB, ct);
        movieA.MarkEnriched(
            SnapshotMetadata("Alpha", "tt0000011", [genre], [director], [writer], [actor], 60), Now);
        movieB.MarkEnriched(
            SnapshotMetadata("Bravo", "tt0000012", [genre], [director], [writer], [actor], 61), Now);

        await using var gate = fixture.CreateMigratedContext();
        await using (var gateTransaction = await gate.Database.BeginTransactionAsync(ct))
        {
            await gate.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0}, hashtext({1}))", [1, genre.ToLowerInvariant()], ct);

            var saveA = repositoryA.SaveChangesAsync(ct);
            var saveB = repositoryB.SaveChangesAsync(ct);
            await Task.Delay(300, ct);
            saveA.IsCompleted.ShouldBeFalse();
            saveB.IsCompleted.ShouldBeFalse();

            await gateTransaction.RollbackAsync(ct);
            await Task.WhenAll(saveA, saveB);
        }

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Genres.Select(g => g.Name).ToArrayAsync(ct)).ShouldBe([genre]);
        (await verify.Directors.Select(d => d.Name).ToArrayAsync(ct)).ShouldBe([director]);
        (await verify.Writers.Select(w => w.Name).ToArrayAsync(ct)).ShouldBe([writer]);
        (await verify.Actors.Select(a => a.Name).ToArrayAsync(ct)).ShouldBe([actor]);
        (await verify.Movies.SelectMany(item => item.Writers).CountAsync(ct)).ShouldBe(2);
        (await verify.Movies.SelectMany(item => item.Genres).CountAsync(ct)).ShouldBe(2);
        (await verify.Movies.SelectMany(item => item.Directors).CountAsync(ct)).ShouldBe(2);
        (await verify.Movies.SelectMany(item => item.Actors).CountAsync(ct)).ShouldBe(2);
    }

    [Fact]
    public async Task MetadataSave_AmbientTransaction_CommitsWithoutOwnedRetry()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Ambient Success", "ambient-success", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            SnapshotMetadata("Ambient", "tt0000031", ["Ambient Genre"], ["Ambient Director"], ["Ambient Writer"], ["Ambient Actor"], 60),
            Now);

        // act
        await using var ambient = await db.Database.BeginTransactionAsync(ct);
        await repository.SaveChangesAsync(ct);
        await ambient.CommitAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking()
            .Include(item => item.Writers)
            .SingleAsync(item => item.Id == id, ct);
        record.MetadataTitle.ShouldBe("Ambient");
        record.Writers.Select(writer => writer.Name).ShouldBe(["Ambient Writer"]);
    }

    [Fact]
    public async Task SaveChanges_WatchlistToggleTwice_PersistsFinalState()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Toggle", "toggle", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.AddToWatchlist();
        await repository.SaveChangesAsync(ct);

        // act
        movie.RemoveFromWatchlist();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.IsInWatchlist.ShouldBeFalse();
    }

    [Fact]
    public async Task MetadataSave_MixedBatchWithConflict_RetriesMetadataWrite()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("40001", 1);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var plainId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(plainId, "Plain", new LibraryPath("C:/library/mutation-mixed-plain.mkv"), new MediaFormat("mkv")), ct);
        var enrichedId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(enrichedId, "Enriched", new LibraryPath("C:/library/mutation-mixed-enriched.mkv"), new MediaFormat("mkv")), ct);
        var movie = await repository.GetAsync(enrichedId, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            SnapshotMetadata("Mixed", "tt0000032", ["Mixed Genre"], [], ["Mixed Writer"], [], 60),
            Now);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        interceptor.FailureCount.ShouldBe(1);
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.CountAsync(ct)).ShouldBe(2);
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Mixed Writer"]);
    }

    [Fact]
    public async Task ConcurrentEnrichment_DirectorLock_BlocksBothSavesUntilRelease()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;

        // act
        await AssertRoleGateBlocksBothSavesAsync(2, "shared director", ct);
    }

    [Fact]
    public async Task ConcurrentEnrichment_WriterLock_BlocksBothSavesUntilRelease()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;

        // act
        await AssertRoleGateBlocksBothSavesAsync(3, "shared writer", ct);
    }

    [Fact]
    public async Task ConcurrentEnrichment_ActorLock_BlocksBothSavesUntilRelease()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;

        // act
        await AssertRoleGateBlocksBothSavesAsync(4, "shared actor", ct);
    }

    private async Task AssertRoleGateBlocksBothSavesAsync(int roleId, string key, CancellationToken ct)
    {
        // arrange
        await fixture.ResetAsync(ct);
        var tag = Guid.NewGuid().ToString("N");
        var genre = $"Drama {tag}";
        var director = $"Shared Director {tag}";
        var writer = $"Shared Writer {tag}";
        var actor = $"Shared Actor {tag}";
        await using var contextA = fixture.CreateMigratedContext();
        await using var contextB = fixture.CreateMigratedContext();
        var repositoryA = Repository(contextA);
        var repositoryB = Repository(contextB);
        var movieA = Movie.Create(await repositoryA.NextIdentityAsync(ct), "Alpha", new LibraryPath($"C:/library/mutation-gate-alpha-{tag}.mkv"), new MediaFormat("mkv"));
        var movieB = Movie.Create(await repositoryB.NextIdentityAsync(ct), "Bravo", new LibraryPath($"C:/library/mutation-gate-bravo-{tag}.mkv"), new MediaFormat("mkv"));
        await repositoryA.AddAsync(movieA, ct);
        await repositoryB.AddAsync(movieB, ct);
        movieA.MarkEnriched(
            SnapshotMetadata("Alpha", "tt0000041", [genre], [director], [writer], [actor], 60), Now);
        movieB.MarkEnriched(
            SnapshotMetadata("Bravo", "tt0000042", [genre], [director], [writer], [actor], 61), Now);

        await using var gate = fixture.CreateMigratedContext();
        await using (var gateTransaction = await gate.Database.BeginTransactionAsync(ct))
        {
            await gate.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0}, hashtext({1}))", [roleId, $"{key} {tag}"], ct);

            var saveA = repositoryA.SaveChangesAsync(ct);
            var saveB = repositoryB.SaveChangesAsync(ct);
            await Task.Delay(300, ct);
            saveA.IsCompleted.ShouldBeFalse();
            saveB.IsCompleted.ShouldBeFalse();

            await gateTransaction.RollbackAsync(ct);
            await Task.WhenAll(saveA, saveB);
        }

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Genres.Select(g => g.Name).ToArrayAsync(ct)).ShouldBe([genre]);
        (await verify.Directors.Select(d => d.Name).ToArrayAsync(ct)).ShouldBe([director]);
        (await verify.Writers.Select(w => w.Name).ToArrayAsync(ct)).ShouldBe([writer]);
        (await verify.Actors.Select(a => a.Name).ToArrayAsync(ct)).ShouldBe([actor]);
    }

    [Fact]
    public async Task MetadataSave_UnknownSqlState_DoesNotRetryAndRethrows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new RetryableConflictInterceptor("XX000", 5);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Unknown", new LibraryPath("C:/library/mutation-unknown.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Unknown", "tt0000027", ["Unknown Genre"], [], ["Unknown Writer"], [], 60),
            Now);

        // act
        var failure = await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));

        // assert
        FindPostgres(failure).ShouldNotBeNull().SqlState.ShouldBe("XX000");
        interceptor.FailureCount.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_DuplicateLibraryPath_RejectsSecondSave()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var first = await AddMovieAsync(repository, "First", "mutation-duplicate-path", ct);
        first.ShouldNotBeNull();
        var secondId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(secondId, "Second", new LibraryPath("C:/library/mutation-duplicate-path.mkv"), new MediaFormat("mkv")), ct);

        // act
        await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task SaveChanges_DuplicateImdbId_RejectsSecondSave()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var firstId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Rehydrate(
                firstId, "First", new LibraryPath("C:/library/mutation-imdb-a.mkv"), new MediaFormat("mkv"),
                false, FullMetadata("https://example.test/a.jpg"), EnrichmentStatus.Enriched, Now, 0, null, null),
            ct);
        await repository.SaveChangesAsync(ct);
        var secondId = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Rehydrate(
                secondId, "Second", new LibraryPath("C:/library/mutation-imdb-b.mkv"), new MediaFormat("mkv"),
                false, FullMetadata("https://example.test/b.jpg"), EnrichmentStatus.Enriched, Now, 0, null, null),
            ct);

        // act
        await Should.ThrowAsync<Exception>(() => repository.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task SaveChanges_NullImdbIds_AllowsBothSaves()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var firstId = await AddMovieAsync(repository, "Null A", "mutation-null-imdb-a", ct);
        var secondId = await AddMovieAsync(repository, "Null B", "mutation-null-imdb-b", ct);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        (await verify.Movies.CountAsync(item => item.Id == firstId || item.Id == secondId, ct)).ShouldBe(2);
    }

    [Fact]
    public async Task SaveChanges_DuplicateImdbIdOnMetadataWrite_ThrowsDuplicateAndClearsTracking()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var holder = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Holder", new LibraryPath("C:/library/mutation-dup-holder.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(holder, ct);
        holder.MarkEnriched(new MovieMetadata("Holder", imdbId: new ImdbId("tt9900100")), Now);
        await repository.SaveChangesAsync(ct);
        var loser = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Loser", new LibraryPath("C:/library/mutation-dup-loser.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(loser, ct);
        await repository.SaveChangesAsync(ct);
        loser.MarkEnriched(new MovieMetadata("Loser", imdbId: new ImdbId("tt9900100")), Now);

        // act
        var failure = await Should.ThrowAsync<DuplicateImdbIdException>(() => repository.SaveChangesAsync(ct));

        // assert
        failure.InnerException.ShouldNotBeNull();
        db.ChangeTracker.Entries().ShouldBeEmpty();
        (await repository.GetAsync(loser.Id, ct)).ShouldNotBeSameAs(loser);
        await using var verify = OpenSibling(db);
        (await verify.Movies.CountAsync(movie => movie.Id == loser.Id, ct)).ShouldBe(1);
        (await verify.Movies.AsNoTracking().SingleAsync(movie => movie.Id == loser.Id, ct)).ImdbId.ShouldBeNull();
    }

    [Fact]
    public async Task MetadataSave_AmbientTransaction_DuplicateImdbId_ThrowsDuplicateAndClearsTracking()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var holder = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Holder", new LibraryPath("C:/library/mutation-ambient-dup-holder.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(holder, ct);
        holder.MarkEnriched(new MovieMetadata("Holder", imdbId: new ImdbId("tt9900200")), Now);
        await repository.SaveChangesAsync(ct);
        var loser = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Loser", new LibraryPath("C:/library/mutation-ambient-dup-loser.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(loser, ct);
        await repository.SaveChangesAsync(ct);
        loser.MarkEnriched(new MovieMetadata("Loser", imdbId: new ImdbId("tt9900200")), Now);

        // act
        await using var ambient = await db.Database.BeginTransactionAsync(ct);
        var failure = await Should.ThrowAsync<DuplicateImdbIdException>(() => repository.SaveChangesAsync(ct));

        // assert
        failure.InnerException.ShouldNotBeNull();
        db.ChangeTracker.Entries().ShouldBeEmpty();
        (await repository.GetAsync(loser.Id, ct)).ShouldNotBeSameAs(loser);
        await ambient.RollbackAsync(ct);
    }

    [Fact]
    public async Task ConcurrentEnrichment_DirectorOnlyLock_BlocksBothSavesUntilRelease()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var tag = Guid.NewGuid().ToString("N");
        var director = $"Only Director {tag}";
        await using var contextA = fixture.CreateMigratedContext();
        await using var contextB = fixture.CreateMigratedContext();
        var repositoryA = Repository(contextA);
        var repositoryB = Repository(contextB);
        var movieA = Movie.Create(await repositoryA.NextIdentityAsync(ct), "Alpha", new LibraryPath($"C:/library/mutation-director-alpha-{tag}.mkv"), new MediaFormat("mkv"));
        var movieB = Movie.Create(await repositoryB.NextIdentityAsync(ct), "Bravo", new LibraryPath($"C:/library/mutation-director-bravo-{tag}.mkv"), new MediaFormat("mkv"));
        await repositoryA.AddAsync(movieA, ct);
        await repositoryB.AddAsync(movieB, ct);
        movieA.MarkEnriched(
            SnapshotMetadata("Alpha", "tt0000051", [], [director], [], [], 60), Now);
        movieB.MarkEnriched(
            SnapshotMetadata("Bravo", "tt0000052", [], [director], [], [], 61), Now);

        await using var gate = fixture.CreateMigratedContext();
        await using (var gateTransaction = await gate.Database.BeginTransactionAsync(ct))
        {
            await gate.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0}, hashtext({1}))", [2, director.ToLowerInvariant()], ct);

            var saveA = repositoryA.SaveChangesAsync(ct);
            var saveB = repositoryB.SaveChangesAsync(ct);
            await Task.Delay(300, ct);
            saveA.IsCompleted.ShouldBeFalse();
            saveB.IsCompleted.ShouldBeFalse();

            await gateTransaction.RollbackAsync(ct);
            await Task.WhenAll(saveA, saveB);
        }

        // assert
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Directors.Select(d => d.Name).ToArrayAsync(ct)).ShouldBe([director]);
        (await verify.Genres.CountAsync(ct)).ShouldBe(0);
        (await verify.Writers.CountAsync(ct)).ShouldBe(0);
        (await verify.Actors.CountAsync(ct)).ShouldBe(0);
        (await verify.Movies.SelectMany(item => item.Directors).CountAsync(ct)).ShouldBe(2);
    }

    [Fact]
    public async Task AcquireSnapshotLocks_TakesLocksInRoleThenNameOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var interceptor = new LockOrderInterceptor();
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Ordered", new LibraryPath("C:/library/mutation-lock-order.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Ordered", "tt0000053", ["Zulu", "Alpha"], ["Delta"], ["Mike"], ["Beta"], 60),
            Now);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        interceptor.Locks.ShouldBe(
        [
            (1, "alpha"), (1, "zulu"), (2, "delta"), (3, "mike"), (4, "beta")
        ]);
    }

    [Fact]
    public async Task SaveChanges_EmptyRoleCollections_SucceedsWithoutLookups()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Empty Roles", "mutation-empty-roles", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkEnriched(
            SnapshotMetadata("Empty Roles", "tt0000054", [], [], [], [], 60),
            Now);
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.MetadataTitle.ShouldBe("Empty Roles");
        (await verify.Genres.CountAsync(ct)).ShouldBe(0);
        (await verify.Directors.CountAsync(ct)).ShouldBe(0);
        (await verify.Writers.CountAsync(ct)).ShouldBe(0);
        (await verify.Actors.CountAsync(ct)).ShouldBe(0);
    }

    [Fact]
    public async Task FindMovieIdByImdbId_HeldAbsentAndEmpty_ReturnsHolderNullAndNull()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var heldId = await repository.NextIdentityAsync(ct);
        var otherId = await repository.NextIdentityAsync(ct);
        db.Movies.AddRange(
            new MovieRecord
            {
                Id = heldId,
                Title = "Held",
                LibraryPath = new LibraryPath("C:/library/mutation-held.mkv"),
                Format = new MediaFormat("mkv"),
                Status = EnrichmentStatus.Pending,
                ImdbId = new ImdbId("tt0000055"),
            },
            Record(otherId, EnrichmentStatus.Pending, null));
        await db.SaveChangesAsync(ct);

        // act
        var held = await repository.FindMovieIdByImdbIdAsync(new ImdbId("tt0000055"), ct);
        var absent = await repository.FindMovieIdByImdbIdAsync(new ImdbId("tt0000056"), ct);

        // assert
        held.ShouldBe(heldId);
        absent.ShouldBeNull();

        // act
        await fixture.ResetAsync(ct);
        var empty = await repository.FindMovieIdByImdbIdAsync(new ImdbId("tt0000055"), ct);

        // assert
        empty.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_WatchedToggle_PersistsEachState()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Watched", "mutation-watched", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkWatched();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        (await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct)).IsWatched.ShouldBeTrue();

        // act
        movie.UnmarkWatched();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var reread = OpenSibling(db);
        (await reread.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct)).IsWatched.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_NotInterestedToggle_PersistsEachState()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "NotInterested", "mutation-not-interested", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MarkNotInterested();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        (await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct)).IsNotInterested.ShouldBeTrue();

        // act
        movie.UnmarkNotInterested();
        await repository.SaveChangesAsync(ct);

        // assert
        await using var reread = OpenSibling(db);
        (await reread.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct)).IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_MatchManually_PersistsManualImdbId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, "Manual", "mutation-manual", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();

        // act
        movie.MatchManually(new ImdbId("tt0000057"));
        await repository.SaveChangesAsync(ct);

        // assert
        await using var verify = OpenSibling(db);
        var record = await verify.Movies.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        record.ManualImdbId.ShouldBe(new ImdbId("tt0000057"));
        record.Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task MetadataSave_RetryableConflict_RecordsBackoffDelays()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetAsync(ct);
        var clock = new RecordingTimeProvider(Now);
        var interceptor = new RetryableConflictInterceptor("40001", 1);
        await using var db = OpenInstrumentedContext(fixture.ConnectionString, interceptor);
        var repository = Repository(db, clock);
        var movie = Movie.Create(
            await repository.NextIdentityAsync(ct),
            "Backoff", new LibraryPath("C:/library/mutation-backoff.mkv"), new MediaFormat("mkv"));
        await repository.AddAsync(movie, ct);
        movie.MarkEnriched(
            SnapshotMetadata("Backoff", "tt0000058", ["Backoff Genre"], [], ["Backoff Writer"], [], 60),
            Now);

        // act
        await repository.SaveChangesAsync(ct);

        // assert
        interceptor.FailureCount.ShouldBe(1);
        clock.Delays.ShouldBe([TimeSpan.FromMilliseconds(25)]);
        await using var verify = fixture.CreateMigratedContext();
        (await verify.Writers.Select(writer => writer.Name).ToArrayAsync(ct)).ShouldBe(["Backoff Writer"]);
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

    private sealed class LockOrderInterceptor : DbCommandInterceptor
    {
        public List<(int RoleId, string Name)> Locks { get; } = [];

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return new ValueTask<int>(result);
        }

        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            Record(command);
            return result;
        }

        private void Record(DbCommand command)
        {
            if (!command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal)
                || command.Parameters.Count < 2
                || command.Parameters[0].Value is not int roleId
                || command.Parameters[1].Value is not string name)
            {
                return;
            }

            Locks.Add((roleId, name));
        }
    }

    private sealed class RecordingTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public List<TimeSpan> Delays { get; } = [];

        public override DateTimeOffset GetUtcNow() => now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime != Timeout.InfiniteTimeSpan)
            {
                Delays.Add(dueTime);
            }

            callback(state);
            return new NoopTimer();
        }

        private sealed class NoopTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class RetryableConflictInterceptor(string sqlState, int allowedFailures) : DbCommandInterceptor
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

        private void ThrowInjected() =>
            throw new PostgresException("synthetic retryable conflict", "ERROR", "ERROR", sqlState);
    }

    private sealed class NonPostgresInterceptor : DbCommandInterceptor
    {
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
                FailureCount++;
                throw new InvalidOperationException("synthetic non-postgres failure");
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
                FailureCount++;
                throw new InvalidOperationException("synthetic non-postgres failure");
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
                FailureCount++;
                throw new InvalidOperationException("synthetic non-postgres failure");
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
                FailureCount++;
                throw new InvalidOperationException("synthetic non-postgres failure");
            }

            return new ValueTask<int>(result);
        }

        private static bool ShouldInject(string commandText) =>
            commandText.Contains("INSERT INTO writers", StringComparison.Ordinal)
                || commandText.Contains("INSERT INTO \"writers\"", StringComparison.Ordinal);
    }

    private async Task SeedAsync(
        MovieId id, EnrichmentStatus status, int attempts, DateTimeOffset? lastAttemptAt, CancellationToken ct)
    {
        await using var seed = fixture.CreateMigratedContext();
        seed.Movies.Add(Record(id, status, lastAttemptAt, attempts));
        await seed.SaveChangesAsync(ct);
    }

    private static async Task<MovieId> AddMovieAsync(
        EfMovieRepository repository, string title, string suffix, CancellationToken ct)
    {
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{suffix}.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);
        return id;
    }

    private LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var connectionString = fixture.ConnectionString;
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(db, new FixedTimeProvider(Now), Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

    private static EfMovieRepository Repository(LamuFlixDbContext db, TimeProvider clock) =>
        new(db, clock, Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

    private static EfMovieRepository Repository(LamuFlixDbContext db, GenreWeightsCache cache) =>
        new(db, new FixedTimeProvider(Now), Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }), cache);

    private static MovieRecord Record(
        MovieId id, EnrichmentStatus status, DateTimeOffset? lastAttemptAt, int attempts = 0) =>
        new()
        {
            Id = id,
            Title = $"Movie {id.Value}",
            LibraryPath = new LibraryPath($"C:/library/movie-{id.Value}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = status,
            EnrichmentAttempts = attempts,
            LastAttemptAt = lastAttemptAt,
        };

    private static MovieMetadata FullMetadata(string posterUrl) =>
        new(
            "Full metadata",
            "Synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId("tt1234567"),
            posterUrl);
}

[CollectionDefinition(nameof(RepositoryMutationCollection))]
public sealed class RepositoryMutationCollection : ICollectionFixture<PostgresFixture>;
