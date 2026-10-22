using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(OllamaEmbeddingCollection.Name)]
public sealed class OllamaEmbeddingResilienceTests(OllamaEmbeddingProbe probe)
    : IClassFixture<OllamaEmbeddingProbe>, IAsyncLifetime
{
    private const int MaxRetryAttempts = 3;

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GenerateAsync_TransientServerErrorThenSuccess_SucceedsAfterExactlyTwoRequests()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .InScenario("transient")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        probe.Server
            .Given(Request.Create().UsingPost())
            .InScenario("transient")
            .WhenStateIs("recovered")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector())));
        await using var provider = probe.BuildServices();

        // act
        var result = await GenerateAsync(provider, TestContext.Current.CancellationToken);

        // assert
        result.Count.ShouldBe(1);
        probe.RequestCount.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateAsync_ExhaustedTooManyRequests_MapsToRateLimited()
    {
        // arrange
        probe.StubBody(429);
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.RateLimited);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task GenerateAsync_ExhaustedServerError_MapsToProviderUnavailable()
    {
        // arrange
        probe.StubBody(500);
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task GenerateAsync_ExhaustedRequestTimeout_MapsToProviderUnavailable()
    {
        // arrange
        probe.StubBody(408);
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task GenerateAsync_UnreachableEndpoint_MapsToProviderUnavailable()
    {
        // arrange
        var dead = LoopbackWireMock.Start();
        var deadUrl = dead.Urls[0];
        dead.Stop();
        dead.Dispose();
        await using var provider = probe.BuildServices(OllamaEmbeddingProbe.Endpoint(deadUrl));

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task GenerateAsync_AttemptTimeoutWithoutCallerCancellation_MapsToProviderUnavailable()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(2))
                .WithBody(OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector())));
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task GenerateAsync_RetryAfterBeyondTheTotalBudget_MapsToProviderUnavailableWithoutASecondRequest()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "30"));
        var resilience = OllamaEmbeddingProbe.TestResilience() with
        {
            AttemptTimeout = TimeSpan.FromSeconds(3),
            SamplingDuration = TimeSpan.FromSeconds(6),
        };
        await using var provider = probe.BuildServices(resilience: resilience);

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_WhenTheBreakerOpens_MapsToProviderUnavailableWithoutANewRequest()
    {
        // arrange
        probe.StubBody(500);
        var resilience = OllamaEmbeddingProbe.TestResilience() with { MinimumThroughput = 2 };
        await using var provider = probe.BuildServices(resilience: resilience);

        // act
        var opening = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));
        var requestsBeforeOpenCircuit = probe.RequestCount;
        var open = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        opening.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        open.Category.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        requestsBeforeOpenCircuit.ShouldBeGreaterThanOrEqualTo(2);
        probe.RequestCount.ShouldBe(requestsBeforeOpenCircuit);
    }

    [Fact]
    public async Task GenerateAsync_ProviderErrorPayload_IsNotRetriedAndMapsToInvalidResponse()
    {
        // arrange
        probe.StubBody(400, """{"error":"permanent"}""");
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_PermanentNotFound_IsNotRetriedAndMapsToInvalidResponse()
    {
        // arrange
        probe.StubBody(404);
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_MalformedJson_IsNotRetriedAndMapsToInvalidResponse()
    {
        // arrange
        probe.StubBody(200, "{");
        await using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        failure.Category.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringBackoff_PreservesCancellation()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithHeader("Retry-After", "1"));
        await using var provider = probe.BuildServices();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var cancellationToken = cancellation.Token;

        // act
        var act = () => generator.GenerateAsync(["plot"], cancellationToken: cancellationToken);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringSend_PreservesCancellation()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(2))
                .WithBody(OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector())));
        await using var provider = probe.BuildServices();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var cancellationToken = cancellation.Token;

        // act
        var act = () => generator.GenerateAsync(["plot"], cancellationToken: cancellationToken);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private static async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken = default) =>
        await provider
            .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
            .GenerateAsync(["plot"], cancellationToken: cancellationToken);
}


