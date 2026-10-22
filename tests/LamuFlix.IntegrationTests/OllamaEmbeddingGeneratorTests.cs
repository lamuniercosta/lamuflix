using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Fluent;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(OllamaEmbeddingCollection.Name)]
public sealed class OllamaEmbeddingGeneratorTests(OllamaEmbeddingProbe probe)
    : IClassFixture<OllamaEmbeddingProbe>, IAsyncLifetime
{
    private const string PlotText = "A sentinel plot line that must never leak.";

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GenerateAsync_SingleText_UsesConfiguredModelAndUnmodifiedInputWithoutWireDimensions()
    {
        // arrange
        var expected = OllamaEmbeddingProbe.IndexedVector();
        probe.StubVectors(expected);
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var result = await generator.GenerateAsync(
            [PlotText],
            new EmbeddingGenerationOptions { Dimensions = EmbeddingDimensions.Value },
            TestContext.Current.CancellationToken);

        // assert
        result.Count.ShouldBe(1);
        result[0].Vector.ToArray().ShouldBe(expected);

        using var body = JsonDocument.Parse(probe.LastRequestBody);
        body.RootElement.GetProperty("model").GetString().ShouldBe(OllamaEmbeddingProbe.ModelName);
        body.RootElement.GetProperty("input").EnumerateArray()
            .Select(element => element.GetString())
            .ShouldBe([PlotText]);
        body.RootElement.TryGetProperty("dimensions", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_ExplicitDimensions_PreservesCallerOptionsAndOmitsWireDimensions()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);
        var options = new EmbeddingGenerationOptions
        {
            Dimensions = EmbeddingDimensions.Value,
            ModelId = OllamaEmbeddingProbe.ModelName,
        };

        // act
        await generator.GenerateAsync(["plot"], options, TestContext.Current.CancellationToken);

        // assert
        options.Dimensions.ShouldBe(EmbeddingDimensions.Value);
        options.ModelId.ShouldBe(OllamaEmbeddingProbe.ModelName);
        using var body = JsonDocument.Parse(probe.LastRequestBody);
        body.RootElement.TryGetProperty("dimensions", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_AdditionalProperties_AreNotRejectedOrMutated()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);
        var options = new EmbeddingGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["custom"] = "value",
            },
        };

        // act
        await generator.GenerateAsync(["plot"], options, TestContext.Current.CancellationToken);

        // assert
        options.AdditionalProperties["custom"].ShouldBe("value");
        using var body = JsonDocument.Parse(probe.LastRequestBody);
        body.RootElement.TryGetProperty("dimensions", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_Batch_PreservesCountAndOrder()
    {
        // arrange
        var first = OllamaEmbeddingProbe.IndexedVector();
        var second = first.Select(value => value + 1).ToArray();
        probe.StubVectors(first, second);
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var result = await generator.GenerateAsync(["first plot", "second plot"], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.Count.ShouldBe(2);
        result[0].Vector.ToArray().ShouldBe(first);
        result[1].Vector.ToArray().ShouldBe(second);

        using var body = JsonDocument.Parse(probe.LastRequestBody);
        body.RootElement.GetProperty("input").EnumerateArray()
            .Select(element => element.GetString())
            .ShouldBe(["first plot", "second plot"]);
    }

    [Fact]
    public async Task GenerateAsync_EmptyBatch_ReturnsEmptyWithoutHttp()
    {
        // arrange
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var result = await generator.GenerateAsync([], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeEmpty();
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_NullBatch_ThrowsArgumentNullExceptionWithoutHttp()
    {
        // arrange
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var act = () => generator.GenerateAsync(null!, cancellationToken: TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
        probe.RequestCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateAsync_BlankItem_ThrowsArgumentExceptionWithoutHttp(string value)
    {
        // arrange
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var act = () => generator.GenerateAsync([value], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentException>(act);
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_NullItem_ThrowsArgumentExceptionWithoutHttp()
    {
        // arrange
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var act = () => generator.GenerateAsync([null!], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentException>(act);
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_ModelMismatch_ThrowsArgumentExceptionWithoutHttp()
    {
        // arrange
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var act = () => generator.GenerateAsync(
            ["plot"],
            new EmbeddingGenerationOptions { ModelId = "nomic-embed-text" });

        // assert
        await Should.ThrowAsync<ArgumentException>(act);
        probe.RequestCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(768)]
    [InlineData(128)]
    [InlineData(0)]
    public async Task GenerateAsync_UnsupportedDimensions_ThrowsArgumentOutOfRangeWithoutHttp(int dimensions)
    {
        // arrange
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var act = () => generator.GenerateAsync(
            ["plot"],
            new EmbeddingGenerationOptions { Dimensions = dimensions });

        // assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_WrongDimension_ThrowsInvalidResponseWithExpectedAndObservedDimensions()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector(768));
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync([PlotText], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        failure.Message.ShouldContain("384");
        failure.Message.ShouldContain("768");
        failure.Message.ShouldNotContain(PlotText);
    }

    [Fact]
    public async Task GenerateAsync_CountMismatch_ThrowsInvalidResponse()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync(["one", "two"], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task GenerateAsync_EmptyVector_ThrowsInvalidResponse()
    {
        // arrange
        probe.StubVectors(Array.Empty<float>());
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync(["plot"], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        failure.Message.ShouldContain("observed 0");
    }

    [Fact]
    public async Task GenerateAsync_MissingEmbeddings_ThrowsInvalidResponse()
    {
        // arrange
        probe.StubBody(200, """{"model":"all-minilm"}""");
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync(["plot"], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task GenerateAsync_MalformedJson_ThrowsInvalidResponse()
    {
        // arrange
        probe.StubBody(200, "{");
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync(["plot"], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        failure.InnerException.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateAsync_NonFiniteNumber_ThrowsInvalidResponse()
    {
        // arrange
        var overflowing = string.Concat(
            """{"model":"all-minilm","embeddings":[[""",
            string.Join(",", Enumerable.Repeat("1e39", EmbeddingDimensions.Value)),
            """]]}""");
        probe.StubBody(200, overflowing);
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync(["plot"], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task GenerateAsync_ProviderErrorPayload_ThrowsSanitizedInvalidResponse()
    {
        // arrange
        probe.StubBody(400, """{"error":"do-not-leak-sentinel"}""");
        await using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync([PlotText], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        failure.InnerException.ShouldBeNull();
        failure.Message.ShouldNotContain("do-not-leak-sentinel");
        failure.Message.ShouldNotContain(PlotText);
    }

    [Fact]
    public void GetService_BeforeVerification_ReportsMetadataWithNullDimensions()
    {
        // arrange
        // ReSharper disable once UseAwaitUsing - sync test disposes the provider synchronously.
        using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var metadata = generator.GetService(typeof(EmbeddingGeneratorMetadata))
            .ShouldBeOfType<EmbeddingGeneratorMetadata>();

        // assert
        metadata.ProviderName.ShouldBe("ollama");
        metadata.DefaultModelId.ShouldBe(OllamaEmbeddingProbe.ModelName);
        metadata.DefaultModelDimensions.ShouldBeNull();
    }

    [Fact]
    public async Task GetService_AfterVerification_ReportsVerified384AcrossTransientInstances()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        await using var provider = probe.BuildServices();
        var validator = provider.GetServices<IHostedService>().OfType<OllamaEmbeddingStartupValidator>().Single();

        // act
        await validator.StartAsync(TestContext.Current.CancellationToken);
        var first = Resolve(provider);
        var second = Resolve(provider);

        // assert
        VerifiedDimensions(first).ShouldBe(EmbeddingDimensions.Value);
        VerifiedDimensions(second).ShouldBe(EmbeddingDimensions.Value);
        Should.NotThrow(() => EmbeddingDimensions.Validate(second));
    }

    [Fact]
    public void GetService_KeyedOrUnknown_ReturnsNull()
    {
        // arrange
        // ReSharper disable once UseAwaitUsing - sync test disposes the provider synchronously.
        using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var keyed = generator.GetService(typeof(EmbeddingGeneratorMetadata), "key");
        var unknown = generator.GetService(typeof(string));
        var rawApi = generator.GetService(typeof(IOllamaApiClient));
        var rawChat = generator.GetService(typeof(IChatClient));

        // assert
        keyed.ShouldBeNull();
        unknown.ShouldBeNull();
        rawApi.ShouldBeNull();
        rawChat.ShouldBeNull();
    }

    [Fact]
    public void GetService_AssignableType_ReturnsTheWrapper()
    {
        // arrange
        // ReSharper disable once UseAwaitUsing - sync test disposes the provider synchronously.
        using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        // act
        var returned = generator.GetService(typeof(IEmbeddingGenerator<string, Embedding<float>>));

        // assert
        returned.ShouldBeSameAs(generator);
    }

    [Fact]
    public void Dispose_DoesNotDisposeTheInjectedHttpClient()
    {
        // arrange
        using var handler = new TrackingHandler();
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri(OllamaEmbeddingProbe.Endpoint(probe.BaseUrl));
        var generator = new OllamaSharpEmbeddingGenerator(
            httpClient,
            Options.Create(new OllamaOptions
            {
                Endpoint = OllamaEmbeddingProbe.Endpoint(probe.BaseUrl),
                ModelName = OllamaEmbeddingProbe.ModelName,
            }),
            new OllamaEmbeddingModelState(),
            TimeProvider.System,
            NullLogger<OllamaSharpEmbeddingGenerator>.Instance);

        // act
        generator.Dispose();

        // assert
        handler.Disposed.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAsync_SyntheticCredentialsAndBody_AreRedactedFromMessagesAndLogs()
    {
        // arrange
        const string credential = "topsecret";
        const string query = "hidden-query";
        const string bodySentinel = "do-not-leak-body";
        probe.StubBody(400, $"{{\"error\":\"{bodySentinel}\"}}");
        var endpoint = $"http://alice:{credential}@127.0.0.1:{new Uri(probe.BaseUrl).Port}/?token={query}";
        await using var provider = probe.BuildServices(endpoint);
        var generator = Resolve(provider);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(
            () => generator.GenerateAsync([PlotText], cancellationToken: TestContext.Current.CancellationToken));

        // assert
        failure.Message.ShouldNotContain(credential);
        failure.Message.ShouldNotContain(query);
        failure.Message.ShouldNotContain(bodySentinel);
        failure.Message.ShouldNotContain(PlotText);
        failure.Message.ShouldContain("127.0.0.1");
        failure.InnerException.ShouldBeNull();

        var logs = string.Join("\n", probe.Logs.Entries.Select(entry => entry.Message + entry.ExceptionText));
        logs.ShouldNotContain(credential);
        logs.ShouldNotContain(query);
        logs.ShouldNotContain(bodySentinel);
        logs.ShouldNotContain(PlotText);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void GenerateAsync_ArbitraryFiniteBatches_PreserveCountAndOrder()
    {
        // ReSharper disable once UseAwaitUsing - sync property test disposes the provider synchronously.
        using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        Prop.ForAll<int, int>((countSeed, valueSeed) =>
        {
            var count = (countSeed & int.MaxValue) % 4 + 1;
            var vectors = Enumerable.Range(0, count)
                .Select(index => OllamaEmbeddingProbe.IndexedVector()
                    .Select(value => value + index + (valueSeed % 7))
                    .ToArray())
                .ToArray();
            probe.Reset();
            probe.StubVectors(vectors);

            var inputs = Enumerable.Range(0, count)
                .Select(index => $"plot-{valueSeed}-{index}")
                .ToArray();
            var result = generator.GenerateAsync(inputs, cancellationToken: TestContext.Current.CancellationToken).GetAwaiter().GetResult();

            return result.Count == count
                && result.Select((embedding, index) => embedding.Vector.ToArray().SequenceEqual(vectors[index])).All(match => match)
                && probe.RequestCount == 1;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void GenerateAsync_ArbitraryWrongDimensions_AreRejected()
    {
        // ReSharper disable once UseAwaitUsing - sync property test disposes the provider synchronously.
        using var provider = probe.BuildServices();
        var generator = Resolve(provider);

        Prop.ForAll<int>(lengthSeed =>
        {
            var length = (lengthSeed & int.MaxValue) % 383 + 1;
            probe.Reset();
            probe.StubVectors(OllamaEmbeddingProbe.Vector(length));

            var failure = Should.Throw<EmbeddingGenerationException>(
                () => generator.GenerateAsync(["plot"], cancellationToken: TestContext.Current.CancellationToken).GetAwaiter().GetResult());

            return ReferenceEquals(failure.Category, EnrichmentFailureCategory.InvalidResponse);
        }).QuickCheckThrowOnFailure();
    }

    private static IEmbeddingGenerator<string, Embedding<float>> Resolve(IServiceProvider provider) =>
        provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

    private static int? VerifiedDimensions(IEmbeddingGenerator<string, Embedding<float>> generator) =>
        generator.GetService(typeof(EmbeddingGeneratorMetadata))
            .ShouldBeOfType<EmbeddingGeneratorMetadata>()
            .DefaultModelDimensions;

    private sealed class TrackingHandler : HttpMessageHandler
    {
        public bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}




