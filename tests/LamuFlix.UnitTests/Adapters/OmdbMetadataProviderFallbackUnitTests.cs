using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.UnitTests.Adapters;

public sealed class OmdbMetadataProviderFallbackUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    private const string MovieNotFound = """{ "Response": "False", "Error": "Movie not found!" }""";
    private const string TooManyResults = """{ "Response": "False", "Error": "Too many results." }""";

    [Fact]
    public async Task FindAsync_TitleMissWithYear_SearchesWithYearThenFetchesByIdWithFullPlot()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("Blade Runner", "1982", "tt0083658"))
                : Ok(SearchBody(Hit("Blade Runner", "1982", "tt0083658"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now)), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.Title.ShouldBe("Blade Runner");
        handler.Requests.Count.ShouldBe(3);
        handler.Requests[1]["s"].ShouldBe("Blade Runner");
        handler.Requests[1]["y"].ShouldBe("1982");
        handler.Requests[1]["type"].ShouldBe("movie");
        handler.Requests[2]["i"].ShouldBe("tt0083658");
        handler.Requests[2]["plot"].ShouldBe("full");
        handler.Requests[2].ContainsKey("t").ShouldBeFalse();
    }

    [Fact]
    public async Task FindAsync_TitleMissWithoutYear_SearchesWithoutYearParameter()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("Blade Runner", "1982", "tt0083658"))
                : Ok(SearchBody(Hit("Blade Runner", "1982", "tt0083658"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[1]["s"].ShouldBe("Blade Runner");
        handler.Requests[1].ContainsKey("y").ShouldBeFalse();
    }

    [Fact]
    public async Task FindAsync_YearFilteredSearchEmpty_RetriesSearchWithoutYear()
    {
        // arrange
        var handler = new RoutingHandler(p =>
        {
            if (p.ContainsKey("t"))
            {
                return Ok(MovieNotFound);
            }

            if (p.ContainsKey("i"))
            {
                return Ok(Detail("Blade Runner", "1982", "tt0083658"));
            }

            return p.ContainsKey("y")
                ? Ok(MovieNotFound)
                : Ok(SearchBody(Hit("Blade Runner", "1982", "tt0083658")));
        });
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now)), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests.Count.ShouldBe(4);
        handler.Requests[1].ContainsKey("y").ShouldBeTrue();
        handler.Requests[2].ContainsKey("y").ShouldBeFalse();
        handler.Requests[3]["i"].ShouldBe("tt0083658");
    }

    [Fact]
    public async Task FindAsync_TitleWithDigits_MatchesCandidateSpelledAsWords()
    {
        // arrange
        var handler = new RoutingHandler(p =>
        {
            if (p.ContainsKey("t"))
            {
                return Ok(MovieNotFound);
            }

            if (p.ContainsKey("i"))
            {
                return Ok(Detail("Two Days in New York", "2012", "tt1602472"));
            }

            return p["s"] == "Two Days in New York"
                ? Ok(SearchBody(Hit("Two Days in New York", "2012", "tt1602472")))
                : Ok(TooManyResults);
        });
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("2 Days in New York", new ReleaseYear(2012, Now)), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.Title.ShouldBe("Two Days in New York");
        handler.Requests.Count.ShouldBe(5);
        handler.Requests[4]["i"].ShouldBe("tt1602472");
    }

    [Fact]
    public async Task FindAsync_CandidateOutsideYearTolerance_ReturnsNotFoundWithoutFetchingById()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : Ok(SearchBody(Hit("The Invite", "2019", "tt9999999"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("The Invite", new ReleaseYear(2026, Now)), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        handler.Requests.Count.ShouldBe(3);
        handler.Requests.ShouldAllBe(p => !p.ContainsKey("i"));
    }

    [Fact]
    public async Task FindAsync_CandidateWithinOneYear_IsAccepted()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("The Invite", "2025", "tt7777777"))
                : Ok(SearchBody(Hit("The Invite", "2025", "tt7777777"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("The Invite", new ReleaseYear(2026, Now)), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[^1]["i"].ShouldBe("tt7777777");
    }

    [Fact]
    public async Task FindAsync_CandidateBelowThreshold_ReturnsNotFoundWithoutFetchingById()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : Ok(SearchBody(Hit("Blade Runner 2049", "2017", "tt1856101"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        handler.Requests.Count.ShouldBe(2);
        handler.Requests.ShouldAllBe(p => !p.ContainsKey("i"));
    }

    [Fact]
    public async Task FindAsync_LowerConfiguredThreshold_AcceptsCandidate()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("Blade Runner 2049", "2017", "tt1856101"))
                : Ok(SearchBody(Hit("Blade Runner 2049", "2017", "tt1856101"))));
        var provider = Provider(handler, threshold: 0.7);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[^1]["i"].ShouldBe("tt1856101");
    }

    [Fact]
    public async Task FindAsync_SeveralCandidates_FetchesTheAboveThresholdTitle()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("Blade Runner", "1982", "tt0083658"))
                : Ok(SearchBody(
                    Hit("Blade Runner 2049", "2017", "tt1856101"),
                    Hit("Blade Runner", "1982", "tt0083658"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[^1]["i"].ShouldBe("tt0083658");
    }

    [Fact]
    public async Task FindAsync_DirectHit_DoesNotSearch()
    {
        // arrange
        var handler = new RoutingHandler(_ => Ok(Detail("Blade Runner", "1982", "tt0083658")));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].ContainsKey("t").ShouldBeTrue();
    }

    [Fact]
    public async Task FindAsync_SearchTooManyResults_ReturnsNotFound()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t") ? Ok(MovieNotFound) : Ok(TooManyResults));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_SearchMalformedBody_ReturnsInvalidResponse()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t") ? Ok(MovieNotFound) : Ok("""{ "Response": "True" }"""));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_SearchRateLimited_ReturnsRateLimited()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t") ? Ok(MovieNotFound) : new Reply((HttpStatusCode)429, string.Empty));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.RateLimited);
    }

    [Fact]
    public async Task FindAsync_SearchServerError_ReturnsProviderUnavailable()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t") ? Ok(MovieNotFound) : new Reply(HttpStatusCode.InternalServerError, string.Empty));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_FallbackMatch_LogsChosenCandidateAndScoreAtInformation()
    {
        // arrange
        var logger = new RecordingLogger();
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("Blade Runner", "1982", "tt0083658"))
                : Ok(SearchBody(Hit("Blade Runner", "1982", "tt0083658"))));
        var provider = Provider(handler, logger);

        // act
        await provider.FindAsync(new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now)), TestContext.Current.CancellationToken);

        // assert
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("Blade Runner", StringComparison.Ordinal)
            && entry.Message.Contains("tt0083658", StringComparison.Ordinal)
            && entry.Message.Contains("score", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindAsync_DashedTitle_SearchesColonVariant()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("i")
            ? Ok(Detail("Futurama: Into the Wild Green Yonder", "2009", "tt1054487"))
            : p.TryGetValue("s", out var search) && search.Contains(':', StringComparison.Ordinal)
                ? Ok(SearchBody(Hit("Futurama: Into the Wild Green Yonder", "2009", "tt1054487")))
                : Ok(MovieNotFound));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(
            new MetadataLookup("Futurama - Into The Wild Green Yonder", new ReleaseYear(2008, Now)),
            TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[^2]["s"].ShouldBe("Futurama: Into The Wild Green Yonder");
        handler.Requests[^1]["i"].ShouldBe("tt1054487");
    }

    [Fact]
    public async Task FindAsync_CandidateTwoYearsLater_IsAccepted()
    {
        // arrange
        var handler = new RoutingHandler(p => p.ContainsKey("t")
            ? Ok(MovieNotFound)
            : p.ContainsKey("i")
                ? Ok(Detail("City of God", "2004", "tt0317248"))
                : Ok(SearchBody(Hit("City of God", "2004", "tt0317248"))));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("City of God", new ReleaseYear(2002, Now)), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[^1]["i"].ShouldBe("tt0317248");
    }

    [Fact]
    public async Task FindAsync_StoredTitleWithReleaseTags_IsRecleanedBeforeLookup()
    {
        // arrange
        var handler = new RoutingHandler(_ => Ok(Detail("Barbie", "2023", "tt1517268")));
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Barbie 2023 720p HDCAM-C1NEM4[TGx]", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.Requests[0]["t"].ShouldBe("Barbie");
        handler.Requests[0]["y"].ShouldBe("2023");
    }

    [Fact]
    public async Task FindAsync_StoredTitleWithEditionLabel_KeepsStoredYear()
    {
        // arrange
        var handler = new RoutingHandler(_ => Ok(Detail("The Lord of the Rings: The Two Towers", "2002", "tt0167261")));
        var provider = Provider(handler);

        // act
        await provider.FindAsync(
            new MetadataLookup("The Lord of the Rings The Two Towers EXTENDED", new ReleaseYear(2002, Now)),
            TestContext.Current.CancellationToken);

        // assert
        handler.Requests[0]["t"].ShouldBe("The Lord of the Rings The Two Towers");
        handler.Requests[0]["y"].ShouldBe("2002");
    }

    [Fact]
    public async Task FindAsync_StoredYearAndYearInTitle_KeepsStoredLookup()
    {
        // arrange
        var handler = new RoutingHandler(_ => Ok(Detail("Death Race 2000", "1975", "tt0072856")));
        var provider = Provider(handler);

        // act
        await provider.FindAsync(new MetadataLookup("Death Race 2000", new ReleaseYear(1975, Now)), TestContext.Current.CancellationToken);

        // assert
        handler.Requests[0]["t"].ShouldBe("Death Race 2000");
        handler.Requests[0]["y"].ShouldBe("1975");
    }

    [Fact]
    public async Task FindAsync_QuotaExhaustedAfterPartialCandidates_DiscardsPartialAndStopsVariants()
    {
        // arrange
        var searches = 0;
        var handler = new RoutingHandler(p =>
        {
            if (p.ContainsKey("t"))
            {
                return Ok(MovieNotFound);
            }

            searches++;
            return searches == 1
                ? Ok(SearchBody(Hit("Blade Runner 2049", "2017", "tt1856101")))
                : throw new MetadataQuotaExhaustedException();
        });
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(
            new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now)),
            TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.RateLimited);
        handler.Requests.Count.ShouldBe(3);
        handler.Requests.ShouldAllBe(p => !p.ContainsKey("i"));
    }

    [Fact]
    public async Task FindAsync_QuotaPersistenceFailureAfterPartialCandidates_DiscardsPartialAndStopsVariants()
    {
        // arrange
        var searches = 0;
        var handler = new RoutingHandler(p =>
        {
            if (p.ContainsKey("t"))
            {
                return Ok(MovieNotFound);
            }

            searches++;
            return searches == 1
                ? Ok(SearchBody(Hit("Blade Runner 2049", "2017", "tt1856101")))
                : throw new MetadataQuotaPersistenceException(new InvalidOperationException("database unavailable"));
        });
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(
            new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now)),
            TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        handler.Requests.Count.ShouldBe(3);
        handler.Requests.ShouldAllBe(p => !p.ContainsKey("i"));
    }

    private static Reply Ok(string body) => new(HttpStatusCode.OK, body);

    private static string Hit(string title, string year, string id) =>
        $$"""{ "Title": "{{title}}", "Year": "{{year}}", "imdbID": "{{id}}", "Type": "movie" }""";

    private static string SearchBody(params string[] hits)
    {
        var joined = string.Join(", ", hits);
        return $$"""{ "Response": "True", "Search": [{{joined}}], "totalResults": "{{hits.Length}}" }""";
    }

    private static string Detail(string title, string year, string id) =>
        $$"""{ "Response": "True", "Title": "{{title}}", "Year": "{{year}}", "Plot": "Full plot.", "imdbID": "{{id}}" }""";

    private static OmdbMetadataProvider Provider(HttpMessageHandler handler, ILogger<OmdbMetadataProvider>? logger = null, double threshold = 0.8)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(
            new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/", MatchThreshold = threshold });
        return new OmdbMetadataProvider(
            http,
            options,
            new FixedTimeProvider(Now),
            logger ?? NullLogger<OmdbMetadataProvider>.Instance,
            new MetadataProviderHealthState());
    }

    private static IReadOnlyDictionary<string, string> Parameters(Uri uri) =>
        uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]), StringComparer.Ordinal);

    private sealed record Reply(HttpStatusCode Status, string Body);

    private sealed class RoutingHandler(Func<IReadOnlyDictionary<string, string>, Reply> route) : HttpMessageHandler
    {
        private readonly List<IReadOnlyDictionary<string, string>> requests = [];

        public IReadOnlyList<IReadOnlyDictionary<string, string>> Requests => requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Request has no URI.");
            var parameters = Parameters(uri);
            requests.Add(parameters);
            var reply = route(parameters);
            return Task.FromResult(new HttpResponseMessage(reply.Status) { Content = new StringContent(reply.Body) });
        }
    }

    private sealed class RecordingLogger : ILogger<OmdbMetadataProvider>
    {
        private readonly ConcurrentBag<(LogLevel Level, string Message)> entries = [];

        public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Add((logLevel, formatter(state, exception)));
    }
}
