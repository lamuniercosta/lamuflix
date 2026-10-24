using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class PlaybackTasteProfileInvalidationTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RecordAsync_SuccessfulPlayback_NextDerivedProfileReadRecomputes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        var played = await harness.SeedMovieAsync("Unplayed horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var beforePlayback = await harness.ReadProfileAsync(ct);

        // act
        await harness.RecordPlaybackAsync(played, ct);
        var afterPlayback = await harness.ReadProfileAsync(ct);

        // assert
        beforePlayback.GenrePreferences.Keys.ShouldBe(["Drama"]);
        afterPlayback.GenrePreferences.Keys.ShouldContain("Drama");
        afterPlayback.GenrePreferences.Keys.ShouldContain("Horror");
    }

    [Fact]
    public async Task RecordAsync_FailedPlayback_DoesNotInvalidateTheCachedProfile()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var harness = TasteProfileInvalidationHarness.Create(fixture);
        await harness.SeedMovieAsync("Watched drama", isWatched: true, isNotInterested: false, ["Drama"], ct);
        await harness.SeedMovieAsync("Unplayed horror", isWatched: false, isNotInterested: false, ["Horror"], ct);

        var beforePlayback = await harness.ReadProfileAsync(ct);
        beforePlayback.GenrePreferences.Keys.ShouldBe(["Drama"]);

        // act
        await Should.ThrowAsync<DbUpdateException>(
            () => harness.RecordPlaybackAsync(new MovieId(2_000_000_000), ct));
        var afterPlayback = await harness.ReadProfileAsync(ct);

        // assert
        afterPlayback.GenrePreferences.Keys.ShouldBe(["Drama"]);
    }
}
