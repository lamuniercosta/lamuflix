using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(OllamaEmbeddingCollection.Name)]
public sealed class OllamaEmbeddingCompositionTests(OllamaEmbeddingProbe probe)
    : IClassFixture<OllamaEmbeddingProbe>, IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task StartupVerification_With384Vector_PublishesVerifiedDimensionsAndHealthyState()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        using var provider = probe.BuildServices();

        // act
        await StartAsync(provider);
        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // assert
        VerifiedDimensions(generator).ShouldBe(EmbeddingDimensions.Value);
        Should.NotThrow(() => EmbeddingDimensions.Validate(generator));
        (await CheckAsync(provider)).Status.ShouldBe(HealthStatus.Healthy);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task StartupVerification_With768Vector_FailsWithExpectedAndObservedDimensions()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector(768));
        using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(provider));

        // assert
        failure.Message.ShouldContain(OllamaEmbeddingProbe.ModelName);
        failure.Message.ShouldContain("384");
        failure.Message.ShouldContain("768");
        failure.Message.ShouldContain("127.0.0.1");
        failure.InnerException.ShouldBeOfType<EmbeddingGenerationException>()
            .InnerException.ShouldBeNull();
        (await CheckAsync(provider)).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task StartupVerification_WithMissingModel_FailsClosed()
    {
        // arrange
        probe.StubBody(404);
        using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(provider));

        // assert
        failure.Message.ShouldContain(OllamaEmbeddingProbe.ModelName);
        failure.Message.ShouldContain("invalid_response");
    }

    [Fact]
    public async Task StartupVerification_WithUnavailableEndpoint_FailsClosed()
    {
        // arrange
        var dead = LoopbackWireMock.Start();
        var deadUrl = dead.Urls[0];
        dead.Stop();
        dead.Dispose();
        using var provider = probe.BuildServices(OllamaEmbeddingProbe.Endpoint(deadUrl));

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(provider));

        // assert
        failure.Message.ShouldContain("provider_unavailable");
    }

    [Fact]
    public async Task StartupVerification_WithMalformedResponse_FailsClosed()
    {
        // arrange
        probe.StubBody(200, "{");
        using var provider = probe.BuildServices();

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(provider));

        // assert
        failure.Message.ShouldContain("invalid_response");
    }

    [Fact]
    public async Task StartupVerification_DelayedResponse_CannotCompleteBeforeAwaitedVerification()
    {
        // arrange
        probe.Server
            .Given(WireMock.RequestBuilders.Request.Create().UsingPost())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(2))
                .WithBody(OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector())));
        using var provider = probe.BuildServices();
        var validator = Validator(provider);

        // act
        var startup = validator.StartAsync(TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(startup, Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken));

        // assert
        completed.ShouldNotBeSameAs(startup);
        await Should.ThrowAsync<InvalidOperationException>(() => startup);
    }

    [Fact]
    public async Task StartupVerification_SyntheticCredentialsAndBody_AreRedacted()
    {
        // arrange
        const string credential = "topsecret";
        const string query = "hidden-query";
        const string bodySentinel = "do-not-leak-body";
        probe.StubBody(500, $"{{\"error\":\"{bodySentinel}\"}}");
        var endpoint = $"http://alice:{credential}@127.0.0.1:{new Uri(probe.BaseUrl).Port}/?token={query}";
        using var provider = probe.BuildServices(endpoint);

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(provider));

        // assert
        failure.Message.ShouldNotContain(credential);
        failure.Message.ShouldNotContain(query);
        failure.Message.ShouldNotContain(bodySentinel);
        failure.Message.ShouldContain("127.0.0.1");
    }

    [Fact]
    public async Task ModelState_OverlappingSuccessAndFailure_PublishesALegalSnapshotAndRestoresHealthy()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        using var provider = probe.BuildServices();
        await StartAsync(provider);
        var state = provider.GetRequiredService<OllamaEmbeddingModelState>();
        using var handler = new GateHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(OllamaEmbeddingProbe.Endpoint(probe.BaseUrl)),
        };
        var options = Options.Create(new OllamaOptions
        {
            Endpoint = OllamaEmbeddingProbe.Endpoint(probe.BaseUrl),
            ModelName = OllamaEmbeddingProbe.ModelName,
        });
        using var first = CreateGenerator(httpClient, options, state);
        using var second = CreateGenerator(httpClient, options, state);

        // act
        var outcomes = await Task.WhenAll(
            Attempt(() => first.GenerateAsync(["first"])),
            Attempt(() => second.GenerateAsync(["second"])));

        // assert
        outcomes.Count(succeeded => succeeded).ShouldBe(1);
        (await CheckAsync(provider)).Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);

        var requestsBeforeSuccess = probe.RequestCount;
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        await provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
            .GenerateAsync(["recovery"], cancellationToken: TestContext.Current.CancellationToken);
        var entry = await CheckAsync(provider);
        entry.Status.ShouldBe(HealthStatus.Healthy);
        probe.RequestCount.ShouldBe(requestsBeforeSuccess + 1);
    }

    private static async Task StartAsync(IServiceProvider provider) =>
    await Validator(provider).StartAsync(TestContext.Current.CancellationToken);

    private static OllamaEmbeddingStartupValidator Validator(IServiceProvider provider) =>
        provider.GetServices<IHostedService>().OfType<OllamaEmbeddingStartupValidator>().Single();

    private static OllamaSharpEmbeddingGenerator CreateGenerator(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        OllamaEmbeddingModelState state) =>
        new(
            httpClient,
            options,
            state,
            TimeProvider.System,
            NullLogger<OllamaSharpEmbeddingGenerator>.Instance);

    private static async Task<bool> Attempt(Func<Task> generate)
    {
        try
        {
            await generate();
            return true;
        }
        catch (EmbeddingGenerationException)
        {
            return false;
        }
    }

    private static int? VerifiedDimensions(IEmbeddingGenerator<string, Embedding<float>> generator) =>
        generator.GetService(typeof(EmbeddingGeneratorMetadata))
            .ShouldBeOfType<EmbeddingGeneratorMetadata>()
            .DefaultModelDimensions;

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider provider) =>
        (await provider
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(
                registration => registration.Name == OllamaEmbeddingProbe.CheckName,
                TestContext.Current.CancellationToken))
        .Entries[OllamaEmbeddingProbe.CheckName];

    private sealed class GateHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int requestCount;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Interlocked.Increment(ref requestCount);
            if (index == 2)
            {
                gate.TrySetResult();
            }

            await gate.Task.WaitAsync(cancellationToken);

            return index == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector()),
                        Encoding.UTF8,
                        "application/json"),
                };
        }
    }
}


