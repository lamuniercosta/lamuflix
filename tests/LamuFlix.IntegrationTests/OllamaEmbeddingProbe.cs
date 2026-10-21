using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OllamaEmbeddingCollection
{
    public const string Name = "ollama-embeddings";
}

public sealed class OllamaEmbeddingProbe : IAsyncLifetime
{
    public const string ModelName = "all-minilm";
    public const string CheckName = OllamaEmbeddingServiceCollectionExtensions.HealthCheckName;

    private static readonly HttpClient WarmUpClient = new();
    private readonly List<IDisposable> disposables = [];

    public MetadataProviderProbe.RecordingLoggerFactory Logs { get; } = new();

    // ReSharper disable once NullableWarningSuppressionIsUsed - xUnit constructs the fixture; the server starts in InitializeAsync.
    public WireMockServer Server { get; private set; } = null!;

    public string BaseUrl => Server.Urls[0];

    public int RequestCount => Server.LogEntries.Count();

    public string LastRequestBody =>
        Server.LogEntries.Last().RequestMessage?.Body
        ?? throw new InvalidOperationException("No request was recorded.");

    public async ValueTask InitializeAsync()
    {
        Server = LoopbackWireMock.Start();

        using var warmUp = await WarmUpClient.GetAsync(new Uri(BaseUrl), TestContext.Current.CancellationToken);
        Server.Reset();
    }

    public ValueTask DisposeAsync()
    {
        Server.Stop();
        Server.Dispose();
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    public void Reset() => Server.Reset();

    public static string Endpoint(string baseUrl) => baseUrl.TrimEnd('/') + "/";

    public static float[] Vector(int length = 384, float value = 0.5f) =>
        [.. Enumerable.Repeat(value, length)];

    public static float[] IndexedVector(int length = 384)
    {
        var vector = new float[length];
        for (var index = 0; index < length; index++)
        {
            vector[index] = index / 1000f;
        }

        return vector;
    }

    public static string EmbedBody(params float[][] vectors) =>
        JsonSerializer.Serialize(new
        {
            model = ModelName,
            embeddings = vectors,
        });

    public ServiceProvider BuildServices(
        string? endpoint = null,
        string model = ModelName,
        OllamaResilienceOptions? resilience = null,
        TimeProvider? clock = null,
        Action<IServiceCollection>? configure = null)
    {
        var services = NewServices(endpoint, model, resilience, clock, configure);
        services.AddOllamaEmbeddingGenerator();

        var provider = services.BuildServiceProvider();
        disposables.Add(provider);
        return provider;
    }

    public ServiceCollection NewServices(
        string? endpoint = null,
        string model = ModelName,
        OllamaResilienceOptions? resilience = null,
        TimeProvider? clock = null,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(Logs);
        services.AddSingleton<IOptions<OllamaOptions>>(Options.Create(new OllamaOptions
        {
            Endpoint = endpoint ?? Endpoint(BaseUrl),
            ModelName = model,
        }));
        services.AddSingleton<IOptions<OllamaResilienceOptions>>(Options.Create(resilience ?? TestResilience()));
        if (clock is not null)
        {
            services.AddSingleton(clock);
        }

        configure?.Invoke(services);
        return services;
    }

    public static OllamaResilienceOptions TestResilience() => new()
    {
        BaseDelay = TimeSpan.FromMilliseconds(10),
        AttemptTimeout = TimeSpan.FromMilliseconds(500),
        TotalTimeout = TimeSpan.FromSeconds(3),
        SamplingDuration = TimeSpan.FromSeconds(1),
        BreakDuration = TimeSpan.FromSeconds(1),
    };

    public void StubVectors(params float[][] vectors) =>
        StubBody(200, EmbedBody(vectors));

    public void StubBody(int statusCode, string body = "") =>
        Server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBody(body));

    public IReadOnlyList<string> RequestBodies =>
        [.. Server.LogEntries
            .Select(entry => entry.RequestMessage?.Body)
            .Where(body => body is not null)
            // ReSharper disable once NullableWarningSuppressionIsUsed - Where(is not null) above guarantees non-null.
            .Select(body => body!)];
}
