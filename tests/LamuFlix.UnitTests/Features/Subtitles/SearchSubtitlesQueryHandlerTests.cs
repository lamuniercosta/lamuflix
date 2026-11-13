using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using ProviderOptionsAccessor = Microsoft.Extensions.Options.IOptions<LamuFlix.Core.Options.SubtitleProviderOptions>;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SearchSubtitlesQueryHandlerTests
{
    private const int MovieIdValue = 42;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly ILibraryFileReader reader = Substitute.For<ILibraryFileReader>();
    private readonly ISubtitleProvider provider = Substitute.For<ISubtitleProvider>();

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xx-YY")]
    public async Task HandleAsync_UnsupportedLanguage_RejectsTheRequestBeforeAnyProviderCall(string? language)
    {
        // arrange
        Arrange();
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), language), CancellationToken.None);

        // assert
        var exception = await Should.ThrowAsync<SubtitleRequestRejectedException>(act);
        exception.Message.ShouldBe("The requested subtitle language is not a supported language code.");
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(" pt-br ")]
    [InlineData("PT-BR")]
    [InlineData("pob")]
    public async Task HandleAsync_LanguageSpacingCaseAndAliasDifferences_EmitTheCanonicalCatalogCode(string requested)
    {
        // arrange
        Arrange();
        var searches = RecordSearches(new SubtitleSearchResult([], SubtitleQuota.Unknown));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), requested), CancellationToken.None);

        // assert
        searches.ShouldNotBeEmpty();
        searches.ShouldAllBe(search => search.PreferredLanguages.Count == 1
            && search.PreferredLanguages[0] == "pt-BR");
        result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EmptyPreferredLanguages_ExplicitSupportedLanguageStillSearches()
    {
        // arrange
        Arrange();
        var searches = RecordSearches(new SubtitleSearchResult([], SubtitleQuota.Unknown));
        var handler = CreateHandler(preferredLanguages: []);

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.ShouldBeEmpty();
        searches.ShouldNotBeEmpty();
        foreach (var search in searches)
        {
            search.PreferredLanguages.ShouldBe(["en"]);
        }
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsMovieNotFound()
    {
        // arrange
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        var exception = await Should.ThrowAsync<SubtitleMovieNotFoundException>(act);
        exception.Message.ShouldBe("The movie is not present in the library.");
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_InvalidProviderConfiguration_ThrowsProviderNotConfigured()
    {
        // arrange
        Arrange();
        var handler = CreateHandler(providerConfigured: false);

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        var exception = await Should.ThrowAsync<SubtitleProviderNotConfiguredException>(act);
        exception.Message.ShouldBe("The subtitle provider acquisition settings are missing or invalid.");
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ProviderRateLimit_ReportsASearchRateLimitRatherThanADownloadQuota()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleQuotaExceededException(TimeSpan.FromSeconds(30)));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleSearchRateLimitedException>(act);
    }

    [Fact]
    public async Task HandleAsync_ProviderTimeout_PropagatesTheTypedTimeout()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleProviderTimeoutException());
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleProviderTimeoutException>(act);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_PropagatesTheTypedProviderFailure()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleProviderFailureException("unusable search response"));
        var handler = CreateHandler();

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
    }

    [Fact]
    public async Task HandleAsync_Candidates_AreRankedByMatchThenDownloadsThenReleaseThenCandidateId()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [
                    new SubtitleCandidate(303, "en", 5, false, "Unrelated.Release-GROUP"),
                    new SubtitleCandidate(101, "en", 5, false, "Heat.1995.1080p.BluRay-GROUP"),
                    new SubtitleCandidate(202, "en", 99, true, "Heat.1995.720p-GROUP"),
                    new SubtitleCandidate(404, "en", 5, false, "Heat.1995.1080p.WEB-GROUP"),
                ],
                SubtitleQuota.Unknown)));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.Select(candidate => candidate.ReleaseName).ShouldBe(
        [
            "Heat.1995.720p-GROUP",
            "Heat.1995.1080p.BluRay-GROUP",
            "Heat.1995.1080p.WEB-GROUP",
            "Unrelated.Release-GROUP",
        ]);
        result.Candidates[0].MatchPercent.ShouldBe(SubtitleMatchScorer.MovieHashMatchPercent);
        result.Candidates.Select(candidate => candidate.MatchPercent)
            .ShouldBe(result.Candidates.Select(candidate => candidate.MatchPercent)
                .OrderByDescending(percent => percent));
        result.Candidates.Select(candidate => candidate.DownloadCount).ShouldBe([99, 5, 5, 5]);
    }

    [Fact]
    public async Task HandleAsync_EachCandidate_ReceivesADistinctOpaqueBindingAndKeepsItsQuota()
    {
        // arrange
        Arrange();
        var resetAt = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [
                    new SubtitleCandidate(101, "en", 7, false, "Heat.1995.1080p-GROUP"),
                    new SubtitleCandidate(202, "en", 7, false, "Heat.1995.1080p-GROUP"),
                ],
                new SubtitleQuota(11, resetAt))));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.Length.ShouldBe(2);
        result.Candidates.Select(candidate => candidate.CandidateId).Distinct().Count().ShouldBe(2);
        result.Candidates.ShouldAllBe(candidate =>
            candidate.CandidateId.Length == 32 && candidate.Language == "en" && candidate.DownloadCount == 7);
        result.Quota.RemainingDownloads.ShouldBe(11);
        result.Quota.ResetAtUtc.ShouldBe(resetAt);
    }

    [Fact]
    public async Task HandleAsync_UnknownQuota_KeepsNullFieldsRatherThanGuessing()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult([], SubtitleQuota.Unknown)));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Quota.ShouldBe(SubtitleQuota.Unknown);
        result.Quota.RemainingDownloads.ShouldBeNull();
        result.Quota.ResetAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_WritableLibrary_ReportsDownloadAvailabilityEnabled()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult([], SubtitleQuota.Unknown)));
        var handler = CreateHandler(saveEnabled: true);

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.DownloadEnabled.ShouldBeTrue();
        result.DownloadReason.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ReadOnlyLibrary_StillSearchesAndReportsTheReadOnlyReason()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [new SubtitleCandidate(101, "en", 1, false, "Heat.1995.1080p-GROUP")],
                SubtitleQuota.Unknown)));
        var handler = CreateHandler(saveEnabled: false);

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.DownloadEnabled.ShouldBeFalse();
        result.DownloadReason.ShouldBe(SubtitleProblemReasons.ReadOnlyLibrary);
        result.Candidates.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_HashSearchWithoutCandidates_FallsBackToTheTitleSearch()
    {
        // arrange
        Arrange();
        var searches = new List<SubtitleSearch>();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var search = callInfo.Arg<SubtitleSearch>();
                searches.Add(search);
                return Task.FromResult(new SubtitleSearchResult([], SubtitleQuota.Unknown));
            });
        var handler = CreateHandler();

        // act
        await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        searches.Count.ShouldBe(2);
        searches[0].MovieHash.ShouldNotBeNullOrWhiteSpace();
        searches[1].MovieHash.ShouldBeNull();
        searches[1].Title.ShouldBe("Heat");
    }

    [Fact]
    public async Task HandleAsync_HashSearchWithCandidates_SkipsTheTitleSearch()
    {
        // arrange
        Arrange();
        var hashCandidate = new SubtitleCandidate(101, "en", 7, true, "Heat.1995.1080p.BluRay-GROUP");
        var searches = RecordSearches(new SubtitleSearchResult([hashCandidate], SubtitleQuota.Unknown));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        searches.Count.ShouldBe(1);
        searches[0].MovieHash.ShouldNotBeNullOrWhiteSpace();
        var candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.CandidateId.ShouldNotBeNullOrWhiteSpace();
        candidate.Language.ShouldBe("en");
        candidate.ReleaseName.ShouldBe("Heat.1995.1080p.BluRay-GROUP");
        candidate.MatchPercent.ShouldBe(SubtitleMatchScorer.MovieHashMatchPercent);
        candidate.DownloadCount.ShouldBe(7);
    }

    [Fact]
    public async Task HandleAsync_HashSearchEmptyAndMissingTitle_ReturnsEmptyWithoutTitleSearch()
    {
        // arrange
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(new MovieDetails(
            new MovieId(MovieIdValue),
            string.Empty,
            new LibraryPath(MovieFolder),
            new MediaFormat("mkv"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched));
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(
            new PrimaryVideo(new LibraryPath($"{MovieFolder}/{PrimaryFileName}"), new MediaFormat("mkv")));
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(new MemoryStream([]));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.ShouldBeEmpty();
        result.Quota.ShouldBe(SubtitleQuota.Unknown);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_NullReleaseName_EmitsEmptyReleaseName()
    {
        // arrange
        Arrange();
        var searches = RecordSearches(new SubtitleSearchResult(
            [new SubtitleCandidate(101, "en", 7, true)],
            SubtitleQuota.Unknown));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        searches.Count.ShouldBe(1);
        result.Candidates.ShouldHaveSingleItem().ReleaseName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task HandleAsync_EqualMatchPercents_OrdersByDownloadCount()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [
                    new SubtitleCandidate(101, "en", 1, false, "Heat.1995.1080p.A-GROUP"),
                    new SubtitleCandidate(202, "en", 99, false, "Heat.1995.1080p.B-GROUP"),
                ],
                SubtitleQuota.Unknown)));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.Select(candidate => candidate.DownloadCount).ShouldBe([99, 1]);
    }

    [Fact]
    public async Task HandleAsync_DifferingMatchPercents_OrdersByMatchBeforeDownloads()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [
                    new SubtitleCandidate(101, "en", 99, false, "Heat.1995.1080p.B-GROUP"),
                    new SubtitleCandidate(202, "en", 1, true, "Heat.1995.720p-GROUP"),
                ],
                SubtitleQuota.Unknown)));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.Select(candidate => candidate.MatchPercent).ShouldBe(
        [
            SubtitleMatchScorer.MovieHashMatchPercent,
            60,
        ]);
        result.Candidates[0].DownloadCount.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_EqualMatchAndDownloads_OrdersByReleaseThenCandidateId()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [
                    new SubtitleCandidate(202, "en", 5, false, "Heat.1995.1080p.WEB-GROUP"),
                    new SubtitleCandidate(101, "en", 5, false, "Heat.1995.1080p.BluRay-GROUP"),
                ],
                SubtitleQuota.Unknown)));
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(MovieIdValue), "en"), CancellationToken.None);

        // assert
        result.Candidates.Select(candidate => candidate.ReleaseName).ShouldBe(
        [
            "Heat.1995.1080p.BluRay-GROUP",
            "Heat.1995.1080p.WEB-GROUP",
        ]);
    }

    [Fact]
    public async Task HandleAsync_Success_TagsTheProviderLookupActivity()
    {
        // arrange
        Arrange();
        var searches = RecordSearches(new SubtitleSearchResult([], SubtitleQuota.Unknown));
        var handler = CreateHandler();
        using var ambient = StartActivity();
        var stopped = CaptureStoppedActivities(TelemetryConstants.SubtitleProviderLookup, 424242);

        // act
        await handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(424242), "en"), CancellationToken.None);

        // assert
        searches.ShouldNotBeEmpty();
        var activity = stopped.ShouldHaveSingleItem();
        activity.GetTagItem(TelemetryConstants.MovieId).ShouldBe(424242);
        activity.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.SucceededOutcome);
        ambient.GetTagItem(TelemetryConstants.MovieId).ShouldBe(424242);
    }

    [Fact]
    public async Task HandleAsync_Failure_TagsTheProviderLookupActivityAsAnError()
    {
        // arrange
        Arrange();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubtitleProviderFailureException("unusable search response"));
        var handler = CreateHandler();
        using var ambient = StartActivity();
        var stopped = CaptureStoppedActivities(TelemetryConstants.SubtitleProviderLookup, 424242);

        // act
        var act = () => handler.HandleAsync(new SearchSubtitlesQuery(new MovieId(424242), "en"), CancellationToken.None);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        var activity = stopped.ShouldHaveSingleItem();
        activity.GetTagItem(TelemetryConstants.MovieId).ShouldBe(424242);
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(TelemetryConstants.FailureCategoryTagName).ShouldBe(TelemetryConstants.FailedOutcome);
        ambient.GetTagItem(TelemetryConstants.MovieId).ShouldBe(424242);
    }

    private static Activity StartActivity() => new Activity("test").Start();

    private static List<Activity> CaptureStoppedActivities(string operationName, int movieId)
    {
        var stopped = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == operationName
                    && Equals(activity.GetTagItem(TelemetryConstants.MovieId), movieId))
                {
                    lock (stopped)
                    {
                        stopped.Add(activity);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return stopped;
    }

    private List<SubtitleSearch> RecordSearches(SubtitleSearchResult result)
    {
        var searches = new List<SubtitleSearch>();
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                searches.Add(callInfo.Arg<SubtitleSearch>());
                return Task.FromResult(result);
            });
        return searches;
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
        reader.OpenRead(Arg.Any<LibraryPath>()).Returns(new MemoryStream(new byte[MovieHash.MinimumEligibleLength * 2]));
    }

    private SearchSubtitlesQueryHandler CreateHandler(
        bool saveEnabled = true,
        string[]? preferredLanguages = null,
        bool providerConfigured = true) =>
        new(
            catalog,
            scanner,
            reader,
            provider,
            new SubtitleCandidateBinder(
                TimeProvider.System,
                new Random(20260304),
                () => TimeSpan.FromMinutes(5),
                () => 1000),
            OptionsFactory.Create(new SubtitlesOptions
            {
                SaveEnabled = saveEnabled,
                PreferredLanguages = preferredLanguages ?? [],
            }),
            providerConfigured ? OptionsFactory.Create(ConfiguredProviderOptions()) : UnconfiguredProviderOptions());

    private static ProviderOptionsAccessor UnconfiguredProviderOptions()
    {
        var options = Substitute.For<ProviderOptionsAccessor>();
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
}
