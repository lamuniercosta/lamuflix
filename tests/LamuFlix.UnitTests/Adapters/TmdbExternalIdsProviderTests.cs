using System;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.UnitTests.Adapters;

public sealed class TmdbExternalIdsProviderTests
{
    [Fact]
    public async Task GetExternalIdsAsync_ReturnsImdbIdFromOnDemandLookup()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "id": 123, "imdb_id": "tt0111161" }"""));
        var provider = Provider(handler, enabled: true);

        // act
        var imdbId = await provider.GetExternalIdsAsync(123, TestContext.Current.CancellationToken);

        // assert
        imdbId.ShouldBe(new ImdbId("tt0111161"));
        handler.Requests.ShouldHaveSingleItem().AbsolutePath.ShouldBe("/3/movie/123/external_ids");
        handler.Requests[0].Query.ShouldContain("api_key=key");
    }

    [Fact]
    public async Task GetExternalIdsAsync_EmptyOrMalformedResponses_ReturnNullAfterEachCall()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "id": 123, "imdb_id": "" }"""));
        var provider = Provider(handler, enabled: true);

        // act
        var first = await provider.GetExternalIdsAsync(201, TestContext.Current.CancellationToken);
        var second = await provider.GetExternalIdsAsync(202, TestContext.Current.CancellationToken);
        var third = await provider.GetExternalIdsAsync(203, TestContext.Current.CancellationToken);

        // assert
        first.ShouldBeNull();
        second.ShouldBeNull();
        third.ShouldBeNull();
        handler.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetExternalIdsAsync_FailedAndEmptyOutcomes_AllCountTowardTheCallSequence()
    {
        // arrange
        var handler = new RecordingHandler(
            (HttpStatusCode.InternalServerError, string.Empty),
            (HttpStatusCode.OK, "not json"),
            (HttpStatusCode.OK, """{ "id": 301 }"""));
        var provider = Provider(handler, enabled: true);

        // act
        var outcomes = new List<ImdbId?>();
        foreach (var tmdbId in new[] { 301, 302, 303 })
        {
            outcomes.Add(await provider.GetExternalIdsAsync(tmdbId, TestContext.Current.CancellationToken));
        }

        // assert
        outcomes.All(imdbId => imdbId is null).ShouldBeTrue();
        handler.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetDiscoveryExternalIdAsync_ExplicitNullId_IsConfirmedAbsence()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "id": 201, "imdb_id": null }"""));
        var provider = Provider(handler, enabled: true);

        // act
        var result = await provider.GetDiscoveryExternalIdAsync(201, TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(DiscoveryExternalIdStatus.ConfirmedAbsence);
        result.ImdbId.ShouldBeNull();
    }

    [Fact]
    public async Task GetDiscoveryExternalIdAsync_OmittedField_IsUnresolved()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "id": 202 }"""));
        var provider = Provider(handler, enabled: true);

        // act
        var result = await provider.GetDiscoveryExternalIdAsync(202, TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(DiscoveryExternalIdStatus.Unresolved);
    }

    [Theory]
    [InlineData("""{ "id": 203, "imdb_id": "" }""")]
    [InlineData("""{ "id": 203, "imdb_id": "not-an-id" }""")]
    [InlineData("""{ "id": 203, "imdb_id": 123 }""")]
    public async Task GetDiscoveryExternalIdAsync_MalformedIdentifier_IsUnresolved(string body)
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, body));
        var provider = Provider(handler, enabled: true);

        // act
        var result = await provider.GetDiscoveryExternalIdAsync(203, TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(DiscoveryExternalIdStatus.Unresolved);
    }

    [Fact]
    public async Task GetDiscoveryExternalIdAsync_TransportFailure_IsUnresolved()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.InternalServerError, string.Empty));
        var provider = Provider(handler, enabled: true);

        // act
        var result = await provider.GetDiscoveryExternalIdAsync(204, TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(DiscoveryExternalIdStatus.Unresolved);
    }

    [Fact]
    public async Task GetDiscoveryExternalIdAsync_ValidId_IsFound()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "id": 205, "imdb_id": "tt0111161" }"""));
        var provider = Provider(handler, enabled: true);

        // act
        var result = await provider.GetDiscoveryExternalIdAsync(205, TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(DiscoveryExternalIdStatus.Found);
        result.ImdbId.ShouldBe(new ImdbId("tt0111161"));
    }

    [Fact]
    public async Task GetDiscoveryExternalIdAsync_WhenTmdbIsDisabled_MakesNoCall()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "imdb_id": "tt0111161" }"""));
        var provider = Provider(handler, enabled: false);

        // act
        var result = await provider.GetDiscoveryExternalIdAsync(206, TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(DiscoveryExternalIdStatus.Unresolved);
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetExternalIdsAsync_WhenTmdbIsDisabled_MakesNoCall()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "imdb_id": "tt0111161" }"""));
        var provider = Provider(handler, enabled: false);

        // act
        var imdbId = await provider.GetExternalIdsAsync(123, TestContext.Current.CancellationToken);

        // assert
        imdbId.ShouldBeNull();
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindAsync_EnrichmentProjection_StaysUnchangedAndMakesNoExternalIdsCall()
    {
        // arrange
        var handler = new RecordingHandler((HttpStatusCode.OK, """{ "id": 123 }"""));
        handler.Respond("/3/find/tt0083658", """{ "movie_results": [ { "id": 401 } ] }""");
        handler.Respond(
            "/3/movie/401",
            """{ "id": 401, "title": "Blade Runner", "imdb_id": "tt0083658", "belongs_to_collection": { "id": 999, "name": "Blade Runner" } }""");
        var provider = Provider(handler, enabled: true);

        // act
        var result = await provider.FindAsync(
            new MetadataLookup("Blade Runner", null, new ImdbId("tt0083658")),
            TestContext.Current.CancellationToken);

        // assert
        var found = result.ShouldBeOfType<MetadataLookupResult.Found>();
        found.Metadata.Title.ShouldBe("Blade Runner");
        found.Metadata.ImdbId.ShouldBe(new ImdbId("tt0083658"));
        found.Metadata.CollectionId.ShouldBe(999);
        handler.Requests.ShouldNotContain(uri => uri.AbsolutePath.EndsWith("/external_ids", StringComparison.Ordinal));
    }

    private static TmdbMetadataProvider Provider(RecordingHandler handler, bool enabled) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:9999/3/") },
            Microsoft.Extensions.Options.Options.Create(new TmdbOptions { Enabled = enabled, ApiKey = "key" }),
            Microsoft.Extensions.Options.Options.Create(new OmdbOptions()),
            TimeProvider.System,
            NullLogger<TmdbMetadataProvider>.Instance,
            new SecondaryMetadataProviderHealthState());

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> responses = new();

        private readonly Dictionary<string, string> overrides = new(StringComparer.Ordinal);

        public RecordingHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            foreach (var response in responses)
            {
                this.responses.Enqueue(response);
            }
        }

        public List<Uri> Requests { get; } = [];

        public void Respond(string path, string body) => overrides[path] = body;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri
                ?? throw new InvalidOperationException("The request URI is missing.");
            Requests.Add(uri);
            var (status, body) = overrides.TryGetValue(uri.AbsolutePath, out var overrideBody)
                ? (HttpStatusCode.OK, overrideBody)
                : responses.Count > 0 ? responses.Dequeue() : (HttpStatusCode.InternalServerError, string.Empty);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
