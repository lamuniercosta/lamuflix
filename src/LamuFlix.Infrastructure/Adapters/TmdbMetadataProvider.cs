using System;
using System.Collections.Generic;
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
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class TmdbMetadataProvider(
    HttpClient httpClient,
    IOptions<TmdbOptions> options,
    IOptions<OmdbOptions> matchOptions,
    TimeProvider timeProvider,
    ILogger<TmdbMetadataProvider> logger,
    SecondaryMetadataProviderHealthState healthState) : IMetadataProvider
{
    private const string NoStatus = "none";
    private const string MalformedBody = "malformed body";
    private const string UnusableBody = "unusable body";
    private const string MissingTitle = "missing or unusable title";
    private const string MissingMovieId = "missing or unusable movie id";
    private const int NoResultStatusCode = 21;

    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly TmdbOptions tmdbOptions = options.Value;
    private readonly double matchThreshold = matchOptions.Value.MatchThreshold;

    public async Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lookup);

        var started = timeProvider.GetTimestamp();

        // ReSharper disable once ExplicitCallerInfoArgument - the span name is a stable telemetry contract, not the caller method name.
        using var activity = Source.StartActivity(TelemetryConstants.MetadataLookup, ActivityKind.Client);
        try
        {
            var result = await ExecuteAsync(lookup, ct);
            Record(activity, result);
            return result;
        }
        finally
        {
            EnrichmentMetrics.EnrichmentDuration.Record(
                timeProvider.GetElapsedTime(started).TotalSeconds,
                new KeyValuePair<string, object?>(TelemetryConstants.ProviderTagName, TelemetryConstants.TmdbProviderName));
        }
    }

    private async Task<MetadataLookupResult> ExecuteAsync(MetadataLookup stored, CancellationToken ct)
    {
        if (!IsConfigured())
        {
            Log.NotConfigured(logger, tmdbOptions.Enabled);
            return new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable);
        }

        return stored.ManualImdbId is { } manualId
            ? await FindThenFetchAsync(manualId, ct)
            : await SearchThenFetchAsync(Recleaned(stored), ct);
    }

    private bool IsConfigured() =>
        tmdbOptions.Enabled && !string.IsNullOrWhiteSpace(tmdbOptions.ApiKey);

    private MetadataLookup Recleaned(MetadataLookup stored)
    {
        var cleaned = FolderNameCleaner.Clean(stored.Title, timeProvider.GetUtcNow());
        return (stored.ReleaseYear, cleaned.Year) switch
        {
            (null, { } year) => new MetadataLookup(cleaned.Title, year),
            (_, null) => stored with { Title = cleaned.Title },
            _ => stored,
        };
    }

    private async Task<MetadataLookupResult> SearchThenFetchAsync(MetadataLookup lookup, CancellationToken ct)
    {
        var fetch = await FetchAsync(BuildSearchUri(lookup), RequestKind.Search, ct);
        if (fetch is Fetch.Rejected rejected)
        {
            return rejected.Failure;
        }

        return ((Fetch.Succeeded)fetch).Body.Results is not { } hits
            ? UnusableBodyFailure()
            : await SelectAsync(lookup, hits, ct);
    }

    private async Task<MetadataLookupResult> SelectAsync(
        MetadataLookup lookup,
        IReadOnlyList<SearchHit> hits,
        CancellationToken ct) =>
        BestCandidate(lookup, hits) is { } selected
            ? await FetchDetailsAsync(selected, ct)
            : NotFoundOutcome();

    private Candidate? BestCandidate(MetadataLookup lookup, IReadOnlyList<SearchHit> hits)
    {
        Candidate? best = null;
        foreach (var hit in hits)
        {
            if (ScoreCandidate(lookup, hit) is not { } candidate || candidate.Score < matchThreshold)
            {
                continue;
            }

            if (best is null || IsBetter(candidate, best))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static bool IsBetter(Candidate candidate, Candidate current)
    {
        var comparison = candidate.Score.CompareTo(current.Score);
        return comparison > 0 || (comparison == 0 && HasLowerId(candidate.Id, current.Id));
    }

    private static bool HasLowerId(int? candidateId, int? currentId) =>
        candidateId is { } id && (currentId is not { } other || id < other);

    private static Candidate? ScoreCandidate(MetadataLookup lookup, SearchHit hit) =>
        hit.Title is null
            ? null
            : TitleMatcher.Score(
                lookup.Title,
                lookup.ReleaseYear?.Value,
                hit.Title,
                ResponseMapper.MapYear(hit.ReleaseDate)) is { } score
                ? new Candidate(hit.Id, score)
                : null;

    private async Task<MetadataLookupResult> FindThenFetchAsync(ImdbId imdbId, CancellationToken ct)
    {
        var fetch = await FetchAsync(BuildFindUri(imdbId), RequestKind.Find, ct);
        if (fetch is Fetch.Rejected rejected)
        {
            return rejected.Failure;
        }

        var body = ((Fetch.Succeeded)fetch).Body;
        if (ResponseMapper.IsNoResult(body) || body.MovieResults is { Count: 0 })
        {
            return NotFoundOutcome();
        }

        return body.MovieResults is { } results
            ? await FetchDetailsAsync(new Candidate(results[0].Id, Score: 0), ct)
            : UnusableBodyFailure();
    }

    private async Task<MetadataLookupResult> FetchDetailsAsync(Candidate candidate, CancellationToken ct)
    {
        if (candidate.Id is not { } movieId || movieId <= 0)
        {
            Log.InvalidResponse(logger, MissingMovieId);
            return Failure(EnrichmentFailureCategory.InvalidResponse);
        }

        var fetch = await FetchAsync(BuildDetailsUri(movieId), RequestKind.Details, ct);
        if (fetch is Fetch.Rejected rejected)
        {
            return rejected.Failure;
        }

        var body = ((Fetch.Succeeded)fetch).Body;
        if (ResponseMapper.IsNoResult(body))
        {
            return NotFoundOutcome();
        }

        var metadata = ResponseMapper.Map(body, timeProvider.GetUtcNow());
        if (metadata is null)
        {
            Log.InvalidResponse(logger, MissingTitle);
            return Failure(EnrichmentFailureCategory.InvalidResponse);
        }

        healthState.Record(category: null, wasUnauthorized: false);
        return new MetadataLookupResult.Found(metadata);
    }

    private async Task<Fetch> FetchAsync(Uri uri, RequestKind kind, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, ct);
            return await ReadAsync(response, kind, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new Fetch.Rejected(UnavailableFailure(statusCode: null, exception: null));
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            return new Fetch.Rejected(UnavailableFailure(statusCode: null, exception: exception));
        }
    }

    private async Task<Fetch> ReadAsync(HttpResponseMessage response, RequestKind kind, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new Fetch.Rejected(StatusFailure(response.StatusCode, kind));
        }

        Response? body;
        try
        {
            body = await DeserializeAsync(response, ct);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return new Fetch.Rejected(MalformedFailure());
        }

        return body is null
            ? new Fetch.Rejected(MalformedFailure())
            : new Fetch.Succeeded(body);
    }

    private MetadataLookupResult NotFoundOutcome()
    {
        healthState.Record(category: null, wasUnauthorized: false);
        return new MetadataLookupResult.NotFound();
    }

    private MetadataLookupResult UnusableBodyFailure()
    {
        Log.InvalidResponse(logger, UnusableBody);
        return Failure(EnrichmentFailureCategory.InvalidResponse);
    }

    private MetadataLookupResult MalformedFailure()
    {
        Log.InvalidResponse(logger, MalformedBody);
        return Failure(EnrichmentFailureCategory.InvalidResponse);
    }

    private MetadataLookupResult StatusFailure(HttpStatusCode statusCode, RequestKind kind)
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
            Log.ProviderUnavailable(
                logger,
                StatusText(statusCode),
                EnrichmentFailureCategory.ProviderUnavailable.Code,
                string.Empty);
            return Failure(EnrichmentFailureCategory.ProviderUnavailable);
        }

        if (statusCode == HttpStatusCode.NotFound && kind is not RequestKind.Search)
        {
            return NotFoundOutcome();
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

    private Uri BuildSearchUri(MetadataLookup lookup) =>
        ComposeUri(
            string.Concat(
                "search/movie",
                QueryKey(),
                "&query=",
                Uri.EscapeDataString(lookup.Title),
                lookup.ReleaseYear is { } year ? PrimaryReleaseYear(year) : string.Empty));

    private Uri BuildDetailsUri(int movieId) =>
        ComposeUri(string.Concat("movie/", movieId.ToString(CultureInfo.InvariantCulture), QueryKey()));

    private Uri BuildFindUri(ImdbId imdbId) =>
        ComposeUri(
            string.Concat("find/", Uri.EscapeDataString(imdbId.Value), QueryKey(), "&external_source=imdb_id"));

    private Uri ComposeUri(string relativePath) => new(httpClient.BaseAddress!, relativePath);

    private string QueryKey() =>
        string.Concat("?api_key=", Uri.EscapeDataString(tmdbOptions.ApiKey));

    private static string PrimaryReleaseYear(ReleaseYear year) =>
        string.Concat("&primary_release_year=", year.Value.ToString(CultureInfo.InvariantCulture));

    private enum RequestKind
    {
        Search,
        Details,
        Find,
    }

    private abstract record Fetch
    {
        private Fetch()
        {
        }

        public sealed record Succeeded(Response Body) : Fetch;

        public sealed record Rejected(MetadataLookupResult Failure) : Fetch;
    }

    private sealed record Candidate(int? Id, double Score);
}
