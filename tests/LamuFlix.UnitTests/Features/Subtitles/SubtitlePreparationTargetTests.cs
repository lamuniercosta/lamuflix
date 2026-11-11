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

public sealed class SubtitlePreparationTargetTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();

    [Fact]
    public async Task ResolveAsync_NullCatalog_ThrowsArgumentNullException()
    {
        // arrange
        var id = new MovieId(MovieIdValue);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitlePreparationTarget.ResolveAsync(null!, scanner, reader, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ResolveAsync_NullScanner_ThrowsArgumentNullException()
    {
        // arrange
        var id = new MovieId(MovieIdValue);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitlePreparationTarget.ResolveAsync(catalog, null!, reader, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ResolveAsync_NullReader_ThrowsArgumentNullException()
    {
        // arrange
        var id = new MovieId(MovieIdValue);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitlePreparationTarget.ResolveAsync(catalog, scanner, null!, id, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ResolveAsync_NullMovieId_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitlePreparationTarget.ResolveAsync(catalog, scanner, reader, null!, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ResolveAsync_MissingMovie_ThrowsNotFound()
    {
        // arrange
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);

        // act
        var act = () => SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, new MovieId(MovieIdValue), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ResolveAsync_MissingPrimaryVideo_ThrowsNotFound()
    {
        // arrange
        DetailsReturning();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new InvalidTransitionException("no primary"));

        // act
        var act = () => SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, new MovieId(MovieIdValue), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ResolveAsync_NonTransitionScannerFailure_PropagatesUnchanged()
    {
        // arrange
        DetailsReturning();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new LibraryFileAccessDeniedException("denied"));

        // act
        var act = () => SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, new MovieId(MovieIdValue), CancellationToken.None);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
    }

    [Fact]
    public async Task ResolveAsync_KnownMovie_ReturnsFolderPrimaryFileNameAndTitle()
    {
        // arrange
        DetailsReturning();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(
                new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
                new MediaFormat("mkv")));

        // act
        var target = await SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, new MovieId(MovieIdValue), CancellationToken.None);

        // assert
        target.Folder.ShouldBe(new LibraryPath(MovieFolder));
        target.PrimaryFile.ShouldBe(new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)));
        target.PrimaryFileName.ShouldBe(PrimaryFileName);
        target.Title.ShouldBe("Heat");
    }

    [Fact]
    public async Task ResolveAsync_KnownMovie_AuthorizesTheDetailsFolderBeforeResolvingPrimary()
    {
        // arrange
        DetailsReturning();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(
                new LibraryPath(Path.Combine(MovieFolder, PrimaryFileName)),
                new MediaFormat("mkv")));

        // act
        await SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, new MovieId(MovieIdValue), CancellationToken.None);

        // assert
        reader.Received(1).AuthorizeFolder(new LibraryPath(MovieFolder));
        scanner.Received(1).ResolvePrimaryVideo(new LibraryPath(MovieFolder));
    }

    [Fact]
    public async Task ResolveAsync_AuthorizeFolderDenied_DoesNotResolvePrimary()
    {
        // arrange
        DetailsReturning();
        reader.When(r => r.AuthorizeFolder(Arg.Any<LibraryPath>())).Do(_ => throw new NotFoundException());

        // act
        var act = () => SubtitlePreparationTarget.ResolveAsync(
            catalog, scanner, reader, new MovieId(MovieIdValue), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        scanner.DidNotReceiveWithAnyArgs().ResolvePrimaryVideo(default!);
    }

    private void DetailsReturning() =>
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
}
