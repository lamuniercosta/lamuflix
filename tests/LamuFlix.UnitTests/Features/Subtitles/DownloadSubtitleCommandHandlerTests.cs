using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class DownloadSubtitleCommandHandlerTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string ReleaseName = "Heat.1995.1080p.BluRay-GROUP";
    private static readonly byte[] SidcarPayload = "1\n00:00:01,000 --> 00:00:02,000\nhello subtitle\n"u8.ToArray();

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();
    private readonly ISubtitleSidecarStore store = Substitute.For<ISubtitleSidecarStore>();
    private readonly ISubtitleProvider provider = Substitute.For<ISubtitleProvider>();
    private readonly SubtitleCandidateBinder binder = new(
        TimeProvider.System,
        new Random(20261112),
        () => TimeSpan.FromMinutes(5),
        () => 1000);

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
    public async Task HandleAsync_MissingMovie_ThrowsMovieNotFoundBeforeAnythingElse()
    {
        // arrange
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), "0123456789abcdef0123456789abcdef"),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleMovieNotFoundException>(act);
        await store.DidNotReceiveWithAnyArgs().EnsurePublishableAsync(
            Arg.Any<LibraryPath>(), Arg.Any<string>(), TestContext.Current.CancellationToken);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ffffffffffffffffffffffffffffffff")]
    public async Task HandleAsync_UnknownOrStaleCandidate_ThrowsStaleCandidateBeforePaidCall(string? candidateId)
    {
        // arrange
        Arrange();
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleStaleCandidateException>(act);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_CandidateIssuedForAnotherMovie_ThrowsStaleCandidateBeforePaidCall()
    {
        // arrange
        Arrange();
        var foreignCandidateId = binder.Issue(
            new MovieId(MovieIdValue + 1), "en", Candidate(), ReleaseName, SubtitleQuota.Unknown);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), foreignCandidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleStaleCandidateException>(act);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ReadOnlyLibrary_ThrowsReadOnlyBeforeProviderConfigAndPaidCall()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        var handler = CreateHandler(saveEnabled: false, providerConfigured: false);

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleReadOnlyLibraryException>(act);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_MissingProviderConfiguration_ThrowsProviderNotConfiguredBeforePaidCall()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        var handler = CreateHandler(providerConfigured: false);

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleProviderNotConfiguredException>(act);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ContainmentRefusal_ThrowsAccessDeniedBeforePaidCall()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        store.EnsurePublishableAsync(Arg.Any<LibraryPath>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LibraryFileAccessDeniedException("outside the authorized library root"));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<LibraryFileAccessDeniedException>(act);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_KnownZeroQuota_RefusesBeforeThePaidCall()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate(quota: new SubtitleQuota(0, null));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        var exhausted = await Should.ThrowAsync<SubtitleDownloadQuotaExhaustedException>(act);
        exhausted.Quota.RemainingDownloads.ShouldBe(0);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(7)]
    public async Task HandleAsync_UnknownOrPositiveLastKnownQuota_ProceedsAfterPrechecks(int? remaining)
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate(quota: new SubtitleQuota(remaining, null));
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleDownload(SidcarPayload, new SubtitleQuota(remaining, null))));
        store.PublishToSubsAsync(
                Arg.Any<LibraryPath>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SubtitlePublishResult.Published("Heat.1995.1080p-en.srt")));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        result.Track.Language.ShouldBe("en");
        result.Quota.ShouldBe(new SubtitleQuota(remaining, null));
        await provider.ReceivedWithAnyArgs(1).DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_InvalidSrtPayload_ThrowsProviderFailureBeforeStaging()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleDownload("<html>not a subtitle</html>"u8.ToArray(), SubtitleQuota.Unknown)));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        await store.DidNotReceiveWithAnyArgs().PublishToSubsAsync(
            Arg.Any<LibraryPath>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<int>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_OversizePayload_ThrowsProviderFailureBeforeStaging()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleDownload(new byte[16], SubtitleQuota.Unknown)));
        var handler = CreateHandler(maxDownloadBytes: 8);
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // act + assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        await store.DidNotReceiveWithAnyArgs().PublishToSubsAsync(
            Arg.Any<LibraryPath>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<int>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ValidCandidate_PublishesWithConfiguredAttemptsAndReturnsTrack()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate(quota: new SubtitleQuota(5, null));
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleDownload(SidcarPayload, new SubtitleQuota(4, null))));
        store.PublishToSubsAsync(
                Arg.Any<LibraryPath>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SubtitlePublishResult.Published("Heat.1995.1080p-en.srt")));
        var handler = CreateHandler(maxAttempts: 37);

        // act
        var result = await handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        result.Track.Language.ShouldBe("en");
        result.Track.Label.ShouldBe("English");
        result.Track.IsDefault.ShouldBeFalse();
        result.Track.Id.ShouldNotBeNullOrWhiteSpace();
        result.Quota.RemainingDownloads.ShouldBe(4);
        await store.Received(1).PublishToSubsAsync(
            Arg.Any<LibraryPath>(),
            PrimaryFileName,
            "en",
            Arg.Any<ReadOnlyMemory<byte>>(),
            37,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AttemptCeilingExhausted_ThrowsReadOnlyLibrary()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleDownload(SidcarPayload, SubtitleQuota.Unknown)));
        store.PublishToSubsAsync(
                Arg.Any<LibraryPath>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SubtitlePublishResult.AttemptsExhausted()));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleReadOnlyLibraryException>(act);
    }

    [Fact]
    public async Task HandleAsync_ProviderTimeoutDuringDownload_PropagatesTheTypedTimeout()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleProviderTimeoutException());
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleProviderTimeoutException>(act);
        await provider.ReceivedWithAnyArgs(1).DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_CallerCancellation_PropagatesAndIsNotATimeout()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var token = cancellation.Token;
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .Returns<Task<SubtitleDownload>>(callInfo =>
            {
                callInfo.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.FromResult(new SubtitleDownload(SidcarPayload, SubtitleQuota.Unknown));
            });
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task HandleAsync_ProviderQuotaExceeded_DuringDownload_PropagatesTheTypedFailure()
    {
        // arrange
        Arrange();
        var candidateId = IssueCandidate();
        provider.DownloadAsync(Arg.Any<SubtitleCandidate>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleQuotaExceededException(TimeSpan.FromSeconds(30)));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
    }

    [Fact]
    public async Task HandleAsync_ExpiredCandidate_ThrowsStaleCandidateWithoutPaidCall()
    {
        // arrange
        Arrange();
        var clock = new MutableTimeProvider(DateTimeOffset.UnixEpoch);
        var expiringBinder = new SubtitleCandidateBinder(
            clock,
            new Random(20261112),
            () => TimeSpan.FromMinutes(5),
            () => 1000);
        var candidateId = expiringBinder.Issue(
            new MovieId(MovieIdValue), "en", Candidate(), ReleaseName, SubtitleQuota.Unknown);
        var handler = CreateHandler(expiringBinder);

        // act
        clock.Advance(TimeSpan.FromMinutes(6));
        var act = () => handler.HandleAsync(
            new DownloadSubtitleCommand(new MovieId(MovieIdValue), candidateId),
            CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleStaleCandidateException>(act);
        await provider.DidNotReceiveWithAnyArgs().DownloadAsync(
            Arg.Any<SubtitleCandidate>(), TestContext.Current.CancellationToken);
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
            new PrimaryVideo(new LibraryPath($"{MovieFolder}/{PrimaryFileName}"), new MediaFormat("mkv")));
        reader.EnumerateFiles(Arg.Any<LibraryPath>()).Returns([PrimaryFileName]);
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(new MemoryStream([0x1A, 0x45, 0xDF, 0xA3]));
    }

    private string IssueCandidate(SubtitleQuota? quota = null) =>
        binder.Issue(new MovieId(MovieIdValue), "en", Candidate(), ReleaseName, quota ?? SubtitleQuota.Unknown);

    private DownloadSubtitleCommandHandler CreateHandler(
        SubtitleCandidateBinder? candidateBinder = null,
        bool saveEnabled = true,
        bool providerConfigured = true,
        long maxDownloadBytes = SubtitlesOptions.DefaultMaxSubtitleDownloadBytes,
        int maxAttempts = SubtitlesOptions.DefaultMaxSubtitlePublicationAttempts) =>
        new(
            catalog,
            scanner,
            reader,
            store,
            provider,
            candidateBinder ?? binder,
            OptionsFactory.Create(new SubtitlesOptions
            {
                SaveEnabled = saveEnabled,
                PreferredLanguages = ["en"],
                MaxSubtitleDownloadBytes = maxDownloadBytes,
                MaxSubtitlePublicationAttempts = maxAttempts,
            }),
            providerConfigured ? OptionsFactory.Create(ConfiguredProviderOptions()) : UnconfiguredProviderOptions(),
            OptionsFactory.Create(new LibraryOptions
            {
                RootPath = MovieFolder,
                MaxSubtitleCues = 100000,
                MaxSubtitleLineCharacters = 16384,
                MaxSubtitleOutputBytes = 33554432,
            }),
            NullLogger<DownloadSubtitleCommandHandler>.Instance);

    private static Microsoft.Extensions.Options.IOptions<SubtitleProviderOptions> UnconfiguredProviderOptions()
    {
        var options = Substitute.For<Microsoft.Extensions.Options.IOptions<SubtitleProviderOptions>>();
        options.Value.Returns(_ => throw new Microsoft.Extensions.Options.OptionsValidationException(
            SubtitleProviderOptions.SectionName,
            typeof(SubtitleProviderOptions),
            ["The ApiKey field must be provided."]));
        return options;
    }

    private static SubtitleProviderOptions ConfiguredProviderOptions() =>
        new()
        {
            ApiKey = "sentinel-key",
            UserAgent = "LamuFlix/test",
            BaseUrl = "https://api.opensubtitles.invalid",
            DownloadAllowedHosts = ["dl.opensubtitles.invalid"],
            MaxDownloadBytes = 4096,
            RequestTimeout = TimeSpan.FromSeconds(5),
        };

    private static SubtitleCandidate Candidate() =>
        new(FileId: 101, Language: "en", DownloadCount: 12, MatchesMovieHash: false, ReleaseName: ReleaseName);

    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset current = start;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan delta) => current = current.Add(delta);
    }
}
