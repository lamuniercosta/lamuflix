using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class GetSubtitleSidecarQueryHandlerTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();
    private readonly ISubtitleSidecarStore store = Substitute.For<ISubtitleSidecarStore>();

    [Fact]
    public async Task HandleAsync_NullQuery_ThrowsArgumentNullException()
    {
        // arrange
        var handler = CreateHandler();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => handler.HandleAsync(null!, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task HandleAsync_ExistingSidecar_ReturnsExactBytesAndTheServerResolvedName()
    {
        // arrange
        byte[] payload = [0x31, 0x0A, 0x00, 0xFF];
        Arrange();
        store.ReadAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(payload));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleSidecarQuery(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        result.Content.ShouldBe(payload);
        result.FileName.ShouldBe("Heat.1995.1080p.srt");
    }

    [Fact]
    public async Task HandleAsync_UsesTheConfiguredServingBound()
    {
        // arrange
        Arrange();
        store.ReadAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<byte>()));
        var handler = CreateHandler(servingBound: 1234);

        // act
        await handler.HandleAsync(new GetSubtitleSidecarQuery(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await store.Received(1).ReadAsync(
            Arg.Any<LibraryPath>(), PrimaryFileName, 1234, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingSidecar_PropagatesNotFound()
    {
        // arrange
        Arrange();
        store.ReadAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotFoundException());
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleSidecarQuery(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFoundWithoutReadingTheStore()
    {
        // arrange
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleSidecarQuery(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        await store.DidNotReceiveWithAnyArgs().ReadAsync(
            Arg.Any<LibraryPath>(), Arg.Any<string>(), default, TestContext.Current.CancellationToken);
    }

    private void Arrange()
    {
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(new MovieDetails(
            new MovieId(MovieIdValue),
            "Heat",
            new LibraryPath(MovieFolder),
            new MediaFormat("mkv"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)), new MediaFormat("mkv")));
    }

    private GetSubtitleSidecarQueryHandler CreateHandler(int servingBound = 8388608) =>
        new(
            catalog,
            scanner,
            reader,
            store,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions
            {
                RootPath = "/library",
                MaxSubtitleInputBytes = servingBound,
            }),
            NullLogger<GetSubtitleSidecarQueryHandler>.Instance);
}
