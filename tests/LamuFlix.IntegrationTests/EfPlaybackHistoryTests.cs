using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfPlaybackHistoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset PlayedAtUtc =
        new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RecordAsync_AppendsRow_ReadableFromFreshContext()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync(fixture, "C:/library/fresh.mkv", ct);
        await using var write = fixture.CreateMigratedContext();
        var history = new EfPlaybackHistory(write);

        await history.RecordAsync(new PlaybackEvent(movieId, PlayedAtUtc), ct);

        await using var read = OpenSibling(write);
        var recorded = await read.PlaybackEvents.SingleAsync(ct);
        recorded.MovieId.ShouldBe(movieId);
        recorded.PlayedAt.ShouldBe(PlayedAtUtc);
        recorded.PlayedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task RecordAsync_TwoPlaysAtSameInstant_HaveDistinctIdentities()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync(fixture, "C:/library/same-instant.mkv", ct);
        await using var db = fixture.CreateMigratedContext();
        var history = new EfPlaybackHistory(db);

        await history.RecordAsync(new PlaybackEvent(movieId, PlayedAtUtc), ct);
        await history.RecordAsync(new PlaybackEvent(movieId, PlayedAtUtc), ct);

        var identities = await db.PlaybackEvents
            .OrderBy(playbackEvent => playbackEvent.Id)
            .Select(playbackEvent => playbackEvent.Id)
            .ToListAsync(ct);
        identities.Count.ShouldBe(2);
        identities.ShouldAllBe(id => id > 0);
        identities[0].ShouldNotBe(identities[1]);
    }

    [Fact]
    public async Task RecordAsync_UnknownMovie_IsRejectedByForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var history = new EfPlaybackHistory(db);

        await Should.ThrowAsync<DbUpdateException>(
            () => history.RecordAsync(new PlaybackEvent(new MovieId(9999), PlayedAtUtc), ct));

        (await db.PlaybackEvents.CountAsync(ct)).ShouldBe(0);
    }

    [Fact]
    public async Task RecordAsync_UtcInstant_RoundTripsWithZeroOffset()
    {
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync(fixture, "C:/library/utc.mkv", ct);
        await using var write = fixture.CreateMigratedContext();
        var history = new EfPlaybackHistory(write);

        await history.RecordAsync(new PlaybackEvent(movieId, PlayedAtUtc), ct);

        await using var read = OpenSibling(write);
        var recorded = await read.PlaybackEvents.SingleAsync(ct);
        recorded.PlayedAt.ShouldBe(PlayedAtUtc);
        recorded.PlayedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task DeleteMovie_CascadesItsPlaybackEventsAndPreservesSiblingMovieEvents()
    {
        var ct = TestContext.Current.CancellationToken;
        var deletedMovieId = await SeedMovieAsync(fixture, "C:/library/deleted.mkv", ct);
        var siblingMovieId = await SeedMovieAsync(fixture, "C:/library/sibling.mkv", ct);
        await using (var seed = fixture.CreateMigratedContext())
        {
            var history = new EfPlaybackHistory(seed);
            await history.RecordAsync(new PlaybackEvent(deletedMovieId, PlayedAtUtc), ct);
            await history.RecordAsync(new PlaybackEvent(deletedMovieId, PlayedAtUtc.AddSeconds(1)), ct);
            await history.RecordAsync(new PlaybackEvent(siblingMovieId, PlayedAtUtc.AddSeconds(2)), ct);
            await history.RecordAsync(new PlaybackEvent(siblingMovieId, PlayedAtUtc.AddSeconds(3)), ct);
            await history.RecordAsync(new PlaybackEvent(siblingMovieId, PlayedAtUtc.AddSeconds(4)), ct);
        }

        await using (var delete = fixture.CreateMigratedContext())
        {
            var movie = await delete.Movies.SingleAsync(record => record.Id == deletedMovieId, ct);
            delete.Movies.Remove(movie);
            await delete.SaveChangesAsync(ct);
        }

        await using var verify = fixture.CreateMigratedContext();
        (await verify.Movies.CountAsync(movie => movie.Id == deletedMovieId, ct)).ShouldBe(0);
        (await verify.Movies.CountAsync(movie => movie.Id == siblingMovieId, ct)).ShouldBe(1);
        (await verify.PlaybackEvents.CountAsync(playbackEvent => playbackEvent.MovieId == deletedMovieId, ct))
            .ShouldBe(0);
        (await verify.PlaybackEvents.CountAsync(playbackEvent => playbackEvent.MovieId == siblingMovieId, ct))
            .ShouldBe(3);
        (await verify.PlaybackEvents.CountAsync(ct)).ShouldBe(3);
    }

    private static async Task<MovieId> SeedMovieAsync(
        PostgresFixture fixture,
        string libraryPath,
        CancellationToken cancellationToken)
    {
        await using var context = fixture.CreateMigratedContext();
        var movie = new MovieRecord
        {
            Title = "Playback Event Owner",
            LibraryPath = new LibraryPath(libraryPath),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Pending
        };
        context.Movies.Add(movie);
        await context.SaveChangesAsync(cancellationToken);
        var movieId = movie.Id;
        movieId.ShouldNotBeNull();
        return movieId;
    }

    private static LamuFlixDbContext OpenSibling(LamuFlixDbContext source)
    {
        var connectionString = source.Database.GetConnectionString();
        connectionString.ShouldNotBeNullOrWhiteSpace();
        return LamuFlixDbContextFactory.OpenContext(connectionString);
    }
}
