using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OllamaSharpEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const string ProviderName = "ollama";

    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);

    private static readonly Meter ApplicationMeter = new(TelemetryConstants.ActivitySourceName);

    private static readonly Histogram<double> EmbeddingDuration =
        ApplicationMeter.CreateHistogram<double>(
            TelemetryConstants.EmbeddingDurationMetricName,
            unit: TelemetryConstants.EmbeddingDurationUnit);

    private static readonly Counter<long> EmbeddingOutcome =
        ApplicationMeter.CreateCounter<long>(TelemetryConstants.EmbeddingOutcomeMetricName);

    private readonly OllamaApiClient client;
    private readonly OllamaEmbeddingModelState modelState;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OllamaSharpEmbeddingGenerator> logger;
    private readonly Uri endpoint;
    private readonly string model;

    public OllamaSharpEmbeddingGenerator(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        OllamaEmbeddingModelState modelState,
        TimeProvider timeProvider,
        ILogger<OllamaSharpEmbeddingGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        this.modelState = modelState;
        this.timeProvider = timeProvider;
        this.logger = logger;
        model = options.Value.ModelName;
        endpoint = new Uri(options.Value.Endpoint);
        client = new OllamaApiClient(httpClient, model);
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var inputs = Guard(values, options);
        if (inputs.Count == 0)
        {
            return [];
        }

        var started = timeProvider.GetTimestamp();
        var outcome = TelemetryConstants.FailedOutcome;
        EnrichmentFailureCategory? category = null;

        // ReSharper disable once ExplicitCallerInfoArgument - the span name is a stable telemetry contract.
        using var activity = Source.StartActivity(TelemetryConstants.EmbeddingGenerate, ActivityKind.Client);
        try
        {
            var response = await RequestAsync(inputs, cancellationToken);
            var embeddings = Validate(response, inputs.Count);
            modelState.RecordSuccess();
            outcome = TelemetryConstants.SucceededOutcome;
            return embeddings;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = TelemetryConstants.CancelledOutcome;
            throw;
        }
        catch (EmbeddingGenerationException failure)
        {
            category = failure.Category;
            modelState.RecordFailure(failure.Category);
            Log.Failed(logger, SafeEndpoint(endpoint), model, failure.Category.Code);
            throw;
        }
        finally
        {
            RecordTelemetry(activity, timeProvider.GetElapsedTime(started), outcome, category);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is null && serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return new EmbeddingGeneratorMetadata(
                ProviderName,
                endpoint,
                model,
                modelState.VerifiedDimensions(endpoint, model));
        }

        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose() => client.Dispose();

    internal static string SafeEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return string.Concat(
            endpoint.Scheme,
            "://",
            endpoint.Host,
            ":",
            endpoint.Port.ToString(CultureInfo.InvariantCulture));
    }

    private IReadOnlyList<string> Guard(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (options?.ModelId is { } modelId && !string.Equals(modelId, model, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The requested embedding model does not match the configured model.",
                nameof(options));
        }

        if (options?.Dimensions is { } dimensions && dimensions != EmbeddingDimensions.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                dimensions,
                $"Only the default dimension of {EmbeddingDimensions.Value} is supported.");
        }

        var inputs = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Embedding inputs must be non-null and non-blank.",
                    nameof(values));
            }

            inputs.Add(value);
        }

        return inputs;
    }

    private async Task<EmbedResponse> RequestAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        try
        {
            return await client.EmbedAsync(
                new EmbedRequest
                {
                    Model = model,
                    Input = [.. inputs],
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsMapped(exception))
        {
            throw Map(exception);
        }
    }

    private GeneratedEmbeddings<Embedding<float>> Validate(EmbedResponse? response, int expectedCount)
    {
        var vectors = response?.Embeddings;
        if (vectors is null || vectors.Count != expectedCount)
        {
            throw InvalidResponse(
                $"returned {vectors?.Count.ToString(CultureInfo.InvariantCulture) ?? "no"} embeddings for {expectedCount.ToString(CultureInfo.InvariantCulture)} inputs");
        }

        var embeddings = new GeneratedEmbeddings<Embedding<float>>(vectors.Count);
        foreach (var vector in vectors)
        {
            if (vector is null || vector.Length != EmbeddingDimensions.Value)
            {
                throw InvalidResponse(
                    $"expected {EmbeddingDimensions.Value.ToString(CultureInfo.InvariantCulture)} dimensions and observed {(vector is null ? "no vector" : vector.Length.ToString(CultureInfo.InvariantCulture))}");
            }

            if (!vector.All(float.IsFinite))
            {
                throw InvalidResponse("the returned vector contained a non-finite value");
            }

            embeddings.Add(new Embedding<float>(vector));
        }

        return embeddings;
    }

    private static bool IsMapped(Exception exception) =>
        exception is HttpRequestException
            or TimeoutRejectedException
            or BrokenCircuitException
            or OllamaException
            or JsonException
            or NotSupportedException
            or OperationCanceledException;

    private EmbeddingGenerationException Map(Exception exception) => exception switch
    {
        HttpRequestException httpRequest => FromStatus(httpRequest.StatusCode),
        TimeoutRejectedException or BrokenCircuitException or OperationCanceledException => Unavailable(),
        _ => InvalidResponse("returned an unusable provider payload"),
    };

    private EmbeddingGenerationException FromStatus(HttpStatusCode? statusCode) => statusCode switch
    {
        HttpStatusCode.TooManyRequests => RateLimited(),
        null or HttpStatusCode.RequestTimeout => Unavailable(),
        { } status when (int)status >= 500 => Unavailable(),
        _ => InvalidResponse("returned a non-success response"),
    };

    private EmbeddingGenerationException Unavailable() =>
        Failure(EnrichmentFailureCategory.ProviderUnavailable, "is unavailable");

    private EmbeddingGenerationException RateLimited() =>
        Failure(EnrichmentFailureCategory.RateLimited, "is rate limiting this library");

    private EmbeddingGenerationException InvalidResponse(string detail) =>
        Failure(EnrichmentFailureCategory.InvalidResponse, detail);

    private EmbeddingGenerationException Failure(EnrichmentFailureCategory category, string detail) =>
        new(
            category,
            string.Concat(
                "The embedding provider at ",
                SafeEndpoint(endpoint),
                " with model '",
                model,
                "' ",
                detail,
                " (",
                category.Code,
                ")."));

    private static void RecordTelemetry(
        Activity? activity,
        TimeSpan elapsed,
        string outcome,
        EnrichmentFailureCategory? category)
    {
        var outcomeTag = new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, outcome);
        if (activity is not null)
        {
            activity.SetTag(TelemetryConstants.OutcomeTagName, outcome);
            if (category is not null)
            {
                activity.SetTag(TelemetryConstants.ErrorType, category.Code);
                activity.SetStatus(ActivityStatusCode.Error);
            }
        }

        if (category is null)
        {
            EmbeddingDuration.Record(elapsed.TotalSeconds, outcomeTag);
            EmbeddingOutcome.Add(1, outcomeTag);
            return;
        }

        var categoryTag = new KeyValuePair<string, object?>(
            TelemetryConstants.FailureCategoryTagName,
            category.Code);
        EmbeddingDuration.Record(elapsed.TotalSeconds, outcomeTag, categoryTag);
        EmbeddingOutcome.Add(1, outcomeTag, categoryTag);
    }
}
