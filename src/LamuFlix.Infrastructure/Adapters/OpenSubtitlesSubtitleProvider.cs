using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Adapters;

public sealed class OpenSubtitlesSubtitleProvider(
    HttpClient httpClient,
    IOptions<SubtitleProviderOptions> optionsProvider,
    IOptions<SubtitlesOptions> subtitlesOptions,
    TimeProvider timeProvider) : ISubtitleProvider
{
    private const string SearchPath = "api/v1/subtitles";
    private const string DownloadPath = "api/v1/download";
    private const string ApiKeyHeader = "Api-Key";
    private const string UserAgentHeader = "User-Agent";
    private const string AcceptHeader = "Accept";
    private const string JsonMediaType = "application/json";
    private const string LanguagesParameter = "languages";
    private const string MovieHashParameter = "moviehash";
    private const string QueryParameter = "query";
    private const string TypeParameter = "type";
    private const string MovieType = "movie";
    private const char LanguageSeparator = ',';

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private SubtitleProviderOptions Options => optionsProvider.Value;

    private long MaxDownloadBytes => Math.Min(
        Options.MaxDownloadBytes > 0 ? Options.MaxDownloadBytes : SubtitlesOptions.DefaultMaxSubtitleDownloadBytes,
        subtitlesOptions.Value.MaxSubtitleDownloadBytes);

    public async Task<SubtitleSearchResult> SearchAsync(SubtitleSearch search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);

        var endpoint = EnsurePermitted(
            SubtitleRequestUriPolicy.TryBuildEndpoint(Options.BaseUrl, SearchPath),
            Options.DownloadAllowedHosts,
            "The configured subtitle provider search endpoint is not an allowed https host.");
        var searchEndpoint = BuildSearchUri(endpoint, search);

        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, searchEndpoint),
            cancellationToken);
        EnsureAccepted(response, "The subtitle provider rejected the search request.");
        var candidates = await ReadCandidatesAsync(response, search.MovieHash is not null, cancellationToken);
        return new SubtitleSearchResult(candidates, SubtitleQuota.Unknown);
    }

    public async Task<SubtitleDownload> DownloadAsync(SubtitleCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var endpoint = EnsurePermitted(
            SubtitleRequestUriPolicy.TryBuildEndpoint(Options.BaseUrl, DownloadPath),
            Options.DownloadAllowedHosts,
            "The configured subtitle provider download endpoint is not an allowed https host.");

        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new DownloadRequest(candidate.FileId), options: SerializerOptions),
            },
            cancellationToken);
        EnsureAccepted(response, "The subtitle provider rejected the download request.");

        var (linkCandidate, quota) = await ReadDownloadBodyAsync(response, cancellationToken);
        var link = EnsurePermitted(
            linkCandidate,
            Options.DownloadAllowedHosts,
            "The subtitle provider returned a download link that is not an allowed https host.");

        using var payload = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, link), cancellationToken);
        EnsureAccepted(payload, "The subtitle provider returned an unusable subtitle download.");
        return new SubtitleDownload(await ReadBoundedContentAsync(payload, cancellationToken), quota);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(Options.RequestTimeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using var request = requestFactory();
        request.Headers.TryAddWithoutValidation(UserAgentHeader, Options.UserAgent);
        if (IsProviderApiHost(request.RequestUri))
        {
            // The API host answers a download without Accept: application/json with a 503 HTML page.
            // The link host serves text/plain, so it gets neither header.
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, Options.ApiKey);
            request.Headers.TryAddWithoutValidation(AcceptHeader, JsonMediaType);
        }

        try
        {
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SubtitleProviderTimeoutException();
        }
        catch (HttpRequestException exception)
        {
            throw new SubtitleProviderFailureException("The subtitle provider could not be reached.", exception);
        }
    }

    private static async Task<IReadOnlyList<SubtitleCandidate>> ReadCandidatesAsync(
        HttpResponseMessage response,
        bool hashSearch,
        CancellationToken cancellationToken)
    {
        SearchResponse? body;
        try
        {
            body = await response.Content.ReadFromJsonAsync<SearchResponse>(SerializerOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new SubtitleProviderFailureException("The subtitle provider returned an unusable search response.", exception);
        }

        if (body?.Data is null)
        {
            throw new SubtitleProviderFailureException("The subtitle provider returned an unusable search response.");
        }

        return [.. body.Data
            .Select(entry => ToCandidate(entry, hashSearch))
            .OfType<SubtitleCandidate>()];
    }

    private static SubtitleCandidate? ToCandidate(SearchEntry? entry, bool hashSearch)
    {
        var attributes = entry?.Attributes;
        var fileId = attributes?.Files?.FirstOrDefault()?.FileId;
        if (attributes is null || string.IsNullOrWhiteSpace(attributes.Language) || fileId is null || fileId <= 0)
        {
            return null;
        }

        return new SubtitleCandidate(
            fileId.Value,
            attributes.Language,
            attributes.DownloadCount ?? 0,
            attributes.MovieHashMatch ?? hashSearch,
            ReleaseName(attributes));
    }

    private static string? ReleaseName(SearchAttributes attributes)
    {
        if (!string.IsNullOrWhiteSpace(attributes.Release))
        {
            return attributes.Release.Trim();
        }

        var fileName = attributes.Files?.FirstOrDefault()?.FileName;
        return string.IsNullOrWhiteSpace(fileName) ? null : Path.GetFileNameWithoutExtension(fileName.Trim());
    }

    private static async Task<(Uri Link, SubtitleQuota Quota)> ReadDownloadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        DownloadResponse? body;
        try
        {
            body = await response.Content.ReadFromJsonAsync<DownloadResponse>(SerializerOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new SubtitleProviderFailureException("The subtitle provider returned an unusable download response.", exception);
        }

        if (body?.Link is null || !Uri.TryCreate(body.Link, UriKind.Absolute, out var link))
        {
            throw new SubtitleProviderFailureException("The subtitle provider returned an unusable download link.");
        }

        return (link, DownloadQuota(body));
    }

    private static SubtitleQuota DownloadQuota(DownloadResponse body)
    {
        var remainingDownloads = ReadRemainingDownloads(body.Remaining);
        var resetAtUtc = ReadResetAtUtc(body.ResetTimeUtc);
        return remainingDownloads is null && resetAtUtc is null
            ? SubtitleQuota.Unknown
            : new SubtitleQuota(remainingDownloads, resetAtUtc);
    }

    private static int? ReadRemainingDownloads(JsonElement remaining) => remaining.ValueKind switch
    {
        JsonValueKind.Number when remaining.TryGetInt32(out var number) => number,
        JsonValueKind.String when int.TryParse(
            remaining.GetString(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed) => parsed,
        _ => null,
    };

    private static DateTimeOffset? ReadResetAtUtc(JsonElement resetTimeUtc) =>
        resetTimeUtc.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(
                resetTimeUtc.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var resetAt)
            ? resetAt
            : null;

    private async Task<byte[]> ReadBoundedContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var bound = MaxDownloadBytes;
        var declaredLength = response.Content.Headers.ContentLength;
        if (declaredLength is { } length && length > bound)
        {
            throw new SubtitleProviderFailureException("The downloaded subtitle exceeded the configured size bound.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[bound];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        if (total == buffer.Length && await HasMoreAsync(stream, cancellationToken))
        {
            throw new SubtitleProviderFailureException("The downloaded subtitle exceeded the configured size bound.");
        }

        var payload = buffer.AsSpan(0, total);
        if (!SubtitlePayloadValidator.IsAcceptable(payload, bound))
        {
            throw new SubtitleProviderFailureException("The downloaded subtitle is not a valid SRT document.");
        }

        return payload.ToArray();
    }

    private static async Task<bool> HasMoreAsync(Stream stream, CancellationToken cancellationToken)
    {
        var probe = new byte[1];
        return await stream.ReadAsync(probe, cancellationToken) > 0;
    }

    private void EnsureAccepted(HttpResponseMessage response, string message)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new SubtitleQuotaExceededException(RetryAfter(response));
        }

        if ((int)response.StatusCode is >= 300 and < 400)
        {
            throw new SubtitleProviderFailureException("The subtitle provider attempted to redirect the request.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new SubtitleProviderFailureException(message);
        }
    }

    private TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header?.Delta is { } delta && delta >= TimeSpan.Zero)
        {
            return delta;
        }

        if (header?.Date is { } date)
        {
            var delay = date - timeProvider.GetUtcNow();
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }

    private static Uri EnsurePermitted(Uri? uri, IReadOnlyList<string> allowedHosts, string message)
    {
        if (uri is null || !SubtitleRequestUriPolicy.IsPermitted(uri, allowedHosts))
        {
            throw new SubtitleProviderFailureException(message);
        }

        return uri;
    }

    private bool IsProviderApiHost(Uri? uri)
    {
        var api = SubtitleRequestUriPolicy.TryBuildEndpoint(Options.BaseUrl, SearchPath);
        return uri is not null
            && api is not null
            && string.Equals(uri.Authority, api.Authority, StringComparison.OrdinalIgnoreCase);
    }

    private static Uri BuildSearchUri(Uri endpoint, SubtitleSearch search)
    {
        var parameters = new List<(string Name, string Value)>();
        var languages = NormalizeLanguages(search.PreferredLanguages);
        if (languages.Count > 0)
        {
            parameters.Add((LanguagesParameter, string.Join(LanguageSeparator, languages.Select(EncodeValue))));
        }

        if (SearchTargetParameter(search) is { } target)
        {
            parameters.Add(target);
        }

        parameters.Add((TypeParameter, MovieType));

        var query = string.Join(
            '&',
            parameters
                .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
                .Select(parameter => string.Concat(parameter.Name, "=", parameter.Value)));
        return new UriBuilder(endpoint) { Query = query }.Uri;
    }

    private static (string Name, string Value)? SearchTargetParameter(SubtitleSearch search)
    {
        if (!string.IsNullOrWhiteSpace(search.MovieHash))
        {
            return (MovieHashParameter, EncodeValue(search.MovieHash.ToLowerInvariant()));
        }

        if (string.IsNullOrWhiteSpace(search.Title))
        {
            return null;
        }

        return (QueryParameter, EncodeValue(search.Title.ToLowerInvariant()));
    }

    private static string EncodeValue(string value) => WebUtility.UrlEncode(value);

    private static IReadOnlyList<string> NormalizeLanguages(IReadOnlyList<string> preferredLanguages) =>
        [.. preferredLanguages
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => language.Trim().ToLowerInvariant())];

    // ReSharper disable NotAccessedPositionalProperty.Local // Serialized by System.Text.Json as the OpenSubtitles request body, never read in code.
    private sealed record DownloadRequest([property: JsonPropertyName("file_id")] long FileId);
    // ReSharper restore NotAccessedPositionalProperty.Local

    private sealed record SearchResponse([property: JsonPropertyName("data")] SearchEntry[]? Data);

    private sealed record SearchEntry([property: JsonPropertyName("attributes")] SearchAttributes? Attributes);

    private sealed record SearchAttributes(
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("download_count")] int? DownloadCount,
        [property: JsonPropertyName("moviehash_match")] bool? MovieHashMatch,
        [property: JsonPropertyName("release")] string? Release,
        [property: JsonPropertyName("files")] SearchFile[]? Files);

    private sealed record SearchFile(
        [property: JsonPropertyName("file_id")] long? FileId,
        [property: JsonPropertyName("file_name")] string? FileName);

    private sealed record DownloadResponse(
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("remaining")] JsonElement Remaining,
        [property: JsonPropertyName("reset_time_utc")] JsonElement ResetTimeUtc);
}
