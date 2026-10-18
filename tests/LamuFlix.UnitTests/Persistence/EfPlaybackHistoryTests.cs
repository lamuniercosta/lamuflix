using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfPlaybackHistoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset PlayedAt = new(2026, 10, 18, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordAsync_ValidEvent_PersistsRowWithEventValues()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movieId = await SeedMovieAsync($"C:/library/playback-{Guid.NewGuid():N}.mkv", ct);
        await using var write = fixture.CreateMigratedContext();
        var history = new EfPlaybackHistory(write);

        // act
        await history.RecordAsync(new PlaybackEvent(movieId, PlayedAt), ct);

        // assert
        await using var read = fixture.CreateMigratedContext();
        var recorded = await read.PlaybackEvents.SingleAsync(record => record.MovieId == movieId, ct);
        recorded.MovieId.ShouldBe(movieId);
        recorded.PlayedAt.ShouldBe(PlayedAt);
    }

    private async Task<MovieId> SeedMovieAsync(string libraryPath, CancellationToken cancellationToken)
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
}

public sealed class EfPlaybackHistoryGuardTests
{
    [Fact]
    public async Task RecordAsync_NullEvent_ThrowsArgumentNullExceptionNamingPlaybackEvent()
    {
        // arrange
        await using var db = new LamuFlixDbContext(UnusedOptions());
        var history = new EfPlaybackHistory(db);

        // act
        var failure = await Should.ThrowAsync<ArgumentNullException>(
            () => history.RecordAsync(null!, TestContext.Current.CancellationToken));

        // assert
        failure.ParamName.ShouldBe("playbackEvent");
    }

    private static DbContextOptions<LamuFlixDbContext> UnusedOptions() =>
        new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql("Host=localhost;Database=lamuflix-unused")
            .Options;
}
