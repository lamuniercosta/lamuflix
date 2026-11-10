using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class ListSubtitleTracksQueryHandlerTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();
    private readonly ListSubtitleTracksQueryHandler handler;

    public ListSubtitleTracksQueryHandlerTests()
    {
        handler = new ListSubtitleTracksQueryHandler(catalog, scanner, reader);
    }

    [Fact]
    public async Task HandleAsync_EligibleSidecars_ReauthorizesFolderThenEnumeratesItAndReturnsTokenizedTracks()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        var ct = CancellationToken.None;
        DetailsReturning(id, MovieFolder);
        scanner.ResolvePrimaryVideo(new LibraryPath(MovieFolder))
            .Returns(new PrimaryVideo(new LibraryPath($"{MovieFolder}/{PrimaryFileName}"), new MediaFormat("mkv")));
        reader.EnumerateFiles(new LibraryPath(MovieFolder))
            .Returns([PrimaryFileName, $"{Stem}.en.srt", $"{Stem}.default.srt"]);

        // act
        var result = await handler.HandleAsync(new ListSubtitleTracksQuery(id), ct);

        // assert
        result.Tracks.Select(track => track.Label).ShouldBe(["Default", "English"]);
        result.Tracks.Select(track => track.Language).ShouldBe(["und", "en"]);
        result.Tracks.Select(track => track.IsDefault).ShouldBe([true, false]);
        result.Tracks.Select(track => track.Id).ShouldBe(
        [
            SubtitleTrackToken.For(id, $"{Stem}.default.srt"),
            SubtitleTrackToken.For(id, $"{Stem}.en.srt"),
        ]);
        Received.InOrder(() =>
        {
            reader.AuthorizeFolder(new LibraryPath(MovieFolder));
            reader.EnumerateFiles(new LibraryPath(MovieFolder));
        });
    }

    [Fact]
    public async Task HandleAsync_NoEligibleSidecars_ReturnsEmptyTrackList()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id, MovieFolder);
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Returns(new PrimaryVideo(new LibraryPath($"{MovieFolder}/{PrimaryFileName}"), new MediaFormat("mkv")));
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, "Another Movie.srt"]);

        // act
        var result = await handler.HandleAsync(new ListSubtitleTracksQuery(id), CancellationToken.None);

        // assert
        result.Tracks.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFoundWithoutTouchingTheLibrary()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        catalog.GetDetailsAsync(id, Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);

        // act
        var act = () => handler.HandleAsync(new ListSubtitleTracksQuery(id), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        reader.DidNotReceiveWithAnyArgs().AuthorizeFolder(Arg.Any<LibraryPath>());
        reader.DidNotReceiveWithAnyArgs().EnumerateFiles(Arg.Any<LibraryPath>());
    }

    [Fact]
    public async Task HandleAsync_MissingPrimaryVideo_ThrowsNotFound()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id, MovieFolder);
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>())
            .Throws(new InvalidTransitionException("no primary video"));

        // act
        var act = () => handler.HandleAsync(new ListSubtitleTracksQuery(id), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task HandleAsync_DeniedFolder_PropagatesAccessDeniedBeforeEnumeration()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id, MovieFolder);
        reader.When(instance => instance.AuthorizeFolder(Arg.Any<LibraryPath>()))
            .Throw(new LibraryFileAccessDeniedException());

        // act
        var act = () => handler.HandleAsync(new ListSubtitleTracksQuery(id), CancellationToken.None);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        reader.DidNotReceiveWithAnyArgs().EnumerateFiles(Arg.Any<LibraryPath>());
    }

    private static string Stem => "Heat.1995.1080p";

    private void DetailsReturning(MovieId id, string storedPath) =>
        catalog.GetDetailsAsync(id, Arg.Any<CancellationToken>()).Returns(new MovieDetails(
            id,
            "Heat",
            new LibraryPath(storedPath),
            new MediaFormat("mkv"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched));
}
