using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class GetSubtitleContentQueryHandlerTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string Stem = "Heat.1995.1080p";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();

    [Fact]
    public async Task HandleAsync_EligibleTrackToken_ReturnsConvertedWebVttWithoutLeakingThePath()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(new LibraryPath(MovieFolder)).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        reader.OpenRead(new LibraryPath($"{MovieFolder}/{Stem}.en.srt"))
            .Returns(StreamOf("1\n00:00:01,000 --> 00:00:02,000\ncue text\n"));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(id, $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        result.WebVtt.ShouldStartWith("WEBVTT\n\n");
        result.WebVtt.ShouldContain("cue text");
        result.WebVtt.ShouldNotContain(MovieFolder);
    }

    [Fact]
    public async Task HandleAsync_TokenNotInFreshAllowlist_ThrowsNotFoundBeforeOpeningAnything()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new GetSubtitleContentQuery(id, "tampered-token"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        reader.DidNotReceiveWithAnyArgs().OpenRead(default!);
    }

    [Fact]
    public async Task HandleAsync_TokenIssuedForAnotherMovie_ThrowsNotFound()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(new MovieId(MovieIdValue + 1), $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFoundWithoutTouchingTheLibrary()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        catalog.GetDetailsAsync(id, Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new GetSubtitleContentQuery(id, "any"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        reader.DidNotReceiveWithAnyArgs().AuthorizeFolder(default!);
        reader.DidNotReceiveWithAnyArgs().EnumerateFiles(default!);
    }

    [Fact]
    public async Task HandleAsync_MissingPrimaryVideo_ThrowsNotFound()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new InvalidTransitionException("no primary"));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new GetSubtitleContentQuery(id, "any"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task HandleAsync_DeniedFolder_PropagatesAccessDeniedBeforeEnumeration()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        reader.When(instance => instance.AuthorizeFolder(Arg.Any<LibraryPath>()))
            .Throw(new LibraryFileAccessDeniedException());
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new GetSubtitleContentQuery(id, "any"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        reader.DidNotReceiveWithAnyArgs().EnumerateFiles(default!);
    }

    [Fact]
    public async Task HandleAsync_PayloadOverConfiguredInputBound_ThrowsSanitizedValidation()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        reader.OpenRead(Arg.Any<LibraryPath>())
            .Returns(StreamOf("1\n00:00:01,000 --> 00:00:02,000\npayload\n"));
        var handler = CreateHandler(maxInputBytes: 8);

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(id, $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        var thrown = await Should.ThrowAsync<ValidationException>(act);
        thrown.Errors.ShouldContainKey("Track");
    }

    [Fact]
    public async Task HandleAsync_UnrepresentablePayload_ThrowsSanitizedValidation()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(StreamOf("not a subtitle document"));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(id, $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<ValidationException>(act);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEligibleEntryYieldingTheSameToken_FailsClosed()
    {
        // arrange
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>())
            .Returns([PrimaryFileName, $"{Stem}.en.srt", $"{Stem}.en.srt"]);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(id, $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
        reader.DidNotReceiveWithAnyArgs().OpenRead(default!);
    }

    private GetSubtitleContentQueryHandler CreateHandler(int maxInputBytes = 8388608) =>
        new(
            catalog,
            scanner,
            reader,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions
            {
                RootPath = "/library",
                MaxSubtitleInputBytes = maxInputBytes,
            }));

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

    private void PrimaryReturning() =>
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(new LibraryPath($"{MovieFolder}/{PrimaryFileName}"), new MediaFormat("mkv")));

    private static Stream StreamOf(string document) => new MemoryStream(Encoding.UTF8.GetBytes(document));
}
