using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.UnitTests.Adapters;

public sealed class OmdbMetadataProviderUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    private const string RichBody = """
        {
          "Response": "True",
          "Title": "Blade Runner",
          "Year": "1982",
          "Runtime": "117 min",
          "Plot": "Full plot.",
          "Genre": "Sci-Fi, Thriller",
          "Director": "Ridley Scott",
          "Writer": "Hampton Fancher, David Webb Peoples",
          "Actors": "Harrison Ford, Rutger Hauer, Sean Young",
          "Metascore": "89",
          "imdbRating": "8.1",
          "imdbID": "tt0083658",
          "Poster": "https://example.test/blade-runner.jpg"
        }
        """;

    [Fact]
    public async Task FindAsync_RichBody_MapsGenresCastCrewMetascoreAndFullPlotQuery()
    {
        // arrange
        var handler = new StubHandler(HttpStatusCode.OK, RichBody);
        var provider = Provider(handler);
        var lookup = new MetadataLookup("Blade Runner", new ReleaseYear(1982, Now));

        // act
        var result = await provider.FindAsync(lookup, TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Genres.ShouldBe(["Sci-Fi", "Thriller"]);
        metadata.Writers.ShouldBe(["Hampton Fancher", "David Webb Peoples"]);
        metadata.Directors.ShouldBe(["Ridley Scott"]);
        metadata.Actors.ShouldBe(["Harrison Ford", "Rutger Hauer", "Sean Young"]);
        metadata.MetaScore.ShouldBe((short)89);
        metadata.Synopsis.ShouldBe("Full plot.");
        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.Query.ShouldContain("plot=full");
        handler.RequestUri.Query.ShouldContain("y=1982");
    }

    [Fact]
    public async Task FindAsync_LookupWithoutYear_OmitsYearQueryButKeepsPlotFull()
    {
        // arrange
        var handler = new StubHandler(HttpStatusCode.OK, RichBody);
        var provider = Provider(handler);

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.Query.ShouldContain("plot=full");
        handler.RequestUri.Query.ShouldNotContain("&y=");
    }

    [Fact]
    public async Task FindAsync_NaCreditsAndBadMetascore_YieldsEmptyCollectionsAndNullScore()
    {
        // arrange
        var body = """
            { "Response": "True", "Title": "Solaris", "Genre": "N/A", "Writer": "N/A", "Director": "", "Actors": "  ", "Metascore": "N/A" }
            """;
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Genres.ShouldBeEmpty();
        metadata.Writers.ShouldBeEmpty();
        metadata.Directors.ShouldBeEmpty();
        metadata.Actors.ShouldBeEmpty();
        metadata.MetaScore.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_DuplicateAndPaddedCredits_DedupesAndTrimsKeepingFirstCasing()
    {
        // arrange
        var body = """
            { "Response": "True", "Title": "Dune", "Genre": "Sci-Fi, sci-fi ,  Adventure ", "Writer": "Frank Herbert, FRANK HERBERT", "Actors": "Timothee, Timothee ", "Metascore": "150" }
            """;
        var provider = Provider(new StubHandler(HttpStatusCode.OK, body));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Dune", null), TestContext.Current.CancellationToken);

        // assert
        var metadata = result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;
        metadata.Genres.ShouldBe(["Sci-Fi", "Adventure"]);
        metadata.Writers.ShouldBe(["Frank Herbert"]);
        metadata.Actors.ShouldBe(["Timothee"]);
        metadata.MetaScore.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_MovieNotFound_ReturnsNotFound()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "False", "Error": "Movie not found!" }"""));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Missing", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_OtherErrorBody_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "False", "Error": "Invalid API key!" }"""));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MissingTitle_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, """{ "Response": "True", "Title": "N/A" }"""));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_MalformedJson_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.OK, "not json"));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_Unauthorized_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.Unauthorized, string.Empty));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_RateLimited_ReturnsRateLimited()
    {
        // arrange
        var provider = Provider(new StubHandler((HttpStatusCode)429, string.Empty));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.RateLimited);
    }

    [Fact]
    public async Task FindAsync_ServerError_ReturnsProviderUnavailable()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.InternalServerError, string.Empty));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_NotFoundStatus_ReturnsInvalidResponse()
    {
        // arrange
        var provider = Provider(new StubHandler(HttpStatusCode.NotFound, string.Empty));

        // act
        var result = await provider.FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    private static OmdbMetadataProvider Provider(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new OmdbOptions { ApiKey = "key", BaseUrl = "https://example.test/" });
        return new OmdbMetadataProvider(http, options, new FixedTimeProvider(Now), NullLogger<OmdbMetadataProvider>.Instance, new MetadataProviderHealthState());
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
