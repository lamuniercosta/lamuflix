using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleMovieResolutionTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();

    [Fact]
    public async Task ResolveAsync_KnownMovie_ReturnsFolderAndBareFileName()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning($"{MovieFolder}/{PrimaryFileName}");

        // act
        var resolved = await SubtitleMovieResolution.ResolveAsync(
            catalog, scanner, reader, id, CancellationToken.None);

        // assert
        resolved.Folder.ShouldBe(new LibraryPath(MovieFolder));
        resolved.PrimaryFileName.ShouldBe(PrimaryFileName);
    }

    [Fact]
    public async Task ResolveAsync_NestedPrimaryPath_StripsDirectoriesFromFileName()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning($"{MovieFolder}/video/{PrimaryFileName}");

        // act
        var resolved = await SubtitleMovieResolution.ResolveAsync(
            catalog, scanner, reader, id, CancellationToken.None);

        // assert
        resolved.PrimaryFileName.ShouldBe(PrimaryFileName);
        resolved.PrimaryFileName.ShouldNotContain("/");
        resolved.PrimaryFileName.ShouldNotContain("\\");
    }

    [Fact]
    public async Task ResolveAsync_MissingMovie_ThrowsNotFoundWithoutAuthorizing()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        catalog.GetDetailsAsync(id, Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);

        // act
        var act = () => SubtitleMovieResolution.ResolveAsync(
            catalog, scanner, reader, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        reader.DidNotReceiveWithAnyArgs().AuthorizeFolder(Arg.Any<LibraryPath>());
        scanner.DidNotReceiveWithAnyArgs().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
    }

    [Fact]
    public async Task ResolveAsync_KnownMovie_AuthorizesDetailsFolder()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning($"{MovieFolder}/{PrimaryFileName}");

        // act
        await SubtitleMovieResolution.ResolveAsync(catalog, scanner, reader, id, CancellationToken.None);

        // assert
        reader.Received(1).AuthorizeFolder(new LibraryPath(MovieFolder));
    }

    [Fact]
    public async Task ResolveAsync_MissingPrimaryVideo_ThrowsNotFound()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new InvalidTransitionException("no primary"));

        // act
        var act = () => SubtitleMovieResolution.ResolveAsync(
            catalog, scanner, reader, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ResolveAsync_DeniedFolder_PropagatesWithoutResolvingPrimary()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        reader.When(instance => instance.AuthorizeFolder(Arg.Any<LibraryPath>()))
            .Throw(new LibraryFileAccessDeniedException());

        // act
        var act = () => SubtitleMovieResolution.ResolveAsync(
            catalog, scanner, reader, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        scanner.DidNotReceiveWithAnyArgs().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
    }

    [Fact]
    public async Task ResolveAsync_UnexpectedScannerFailure_PropagatesInsteadOfNotFound()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new InvalidOperationException("boom"));

        // act
        var act = () => SubtitleMovieResolution.ResolveAsync(
            catalog, scanner, reader, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ResolveAsync_ForwardsCancellationTokenToCatalog()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning($"{MovieFolder}/{PrimaryFileName}");
        using var source = new CancellationTokenSource();
        source.Cancel();

        // act
        await SubtitleMovieResolution.ResolveAsync(catalog, scanner, reader, id, source.Token);

        // assert
        await catalog.Received(1).GetDetailsAsync(id, source.Token);
    }

    private void DetailsReturning(MovieId id) =>
        catalog.GetDetailsAsync(id, Arg.Any<CancellationToken>()).Returns(new MovieDetails(
            id,
            "Heat",
            new LibraryPath(MovieFolder),
            new MediaFormat("mkv"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched));

    private void PrimaryReturning(string fullPath) =>
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(new LibraryPath(fullPath), new MediaFormat("mkv")));
}
