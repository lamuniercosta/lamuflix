using System;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfPlaybackHistoryGuardTests
{
    [Fact]
    public async Task RecordAsync_NullEvent_ThrowsArgumentNullExceptionNamingPlaybackEvent()
    {
        // arrange
        await using var db = new LamuFlixDbContext(UnusedOptions());
        var history = new EfPlaybackHistory(db);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
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
