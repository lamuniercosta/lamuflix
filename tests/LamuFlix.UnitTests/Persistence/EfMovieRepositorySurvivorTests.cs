using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using LamuFlix.UnitTests.Features;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Persistence;

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

    private static LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        var connectionString = source.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(db, new FixedTimeProvider(Now), Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

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
