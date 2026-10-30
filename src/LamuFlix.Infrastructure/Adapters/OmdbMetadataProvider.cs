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
    private const string TooManyResultsError = "Too many results.";
    private const string NotAvailable = "N/A";
    private const int YearDigits = 4;

    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly OmdbOptions omdbOptions = options.Value;

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
                new KeyValuePair<string, object?>(TelemetryConstants.ProviderTagName, TelemetryConstants.OmdbProviderName));
        }
    }

    private async Task<MetadataLookupResult> ExecuteAsync(MetadataLookup stored, CancellationToken ct)
    {
        if (stored.ManualImdbId is { } manualId)
        {
            return await FetchByIdAsync(manualId, ct);
        }

        var lookup = Recleaned(stored);
        var direct = await FetchAsync(BuildUri(lookup), ct);
        if (direct is Fetch.Rejected rejected)
        {
            return rejected.Failure;
        }

        var body = ((Fetch.Succeeded)direct).Body;
        return ResponseMapper.IsMovieNotFound(body)
            ? await FallbackAsync(lookup, ct)
            : Classify(body);
    }

    /// <summary>
    /// Runs the stored title through the current <see cref="FolderNameCleaner"/>, so rows imported before
    /// a cleaning fix (release tags or an edition label left in the title) match without a re-import. A
    /// year the cleaner finds is used only when none is stored; when both exist the stored lookup wins,
    /// so a title such as "Death Race 2000" keeps its number.
    /// </summary>
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

    private async Task<MetadataLookupResult> FallbackAsync(MetadataLookup lookup, CancellationToken ct)
    {
        var candidates = new List<SearchHit>();
        foreach (var query in FallbackQueries(lookup))
        {
            var page = await SearchAsync(query, ct);
            if (page.Failure is { } failure)
            {
                return failure;
            }

            candidates.AddRange(page.Hits);
            if (BestCandidate(lookup, candidates) is { } match)
            {
                return await FetchMatchAsync(lookup, match, ct);
            }
        }

        return NotFoundOutcome();
    }

    private static IEnumerable<SearchQuery> FallbackQueries(MetadataLookup lookup)
    {
        var year = lookup.ReleaseYear?.Value;
        foreach (var title in TitleMatcher.SearchVariants(lookup.Title))
        {
            if (year is { } known)
            {
                yield return new SearchQuery(title, known);
            }

            yield return new SearchQuery(title, null);
        }
    }

    private async Task<SearchPage> SearchAsync(SearchQuery query, CancellationToken ct)
    {
        var fetch = await FetchAsync(BuildSearchUri(query), ct);
        if (fetch is Fetch.Rejected rejected)
        {
            return SearchPage.Failed(rejected.Failure);
        }

        var body = ((Fetch.Succeeded)fetch).Body;
        if (ResponseMapper.IsSearchExhausted(body))
        {
            return SearchPage.Empty;
        }

        return ResponseMapper.IsFound(body) && body.Search is { } hits
            ? new SearchPage(hits, Failure: null)
            : SearchPage.Failed(InvalidBodyOutcome());
    }

    private Candidate? BestCandidate(MetadataLookup lookup, IReadOnlyList<SearchHit> hits)
    {
        Candidate? best = null;
        foreach (var hit in hits)
        {
            var candidate = ScoreCandidate(lookup, hit);
            if (candidate is { } scored
                && scored.Score >= omdbOptions.MatchThreshold
                && (best is null || scored.Score > best.Score))
            {
                best = scored;
            }
        }

        return best;
    }

    private static Candidate? ScoreCandidate(MetadataLookup lookup, SearchHit hit)
    {
        if (hit.Title is null || !ImdbId.TryCreate(hit.Identifier, out var id))
        {
            return null;
        }

        var score = TitleMatcher.Score(
            lookup.Title,
            lookup.ReleaseYear?.Value,
            hit.Title,
            ResponseMapper.MapSearchYear(hit.Year));
        return score is { } value ? new Candidate(hit.Title, id, value) : null;
    }

    private async Task<MetadataLookupResult> FetchMatchAsync(MetadataLookup lookup, Candidate candidate, CancellationToken ct)
    {
        Log.FallbackMatched(logger, lookup.Title, candidate.Title, candidate.Id.Value, Math.Round(candidate.Score, 3));
        return await FetchByIdAsync(candidate.Id, ct);
    }

    private async Task<MetadataLookupResult> FetchByIdAsync(ImdbId id, CancellationToken ct)
    {
        var fetch = await FetchAsync(BuildIdUri(id), ct);
        return fetch is Fetch.Rejected rejected
            ? rejected.Failure
            : Classify(((Fetch.Succeeded)fetch).Body);
    }

    private async Task<Fetch> FetchAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, ct);
            return await ReadAsync(response, ct);
        }
        catch (MetadataQuotaExhaustedException)
        {
            Log.QuotaExhausted(logger);
            return new Fetch.Rejected(QuotaFailure(EnrichmentFailureCategory.RateLimited));
        }
        catch (MetadataQuotaPersistenceException exception)
        {
            Log.QuotaPersistenceFailed(logger, InnerFailureType(exception));
            return new Fetch.Rejected(QuotaFailure(EnrichmentFailureCategory.ProviderUnavailable));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new Fetch.Rejected(UnavailableFailure(statusCode: null, exception: null));
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            return new Fetch.Rejected(UnavailableFailure(statusCode: null, exception: exception));
        }
    }

    private async Task<Fetch> ReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new Fetch.Rejected(StatusFailure(response.StatusCode));
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

    private MetadataLookupResult Classify(Response body)
    {
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

    private MetadataLookupResult MalformedFailure()
    {
        Log.InvalidResponse(logger, MalformedBody);
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

    private static MetadataLookupResult QuotaFailure(EnrichmentFailureCategory category) =>
        new MetadataLookupResult.Failed(category);

    private static string InnerFailureType(MetadataQuotaPersistenceException exception) =>
        exception.InnerException is { } inner ? inner.GetType().Name : exception.GetType().Name;

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

        return ComposeUri(
            string.Concat(
                "&t=",
                Uri.EscapeDataString(lookup.Title),
                "&type=movie",
                "&plot=full",
                year));
    }

    private Uri BuildSearchUri(SearchQuery query)
    {
        var year = query.Year is { } known
            ? "&y=" + known.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        return ComposeUri(
            string.Concat(
                "&s=",
                Uri.EscapeDataString(query.Title),
                "&type=movie",
                year));
    }

    private Uri BuildIdUri(ImdbId id) =>
        ComposeUri(string.Concat("&i=", Uri.EscapeDataString(id.Value), "&plot=full"));

    private Uri ComposeUri(string parameters) =>
        new(
            new Uri(omdbOptions.BaseUrl),
            string.Concat("?apikey=", Uri.EscapeDataString(omdbOptions.ApiKey), parameters));

    private abstract record Fetch
    {
        private Fetch()
        {
        }

        public sealed record Succeeded(Response Body) : Fetch;

        public sealed record Rejected(MetadataLookupResult Failure) : Fetch;
    }

    private sealed record SearchQuery(string Title, int? Year);

    private sealed record SearchPage(IReadOnlyList<SearchHit> Hits, MetadataLookupResult? Failure)
    {
        public static SearchPage Empty { get; } = new([], Failure: null);

        public static SearchPage Failed(MetadataLookupResult failure) => new([], failure);
    }

    private sealed record Candidate(string Title, ImdbId Id, double Score);
}
