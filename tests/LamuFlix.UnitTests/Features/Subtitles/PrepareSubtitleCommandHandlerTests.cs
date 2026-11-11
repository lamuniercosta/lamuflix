using System;
using System.Collections.Generic;
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

public sealed class PrepareSubtitleCommandHandlerTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();
    private readonly ISubtitleSidecarStore store = Substitute.For<ISubtitleSidecarStore>();
    private readonly ISubtitleProvider provider = Substitute.For<ISubtitleProvider>();

    [Fact]
    public async Task HandleAsync_NullCommand_ThrowsArgumentNullException()
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
    public async Task HandleAsync_ExistingSidecar_ReturnsAlreadyPresentBeforeAnyAcquisitionCheck()
    {
        // arrange
        Arrange(sidecarPresent: true);
        var handler = CreateHandler(saveEnabled: false);

        // act
        var result = await handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.AlreadyPresent);
        result.SelectedLanguage.ShouldBeNull();
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
        await store.DidNotReceiveWithAnyArgs().CreateAsync(
            Arg.Any<LibraryPath>(), Arg.Any<string>(), default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_NoSidecarAndSaveDisabled_ThrowsAcquisitionUnavailable()
    {
        // arrange
        Arrange(sidecarPresent: false);
        var handler = CreateHandler(saveEnabled: false);

        // act
        var act = () => handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleAcquisitionUnavailableException>(act);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_NoSidecarAndEmptyPreferences_ThrowsAcquisitionUnavailable()
    {
        // arrange
        Arrange(sidecarPresent: false);
        var handler = CreateHandler(preferredLanguages: []);

        // act
        var act = () => handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleAcquisitionUnavailableException>(act);
    }

    [Fact]
    public async Task HandleAsync_NoCandidate_ReturnsNoMatchWithoutDownloadingOrWriting()
    {
        // arrange
        Arrange(sidecarPresent: false);
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SubtitleCandidate>>([]));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.NoMatch);
        result.SelectedLanguage.ShouldBeNull();
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
        await store.DidNotReceiveWithAnyArgs().CreateAsync(
            Arg.Any<LibraryPath>(), Arg.Any<string>(), default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_CreatedSidecar_ReturnsSavedWithTheMatchedLanguage()
    {
        // arrange
        Arrange(sidecarPresent: false);
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SubtitleCandidate>>([new SubtitleCandidate(101, "en", 7, false)]));
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new byte[] { 0x31, 0x0A }));
        store.CreateAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SubtitleSidecarWriteResult.Created));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.Saved);
        result.SelectedLanguage.ShouldBe("en");
    }

    [Fact]
    public async Task HandleAsync_ConcurrentCreateCollision_ReturnsAlreadyPresentAfterSafeRecheck()
    {
        // arrange
        Arrange(sidecarPresent: false);
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SubtitleCandidate>>([new SubtitleCandidate(101, "en", 7, false)]));
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new byte[] { 0x31, 0x0A }));
        store.CreateAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SubtitleSidecarWriteResult.AlreadyPresent));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.AlreadyPresent);
        result.SelectedLanguage.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ProviderQuotaFailure_PropagatesAsATypedCoreFailure()
    {
        // arrange
        Arrange(sidecarPresent: false);
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleQuotaExceededException(TimeSpan.FromSeconds(120)));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
        await store.DidNotReceiveWithAnyArgs().CreateAsync(
            Arg.Any<LibraryPath>(), Arg.Any<string>(), default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFound()
    {
        // arrange
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task HandleAsync_MissingPrimaryVideo_ThrowsNotFound()
    {
        // arrange
        DetailsReturning();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Throws(new InvalidTransitionException("no primary"));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new PrepareSubtitleCommand(new MovieId(MovieIdValue)), CancellationToken.None);

        // assert
        await Should.ThrowAsync<NotFoundException>(act);
    }

    private void Arrange(bool sidecarPresent)
    {
        DetailsReturning();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(new LibraryPath($"{MovieFolder}/{PrimaryFileName}"), new MediaFormat("mkv")));
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName]);
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(new MemoryStream([0x1A, 0x45, 0xDF, 0xA3]));
        store.ExistsAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sidecarPresent));
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

    private PrepareSubtitleCommandHandler CreateHandler(
        bool saveEnabled = true,
        string[]? preferredLanguages = null) =>
        new(
            catalog,
            scanner,
            reader,
            store,
            provider,
            Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions
            {
                SaveEnabled = saveEnabled,
                PreferredLanguages = preferredLanguages ?? ["en"],
            }),
            Microsoft.Extensions.Options.Options.Create(new SubtitleProviderOptions
            {
                ApiKey = "sentinel-key",
                UserAgent = "LamuFlix/test",
                BaseUrl = "https://api.opensubtitles.invalid",
                DownloadAllowedHosts = ["dl.opensubtitles.invalid"],
                MaxDownloadBytes = 4096,
                RequestTimeout = TimeSpan.FromSeconds(5),
            }),
            NullLogger<PrepareSubtitleCommandHandler>.Instance);
}
