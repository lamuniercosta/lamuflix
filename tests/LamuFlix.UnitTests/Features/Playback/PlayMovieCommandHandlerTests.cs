using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Playback;

public sealed class PlayMovieCommandHandlerTests
{
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaPlayerLauncher launcher = Substitute.For<IMediaPlayerLauncher>();
    private readonly PlayMovieCommandHandler handler;
    private readonly Fixture fixture = new();

    public PlayMovieCommandHandlerTests()
    {
        handler = new PlayMovieCommandHandler(catalog, launcher);
    }

    [Fact]
    public async Task HandleAsync_DetailsFound_LaunchesOnceWithPathAndFormat()
    {
        // arrange
        var id = NewId();
        var path = new LibraryPath("C:/library/play");
        var format = new MediaFormat("mp4");
        var details = new MovieDetails(id, "Play Me", path, format, null);
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(details);

        // act
        var result = await handler.HandleAsync(new PlayMovieCommand(id), ct);

        // assert
        result.ShouldBe(Unit.Value);
        launcher.Received(1).Launch(path, format);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFoundWithoutLaunch()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns((MovieDetails?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new PlayMovieCommand(id), ct));

        // assert
        launcher.DidNotReceive().Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>());
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
