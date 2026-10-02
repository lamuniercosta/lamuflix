using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OmdbMetadataProvider(
    HttpClient httpClient,
    IOptions<OmdbOptions> options,
    TimeProvider timeProvider,
    ILogger<OmdbMetadataProvider> logger,
    MetadataProviderHealthState healthState) : IMetadataProvider
{
    private const string NoStatus = "none";
    private const string MalformedBody = "malformed body";
    private const string UnusableBody = "unusable body";
    private const string MissingTitle = "missing or unusable title";
    private const string FoundFlag = "True";
    private const string NotFoundFlag = "False";
    private const string MovieNotFoundError = "Movie not found!";
    private const string NotAvailable = "N/A";
    private const int YearDigits = 4;

    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly OmdbOptions omdbOptions = options.Value;

    public async Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lookup);

        // ReSharper disable once ExplicitCallerInfoArgument - the span name is a stable telemetry contract, not the caller method name.
        using var activity = Source.StartActivity(TelemetryConstants.MetadataLookup, ActivityKind.Client);
        var result = await ExecuteAsync(lookup, ct);
        Record(activity, result);
        return result;
    }

    private async Task<MetadataLookupResult> ExecuteAsync(MetadataLookup lookup, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(BuildUri(lookup), ct);
            return await InterpretAsync(response, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return UnavailableFailure(statusCode: null, exception: null);
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            return UnavailableFailure(statusCode: null, exception: exception);
        }
    }

    private async Task<MetadataLookupResult> InterpretAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return StatusFailure(response.StatusCode);
        }

        return await BodyOutcomeAsync(response, ct);
    }

    private async Task<MetadataLookupResult> BodyOutcomeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        Response? body;
        try
        {
            body = await DeserializeAsync(response, ct);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            Log.InvalidResponse(logger, MalformedBody);
            return Failure(EnrichmentFailureCategory.InvalidResponse);
        }

        return Classify(body);
    }

    private MetadataLookupResult Classify(Response? body)
    {
        if (body is null)
        {
            Log.InvalidResponse(logger, MalformedBody);
            return Failure(EnrichmentFailureCategory.InvalidResponse);
        }

        if (ResponseMapper.IsFound(body))
        {
            return FoundOutcome(body);
        }

        return ResponseMapper.IsMovieNotFound(body) ? NotFoundOutcome() : InvalidBodyOutcome();
    }

    private MetadataLookupResult FoundOutcome(Response body)
    {
        var metadata = ResponseMapper.Map(body, timeProvider.GetUtcNow());
        if (metadata is null)
        {
            Log.InvalidResponse(logger, MissingTitle);
            return Failure(EnrichmentFailureCategory.InvalidResponse);
        }

        healthState.Record(category: null, wasUnauthorized: false);
        return new MetadataLookupResult.Found(metadata);
    }

    private MetadataLookupResult NotFoundOutcome()
    {
        healthState.Record(category: null, wasUnauthorized: false);
        return new MetadataLookupResult.NotFound();
    }

    private MetadataLookupResult InvalidBodyOutcome()
    {
        Log.InvalidResponse(logger, UnusableBody);
        return Failure(EnrichmentFailureCategory.InvalidResponse);
    }

    private MetadataLookupResult StatusFailure(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.Unauthorized)
        {
            return UnauthorizedFailure();
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            Log.RateLimited(logger, StatusText(statusCode));
            return Failure(EnrichmentFailureCategory.RateLimited);
        }

        if (IsTransient(statusCode))
        {
            Log.ProviderUnavailable(logger, StatusText(statusCode), EnrichmentFailureCategory.ProviderUnavailable.Code, string.Empty);
            return Failure(EnrichmentFailureCategory.ProviderUnavailable);
        }

        Log.InvalidResponse(logger, StatusText(statusCode));
        return Failure(EnrichmentFailureCategory.InvalidResponse);
    }

    private MetadataLookupResult UnauthorizedFailure()
    {
        Log.InvalidApiKey(logger);
        healthState.Record(EnrichmentFailureCategory.InvalidResponse, wasUnauthorized: true);
        return new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse);
    }

    private MetadataLookupResult UnavailableFailure(HttpStatusCode? statusCode, Exception? exception)
    {
        Log.ProviderUnavailable(
            logger,
            StatusText(statusCode),
            EnrichmentFailureCategory.ProviderUnavailable.Code,
            exception is null ? string.Empty : exception.GetType().Name);
        return Failure(EnrichmentFailureCategory.ProviderUnavailable);
    }

    private MetadataLookupResult Failure(EnrichmentFailureCategory category)
    {
        healthState.Record(category, wasUnauthorized: false);
        return new MetadataLookupResult.Failed(category);
    }

    private static async Task<Response?> DeserializeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var payload = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<Response>(payload, SerializerOptions);
    }

    private static void Record(Activity? activity, MetadataLookupResult result)
    {
        if (activity is null || result is not MetadataLookupResult.Failed failed)
        {
            return;
        }

        activity.SetTag(TelemetryConstants.ErrorType, failed.Category.Code);
        activity.SetStatus(ActivityStatusCode.Error);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500;

    private static string StatusText(HttpStatusCode? statusCode) =>
        statusCode is null
            ? NoStatus
            : ((int)statusCode.Value).ToString(CultureInfo.InvariantCulture);

    private Uri BuildUri(MetadataLookup lookup)
    {
        var year = lookup.ReleaseYear is null
            ? string.Empty
            : "&y=" + Uri.EscapeDataString(lookup.ReleaseYear.Value.ToString(CultureInfo.InvariantCulture));

        var query = string.Concat(
            "?apikey=",
            Uri.EscapeDataString(omdbOptions.ApiKey),
            "&t=",
            Uri.EscapeDataString(lookup.Title),
            "&type=movie",
            year);

        return new Uri(new Uri(omdbOptions.BaseUrl), query);
    }
}