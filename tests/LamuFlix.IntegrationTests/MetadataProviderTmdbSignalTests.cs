using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class MetadataProviderTmdbSignalTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string KeywordsPath = "/3/movie/603/keywords";
    private const string CreditsPath = "/3/movie/603/credits";

    private const string KeywordBody = """
        {
          "id": 603,
          "keywords": [
            { "id": 2001, "name": " Dystopia " },
            { "id": 9715, "name": "Superhero" }
          ]
        }
        """;

    private const string CreditBody = """
        {
          "id": 603,
          "cast": [
            { "id": 6384, "name": "Keanu Reeves", "order": 0, "character": "Neo" },
            { "id": 6385, "name": "keanu reeves", "order": 1, "character": "Thomas Anderson" },
            { "id": 6386, "name": "Carrie-Anne Moss", "order": 2, "character": "Trinity" }
          ],
          "crew": [
            { "id": 9339, "name": "Lana Wachowski", "job": "Director" }
          ]
        }
        """;

    private const string DuplicateKeywordBody = """
        {
          "id": 603,
          "keywords": [
            { "id": 2001, "name": "Zombie" },
            { "id": 2001, "name": "Apocalypse" },
            { "id": 9715, "name": "Superhero" }
          ]
        }
        """;

    private const string EmptyKeywordBody = """{ "id": 603, "keywords": [] }""";

    private const string EmptyCreditBody = """{ "id": 603, "cast": [] }""";

    private const string MissingKeywordArrayBody = """{ "id": 603 }""";

    private const string MalformedKeywordItemBody = """
        { "id": 603, "keywords": [ { "id": 0, "name": "Dystopia" } ] }
        """;

    private const string MalformedCreditItemBody = """
        { "id": 603, "cast": [ { "id": 6384, "name": "   " } ] }
        """;

    private const string MismatchedKeywordBody = """
        { "id": 999, "keywords": [ { "id": 2001, "name": "Dystopia" } ] }
        """;

    private const string SuccessFalseKeywordBody = """
        { "success": false, "status_code": 11, "status_message": "Invalid request" }
        """;

    private const string OmdbFoundBody = """
        {
          "Response": "True",
          "Title": "Solaris",
          "Year": "1972",
          "Plot": "A psychologist...",
          "imdbRating": "8.0",
          "imdbID": "tt0069293"
        }
        """;

    private const string OmdbNotFoundBody = """
        {
          "Response": "False",
          "Error": "Movie not found!"
        }
        """;

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_CompleteSignalPayloads_ReturnsAvailableKeywordsCastAndCollection()
    {
        // arrange
        StubFoundMovie();
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Available);
        signals.Keywords.ShouldBe([new MovieKeyword(2001, "Dystopia"), new MovieKeyword(9715, "Superhero")]);
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
        signals.FullCast.ShouldBe(["Keanu Reeves", "Carrie-Anne Moss"], ignoreOrder: true);
        signals.FullCast.ShouldNotContain("Lana Wachowski");
        signals.CollectionAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_DuplicateKeywordIds_KeepsTheOrdinalSmallestTrimmedName()
    {
        // arrange
        StubFoundMovie(keywordBody: DuplicateKeywordBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.Keywords.ShouldBe([new MovieKeyword(2001, "Apocalypse"), new MovieKeyword(9715, "Superhero")]);
    }

    [Fact]
    public async Task FindAsync_PresentEmptyArrays_ReturnsKnownEmptyKeywordsAndCast()
    {
        // arrange
        StubFoundMovie(keywordBody: EmptyKeywordBody, creditBody: EmptyCreditBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.KeywordAvailability.ShouldBe(SignalAvailability.KnownEmpty);
        signals.Keywords.ShouldBeEmpty();
        signals.FullCastAvailability.ShouldBe(SignalAvailability.KnownEmpty);
        signals.FullCast.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindAsync_DetailsWithoutCollection_ReturnsKnownEmptyCollection()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "collection-null.json"));
        StubSignals();
        var services = probe.BuildServices();

        // act
        var metadata = Metadata(await FindAsync(services, "The Matrix"));

        // assert
        metadata.CollectionId.ShouldBeNull();
        metadata.CollectionName.ShouldBeNull();
        metadata.Signals.ShouldNotBeNull().CollectionAvailability.ShouldBe(SignalAvailability.KnownEmpty);
    }

    [Fact]
    public async Task FindAsync_LargeCastPayload_ReturnsEveryValidNameWithoutACutoff()
    {
        // arrange
        const int castSize = 120;
        StubFoundMovie(creditBody: LargeCastBody(603, castSize));
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
        signals.FullCast.Count.ShouldBe(castSize);
        signals.FullCast.ShouldContain("Performer 1");
        signals.FullCast.ShouldContain(string.Create(CultureInfo.InvariantCulture, $"Performer {castSize}"));
    }

    [Fact]
    public async Task FindAsync_MissingKeywordArray_ReturnsUnavailableKeywordsWithValidCast()
    {
        // arrange
        StubFoundMovie(keywordBody: MissingKeywordArrayBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.Keywords.ShouldBeEmpty();
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_MalformedKeywordItem_ReturnsUnavailableKeywordsWithValidCast()
    {
        // arrange
        StubFoundMovie(keywordBody: MalformedKeywordItemBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_MalformedCreditItem_ReturnsUnavailableCastWithValidKeywords()
    {
        // arrange
        StubFoundMovie(creditBody: MalformedCreditItemBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.FullCast.ShouldBeEmpty();
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Available);
        signals.Keywords.ShouldBe([new MovieKeyword(2001, "Dystopia"), new MovieKeyword(9715, "Superhero")]);
    }

    [Fact]
    public async Task FindAsync_SignalPayloadWithMismatchedMovieId_KeepsOnlyThatSignalUnavailable()
    {
        // arrange
        StubFoundMovie(keywordBody: MismatchedKeywordBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_ErrorSignalBody_ReturnsUnavailableKeywordsWithValidCast()
    {
        // arrange
        StubFoundMovie(keywordBody: SuccessFalseKeywordBody);
        var services = probe.BuildServices();

        // act
        var signals = Signals(await FindAsync(services, "The Matrix"));

        // assert
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(429)]
    public async Task FindAsync_KeywordEndpointFailure_ReturnsUnavailableKeywordsAndAvailableCast(int statusCode)
    {
        // arrange
        probe.StubFoundLookup();
        probe.StubPath(KeywordsPath, statusCode);
        probe.StubPath(CreditsPath, 200, CreditBody);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix");

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        var signals = Signals(result);
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_CreditEndpointFailure_KeepsKeywordsAvailable()
    {
        // arrange
        probe.StubFoundLookup();
        probe.StubPath(KeywordsPath, 200, KeywordBody);
        probe.StubPath(CreditsPath, 503);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix");

        // assert
        var signals = Signals(result);
        signals.FullCastAvailability.ShouldBe(SignalAvailability.Unavailable);
        signals.KeywordAvailability.ShouldBe(SignalAvailability.Available);
    }

    [Fact]
    public async Task FindAsync_EverySignalRequest_ResolvesUnderTheApiVersionWithTheQueryKey()
    {
        // arrange
        StubFoundMovie();
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "The Matrix");

        // assert
        probe.RequestPaths.ShouldBe(["/3/search/movie", "/3/movie/603", KeywordsPath, CreditsPath]);
        probe.QueryValues("api_key").ShouldBe([SecondaryMetadataProviderProbe.SentinelApiKey]);
    }

    [Fact]
    public async Task FindAsync_SignalFailures_NeverLeakTheApiKeyIntoLogsOrActivities()
    {
        // arrange
        var sentinel = SecondaryMetadataProviderProbe.SentinelApiKey;
        var escaped = Uri.EscapeDataString(sentinel);
        using var activities = new SecondaryActivityCapture(TelemetryConstants.ActivitySourceName, "System.Net.Http");
        StubFoundMovie(keywordBody: MalformedKeywordItemBody, creditBody: MalformedCreditItemBody);
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "The Matrix");

        // assert
        var logCorpus = string.Join("\n", probe.Logs.Entries.SelectMany(LogFragments));
        logCorpus.ShouldNotContain(sentinel);
        logCorpus.ShouldNotContain(escaped);
        logCorpus.ShouldContain("supplemental signal unavailable");

        var activityCorpus = string.Join(
            "\n",
            activities.Activities.SelectMany(activity => activity.TagObjects
                .Select(tag => string.Concat(tag.Key, "=", tag.Value?.ToString()))));
        activityCorpus.ShouldNotContain(sentinel);
        activityCorpus.ShouldNotContain(escaped);
    }

    [Fact]
    public async Task FindAsync_ChainWithPrimaryFound_SupplementsOnceForTheSameMovie()
    {
        // arrange
        var services = probe.BuildServices();
        probe.StubPath("/", 200, OmdbFoundBody);
        StubFoundMovie();

        // act
        var result = await ChainedAsync(services, "The Matrix");

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Solaris");
        metadata.ImdbId.ShouldBe(new ImdbId("tt0069293"));
        metadata.CollectionId.ShouldBe(2344);
        metadata.CollectionName.ShouldBe("The Matrix Collection");
        metadata.Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Available);
        SecondaryPaths().ShouldBe(["/3/search/movie", "/3/movie/603", KeywordsPath, CreditsPath]);
    }

    [Theory]
    [InlineData("not-found")]
    [InlineData("rate-limited")]
    [InlineData("invalid-response")]
    public async Task FindAsync_ChainSupplementFailure_KeepsThePrimaryFoundWithUnavailableSignals(string outcome)
    {
        // arrange
        var services = probe.BuildServices();
        probe.StubPath("/", 200, OmdbFoundBody);
        StubSupplementFailure(outcome);

        // act
        var result = await ChainedAsync(services, "Solaris");

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Solaris");
        metadata.Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Unavailable);
        metadata.Signals.FullCastAvailability.ShouldBe(SignalAvailability.Unavailable);
        metadata.Signals.CollectionAvailability.ShouldBe(SignalAvailability.Unavailable);
        metadata.CollectionId.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ChainFallbackFound_DoesNotRepeatTheSupplementLookup()
    {
        // arrange
        var services = probe.BuildServices();
        probe.StubPath("/", 200, OmdbNotFoundBody);
        StubFoundMovie();

        // act
        var result = await ChainedAsync(services, "The Matrix");

        // assert
        Metadata(result).Signals.ShouldNotBeNull().KeywordAvailability.ShouldBe(SignalAvailability.Available);
        SecondaryPaths().ShouldBe(["/3/search/movie", "/3/movie/603", KeywordsPath, CreditsPath]);
    }

    [Fact]
    public async Task FindAsync_ChainWithSecondaryDisabled_MakesZeroSignalRequests()
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = "false",
        });
        probe.StubPath("/", 200, OmdbFoundBody);
        StubFoundMovie();

        // act
        var result = await ChainedAsync(services, "The Matrix");

        // assert
        Metadata(result).Title.ShouldBe("Solaris");
        Metadata(result).Signals.ShouldBeNull();
        SecondaryPaths().ShouldBeEmpty();
    }

    private void StubFoundMovie(string? keywordBody = null, string? creditBody = null)
    {
        probe.StubFoundLookup();
        StubSignals(keywordBody, creditBody);
    }

    private void StubSignals(string? keywordBody = null, string? creditBody = null)
    {
        probe.StubPath(KeywordsPath, 200, keywordBody ?? KeywordBody);
        probe.StubPath(CreditsPath, 200, creditBody ?? CreditBody);
    }

    private void StubSupplementFailure(string outcome)
    {
        switch (outcome)
        {
            case "not-found":
                probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "empty.json"));
                break;
            case "rate-limited":
                probe.StubSearch(string.Empty, statusCode: 429);
                break;
            default:
                probe.StubSearch(string.Empty, statusCode: 400);
                break;
        }
    }

    private static string LargeCastBody(int movieId, int castSize)
    {
        var cast = Enumerable
            .Range(1, castSize)
            .Select(index => string.Create(
                CultureInfo.InvariantCulture,
                $"{{ \"id\": {index}, \"name\": \"Performer {index}\" }}"));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{{ \"id\": {movieId}, \"cast\": [{string.Join(", ", cast)}] }}");
    }

    private static async Task<MetadataLookupResult> FindAsync(IServiceProvider services, string title) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup(title, null), TestContext.Current.CancellationToken);

    private static async Task<MetadataLookupResult> ChainedAsync(IServiceProvider services, string title) =>
        await services
            .GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup(title, null), TestContext.Current.CancellationToken);

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private IReadOnlyList<string> SecondaryPaths() =>
        [.. probe.RequestPaths.Where(path => path.StartsWith("/3", StringComparison.Ordinal))];

    private static MovieSignalSnapshot Signals(MetadataLookupResult result) =>
        Metadata(result).Signals.ShouldNotBeNull();

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
