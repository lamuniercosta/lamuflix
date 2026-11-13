using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleHandlerSurvivorTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string Stem = "Heat.1995.1080p";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();

    [Fact]
    public async Task HandleAsync_NullContentQuery_ThrowsArgumentNullException()
    {
        // arrange
        var handler = ContentHandler();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => handler.HandleAsync(null!, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task HandleAsync_NullTracksQuery_ThrowsArgumentNullException()
    {
        // arrange
        var handler = new ListSubtitleTracksQueryHandler(catalog, scanner, reader);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => handler.HandleAsync(null!, CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public void Discover_NullPrimaryFileName_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleSidecarDiscovery.Discover(null!, [PrimaryFileName]);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Discover_NullCandidates_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleSidecarDiscovery.Discover(PrimaryFileName, null!);

        // assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("candidateFileNames");
    }

    [Fact]
    public void Discover_SubsReleaseGroup_ResolvesTheCodedLanguage()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover("m.mkv", ["Subs/x.en.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe("Subs/x.en.srt");
        track.Language.ShouldBe("en");
        track.Label.ShouldBe("English (x)");
        track.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void Discover_UnknownSinglePart_ReportsUndeterminedLanguageWithoutDefault()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover("m.mkv", ["Subs/xyz.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Language.ShouldBe("und");
        track.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void TrackToken_NullMovieId_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleTrackToken.For(null!, $"{Stem}.en.srt");

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void TrackToken_NullFileName_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleTrackToken.For(new MovieId(MovieIdValue), null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulContent_TagsCurrentActivityWithMovieId()
    {
        // arrange
        using var activity = new Activity("SubtitleSurvivor").Start();
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        reader.OpenRead(Arg.Any<LibraryPath>())
            .Returns(StreamOf("1\n00:00:01,000 --> 00:00:02,000\ncue\n"));
        var handler = ContentHandler();

        // act
        await handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(id, $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        activity.GetTagItem(TelemetryConstants.MovieId).ShouldBe(MovieIdValue);
    }

    [Fact]
    public async Task HandleAsync_PayloadExactlyAtInputBound_Succeeds()
    {
        // arrange
        const int bound = 64;
        var text = new string('x', 31);
        var document = "1\n00:00:01,000 --> 00:00:02,000\n" + text + "\n";
        Encoding.UTF8.GetByteCount(document).ShouldBe(bound);
        var id = new MovieId(MovieIdValue);
        DetailsReturning(id);
        PrimaryReturning();
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName, $"{Stem}.en.srt"]);
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(StreamOf(document));
        var handler = ContentHandler(bound);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleContentQuery(id, SubtitleTrackToken.For(id, $"{Stem}.en.srt")),
            CancellationToken.None);

        // assert
        result.WebVtt.ShouldContain(text);
    }

    [Fact]
    public void Discover_LanguageOrdersTracksBeforeLabelAndFileName()
    {
        // arrange
        const string primary = "m.mkv";

        // act
        var tracks = SubtitleSidecarDiscovery.Discover(primary, ["m.pob.srt", "m.en.srt", "m.srt"]);

        // assert
        tracks.Select(track => track.FileName).ShouldBe(["m.en.srt", "m.pob.srt", "m.srt"]);
    }

    [Fact]
    public void Discover_LabelOrdersTracksWithSameLanguage()
    {
        // arrange
        const string primary = "m.mkv";

        // act
        var tracks = SubtitleSidecarDiscovery.Discover(primary, ["m.b.srt", "m.a.srt"]);

        // assert
        tracks.Select(track => track.FileName).ShouldBe(["m.a.srt", "m.b.srt"]);
    }

    [Fact]
    public void Discover_FileNameOrdersTracksWithSameLanguageAndLabel()
    {
        // arrange
        const string primary = "m.mkv";

        // act
        var tracks = SubtitleSidecarDiscovery.Discover(primary, ["m.en.srt", "m.EN.srt"]);

        // assert
        tracks.Select(track => track.FileName).ShouldBe(["m.EN.srt", "m.en.srt"]);
    }

    private GetSubtitleContentQueryHandler ContentHandler(int maxInputBytes = 8388608) =>
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
