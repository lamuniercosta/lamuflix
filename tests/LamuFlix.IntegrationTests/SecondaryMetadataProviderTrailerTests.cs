using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class SecondaryMetadataProviderTrailerTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const int MovieId = 603;
    private const string VideosPath = "/3/movie/603/videos";
    private const string BrazilianPortuguese = "pt-BR";
    private const string English = "en";
    private const string PtBrKey = "PtBrUniqKey";
    private const string OriginalKey = "OrigUniqKey";
    private const string ExtraKey = "ExtraUniqKe";

    private const string EmptyVideosBody = """{ "id": 603, "results": [] }""";

    private const string KeywordBody = """
        {
          "id": 603,
          "keywords": [ { "id": 2001, "name": "Dystopia" } ]
        }
        """;

    private const string CreditBody = """
        {
          "id": 603,
          "cast": [ { "id": 6384, "name": "Keanu Reeves" } ]
        }
        """;

    private const string PtBrVideosBody = """
        {
          "id": 603,
          "results": [
            { "iso_639_1": "pt", "iso_3166_1": "BR", "key": "PtBrUniqKey", "site": "YouTube", "type": "Trailer", "official": false }
          ]
        }
        """;

    private const string EnglishVideosBody = """
        {
          "id": 603,
          "results": [
            { "iso_639_1": "en", "iso_3166_1": "US", "key": "OrigUniqKey", "site": "YouTube", "type": "Trailer", "official": true }
          ]
        }
        """;

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_PtBrAndOriginalTrailers_SelectsThePtBrKeyWithTwoDocumentedLanguageRequests()
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, PtBrVideosBody, language: BrazilianPortuguese);
        probe.StubVideos(MovieId, EnglishVideosBody, language: English);
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBe(PtBrKey);
        VideoLanguages().Distinct().ShouldBe([BrazilianPortuguese, English]);
        probe.HasQuery("language").ShouldBeTrue();
        probe.HasQuery("include_video_language").ShouldBeFalse();
    }

    [Fact]
    public async Task FindAsync_NoPtBrCandidate_KeepsTheOriginalLanguageTrailerKey()
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, EmptyVideosBody, language: BrazilianPortuguese);
        probe.StubVideos(MovieId, EnglishVideosBody, language: English);
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBe(OriginalKey);
        VideoLanguages().Distinct().ShouldBe([BrazilianPortuguese, English]);
    }

    [Theory]
    [InlineData("Vimeo", "Trailer")]
    [InlineData("YouTube", "Teaser")]
    [InlineData("YouTube", "Clip")]
    [InlineData("YouTube", "Featurette")]
    public async Task FindAsync_IneligibleSiteOrType_LeavesTheTrailerKeyNull(string site, string type)
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, IneligibleBody(site, type));
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("toolongkeyvalue")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    public async Task FindAsync_BlankMalformedOrUrlShapedKey_LeavesTheTrailerKeyNull(string key)
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, SingleVideoBody(key));
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_IneligibleCandidatesAlongsideAnEligibleOne_SelectsTheEligibleKey()
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, IneligibleVideosWithTrailerBody());
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBe(ExtraKey);
    }

    [Fact]
    public async Task FindAsync_EmptyVideosResults_LeavesTheTrailerKeyNullAndTheEnrichmentFound()
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, EmptyVideosBody);
        var services = BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        Metadata(result).TrailerKey.ShouldBeNull();
        VideoLanguages().Distinct().ShouldBe([BrazilianPortuguese, English]);
    }

    [Fact]
    public async Task FindAsync_EveryVideosRequestFails_LeavesTheTrailerKeyNullWithoutFailingEnrichment()
    {
        // arrange
        StubFoundMovie();
        StubSignals();
        probe.StubEveryVideosLanguage(MovieId, string.Empty, statusCode: 400);
        var services = BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        Metadata(result).TrailerKey.ShouldBeNull();
        Metadata(result).Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Available);
        VideoLanguages().ShouldContain(BrazilianPortuguese);
        VideoLanguages().ShouldContain(English);
    }

    [Fact]
    public async Task FindAsync_MalformedVideosBody_LeavesTheTrailerKeyNullWithoutFailingEnrichment()
    {
        // arrange
        StubFoundMovie();
        probe.StubEveryVideosLanguage(MovieId, "{ \"id\": 603, ");
        var services = BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        Metadata(result).TrailerKey.ShouldBeNull();
        Metadata(result).Title.ShouldBe("The Matrix");
    }

    [Fact]
    public async Task FindAsync_PtBrVideosRequestFails_KeepsTheCandidatesFromTheOriginalLanguageRequest()
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, string.Empty, statusCode: 429, language: BrazilianPortuguese);
        probe.StubVideos(MovieId, EnglishVideosBody, language: English);
        var services = BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        Metadata(result).TrailerKey.ShouldBe(OriginalKey);
        Metadata(result).Title.ShouldBe("The Matrix");
        VideoLanguages().ShouldContain(BrazilianPortuguese);
        VideoLanguages().ShouldContain(English);
    }

    [Fact]
    public async Task FindAsync_VideosOnlyRateLimiting_ReturnsFoundWithoutAnEnrichmentFailure()
    {
        // arrange
        StubFoundMovie();
        StubSignals();
        probe.StubVideos(MovieId, string.Empty, statusCode: 429, language: BrazilianPortuguese);
        probe.StubVideos(MovieId, string.Empty, statusCode: 429, language: English);
        var services = BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        Metadata(result).TrailerKey.ShouldBeNull();
        Metadata(result).Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_VideosRequestsSlowerThanTheAttemptTimeout_LeavesTheKeyNullWithoutFailingEnrichment()
    {
        // arrange
        StubFoundMovie();
        StubSignals();
        probe.StubPath(
            VideosPath,
            statusCode: 200,
            body: PtBrVideosBody,
            delay: TimeSpan.FromSeconds(2));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        Metadata(result).Title.ShouldBe("The Matrix");
        Metadata(result).TrailerKey.ShouldBeNull();
        Metadata(result).Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_DetailsPathRateLimiting_KeepsTheExistingRetryBehaviourAndNoVideosRequest()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(MovieId, string.Empty, statusCode: 429);
        var services = BuildServices();

        // act
        var result = await FindAsync(services);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.RateLimited);
        probe.RequestPaths.Count(path => path == "/3/movie/603").ShouldBe(4);
        VideoLanguages().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("pt")]
    [InlineData("PT")]
    [InlineData("pt-BR")]
    [InlineData("PT-br")]
    public async Task FindAsync_PortugueseOriginalLanguage_MakesExactlyOneVideosRequest(string originalLanguage)
    {
        // arrange
        StubFoundMovie(originalLanguage: originalLanguage);
        probe.StubVideos(MovieId, PtBrVideosBody, language: BrazilianPortuguese);
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBe(PtBrKey);
        VideoLanguages().Distinct().ShouldBe([BrazilianPortuguese]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_BlankOriginalLanguage_MakesOnlyThePtBrVideosRequest(string originalLanguage)
    {
        // arrange
        StubFoundMovie(originalLanguage: originalLanguage);
        probe.StubVideos(MovieId, PtBrVideosBody, language: BrazilianPortuguese);
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBe(PtBrKey);
        VideoLanguages().Distinct().ShouldBe([BrazilianPortuguese]);
    }

    [Fact]
    public async Task FindAsync_NoOriginalLanguageAndNoPtBrCandidate_LeavesTheTrailerKeyNull()
    {
        // arrange
        StubFoundMovie(originalLanguage: string.Empty);
        probe.StubVideos(MovieId, EnglishVideosBody, language: BrazilianPortuguese);
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_SingleQuotedOriginalLanguage_IsTrimmedBeforeTheSecondRequest()
    {
        // arrange
        StubFoundMovie(originalLanguage: "  en  ");
        probe.StubVideos(MovieId, EmptyVideosBody, language: BrazilianPortuguese);
        probe.StubVideos(MovieId, EnglishVideosBody, language: English);
        var services = BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services));

        // assert
        metadata.TrailerKey.ShouldBe(OriginalKey);
        VideoLanguages().Distinct().ShouldBe([BrazilianPortuguese, English]);
    }

    [Fact]
    public async Task FindAsync_VideosRequestsCarryTheApiKeyAndResolveUnderTheApiVersion()
    {
        // arrange
        StubFoundMovie();
        probe.StubVideos(MovieId, EmptyVideosBody);
        var services = BuildServices();

        // act
        await FindAsync(services);

        // assert
        probe.RequestPaths.ShouldContain(VideosPath);
        probe.QueryValues("api_key").ShouldBe([SecondaryMetadataProviderProbe.SentinelApiKey]);
    }

    [Fact]
    public async Task FindAsync_CancelledDuringTheFirstVideosRequest_PropagatesWithoutASecondVideosRequest()
    {
        // arrange
        StubFoundMovie();
        probe.StubPath(
            VideosPath,
            statusCode: 200,
            body: EmptyVideosBody,
            delay: TimeSpan.FromSeconds(2));
        probe.StubVideos(MovieId, EnglishVideosBody, language: English);
        var services = probe.BuildServices();
        var provider = services.GetRequiredService<TmdbMetadataProvider>();

        // act
        var act = async () =>
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(350));
            await provider.FindAsync(new MetadataLookup("The Matrix", null), cancellation.Token);
        };

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        VideoLanguages().Count.ShouldBeLessThan(2);
    }

    [Fact]
    public async Task FindAsync_TrailerFailures_NeverLeakTheApiKeyIntoLogs()
    {
        // arrange
        var sentinel = SecondaryMetadataProviderProbe.SentinelApiKey;
        var escaped = Uri.EscapeDataString(sentinel);
        StubFoundMovie();
        probe.StubEveryVideosLanguage(MovieId, string.Empty, statusCode: 400);
        var services = BuildServices();

        // act
        await FindAsync(services);

        // assert
        var logCorpus = string.Join("\n", probe.Logs.Entries.SelectMany(LogFragments));
        logCorpus.ShouldNotContain(sentinel);
        logCorpus.ShouldNotContain(escaped);
        logCorpus.ShouldContain("without a trailer");
    }

    private IServiceProvider BuildServices() =>
        probe.BuildServices(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [SecondaryMetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.AttemptTimeout))] =
                "00:00:05",
            [SecondaryMetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.SamplingDuration))] =
                "00:00:20",
            [SecondaryMetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.TotalTimeout))] =
                "00:00:20",
        });

    private void StubFoundMovie(string? originalLanguage = null)
    {
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(MovieId, DetailsBody(originalLanguage));
    }

    private void StubSignals()
    {
        probe.StubPath("/3/movie/603/keywords", 200, KeywordBody);
        probe.StubPath("/3/movie/603/credits", 200, CreditBody);
    }

    private static string DetailsBody(string? originalLanguage) =>
        originalLanguage is null
            ? SecondaryMetadataProviderProbe.Fixture("Details", "found.json")
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"id\":{MovieId},\"title\":\"The Matrix\",\"original_language\":\"{originalLanguage}\",\"runtime\":136,\"overview\":\"A hacker.\"}}");

    private static string IneligibleBody(string site, string type) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"id\":{MovieId},\"results\":[{{\"iso_639_1\":\"en\",\"iso_3166_1\":\"US\",\"key\":\"dQw4w9WgXcQ\",\"site\":\"{site}\",\"type\":\"{type}\",\"official\":true}}]}}");

    private static string SingleVideoBody(string key) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"id\":{MovieId},\"results\":[{{\"iso_639_1\":\"en\",\"iso_3166_1\":\"US\",\"key\":\"{Uri.EscapeDataString(key)}\",\"site\":\"YouTube\",\"type\":\"Trailer\",\"official\":true}}]}}");

    private static string IneligibleVideosWithTrailerBody() =>
        string.Concat(
            $"{{\"id\":{MovieId.ToString(CultureInfo.InvariantCulture)},\"results\":[",
            "{\"iso_639_1\":\"en\",\"iso_3166_1\":\"US\",\"key\":\"zzzzzzzzzzz\",\"site\":\"Vimeo\",\"type\":\"Trailer\",\"official\":true},",
            "{\"iso_639_1\":\"en\",\"iso_3166_1\":\"US\",\"key\":\"zzzzzzzzzzz\",\"site\":\"YouTube\",\"type\":\"Teaser\",\"official\":true},",
            $"{{\"iso_639_1\":\"en\",\"iso_3166_1\":\"US\",\"key\":\"{ExtraKey}\",\"site\":\"YouTube\",\"type\":\"Trailer\",\"official\":true}}",
            "]}");

    private IReadOnlyList<string> VideoLanguages() =>
        [.. probe.Server.LogEntries
            .Where(entry => string.Equals(entry.RequestMessage?.Path, VideosPath, StringComparison.Ordinal))
            .Select(entry => entry.RequestMessage?.Query?["language"].SingleOrDefault() ?? string.Empty)];

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static async Task<MetadataLookupResult> FindAsync(IServiceProvider services) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("The Matrix", null), TestContext.Current.CancellationToken);

    private static IEnumerable<string> LogFragments(MetadataProviderProbe.CapturedLog entry)
    {
        yield return entry.Message;
        yield return entry.ExceptionText;
        foreach (var pair in entry.State)
        {
            yield return pair.Value?.ToString() ?? string.Empty;
        }
    }
}
