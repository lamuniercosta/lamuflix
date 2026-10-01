using System;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAndGet_RoundTripAggregateAndReturnIdentityMappedInstance()
    {
        await using var db = await MigratedContextAsync();
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
        await using var db = await MigratedContextAsync();
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        (await repository.GetAsync(id, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        await using var db = await MigratedContextAsync();
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
        await using var db = await MigratedContextAsync();
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
        movie.MarkEnriched(new MovieMetadata("Metadata", "Plot", runtime: new Runtime(98)), Now);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        movie.AddToWatchlist();
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verify = OpenSibling(db);
        var loaded = await verify.Movies.Include(item => item.Actors).Include(item => item.Directors)
            .Include(item => item.Genres).SingleAsync(item => item.Id == id, TestContext.Current.CancellationToken);
        loaded.Status.ShouldBe(EnrichmentStatus.Enriched);
        loaded.IsInWatchlist.ShouldBeTrue();
        loaded.RottenTomatoesRating.ShouldBe((short)91);
        loaded.MetaScore.ShouldBe((short)82);
        loaded.PosterUrl.ShouldBe("https://example.test/poster.jpg");
        loaded.Plot.ShouldBe("Plot");
        loaded.Actors.ShouldContain(actor => actor.Name == "Ada Actor");
        loaded.Directors.ShouldContain(director => director.Name == "Dana Director");
        loaded.Genres.ShouldContain(genre => genre.Name == "Drama");
    }

    [Fact]
    public async Task TryClaim_ExpiredLeasePersistsAndSequentialDoubleClaimDoesNotIncrementTwice()
    {
        await using var db = await MigratedContextAsync();
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
        await using var db = await MigratedContextAsync();
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
        await using var db = await MigratedContextAsync();
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
        record.EnrichmentAttempts.ShouldBe(2);
        record.LastAttemptAt.ShouldBe(Now.AddMinutes(1));
        record.IsInWatchlist.ShouldBeTrue();
        record.Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    private async Task<LamuFlixDbContext> MigratedContextAsync()
    {
        var context = fixture.CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        return context;
    }

    private static LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        var connectionString = source.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db, DateTimeOffset? now = null) =>
        new(db, new FixedTimeProvider(now ?? Now), Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }));

    private static MovieRecord CreateRecord(MovieId id, EnrichmentStatus status, DateTimeOffset? lastAttemptAt) =>
        new()
        {
            Id = id,
            Title = $"Movie {id.Value}",
            LibraryPath = new LibraryPath($"C:/library/movie-{id.Value}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = status,
            LastAttemptAt = lastAttemptAt,
        };

}