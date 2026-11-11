using System;
using System.Collections.Generic;
using System.Globalization;
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
    TimeProvider timeProvider) : ISubtitleProvider
{
    private const string SearchPath = "api/v1/subtitles";
    private const string DownloadPath = "api/v1/download";
    private const string ApiKeyHeader = "Api-Key";
    private const string UserAgentHeader = "User-Agent";
    private const char LanguageSeparator = ',';

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private SubtitleProviderOptions Options => optionsProvider.Value;

    public async Task<IReadOnlyList<SubtitleCandidate>> SearchAsync(SubtitleSearch search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);

        var endpoint = SubtitleRequestUriPolicy.TryBuildEndpoint(Options.BaseUrl, SearchPath);
        EnsurePermitted(
            endpoint,
            Options.DownloadAllowedHosts,
            "The configured subtitle provider search endpoint is not an allowed https host.");
        var request = new SearchRequest(
            string.IsNullOrWhiteSpace(search.MovieHash) ? null : search.MovieHash,
            string.IsNullOrWhiteSpace(search.MovieHash) ? search.Title : null,
            JoinLanguages(search.PreferredLanguages));

        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(request, options: SerializerOptions) },
            cancellationToken);
        EnsureAccepted(response, "The subtitle provider rejected the search request.");
        return await ReadCandidatesAsync(response, search.MovieHash is not null, cancellationToken);
    }

    public async Task<byte[]> DownloadAsync(SubtitleCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var endpoint = SubtitleRequestUriPolicy.TryBuildEndpoint(Options.BaseUrl, DownloadPath);
        EnsurePermitted(
            endpoint,
            Options.DownloadAllowedHosts,
            "The configured subtitle provider download endpoint is not an allowed https host.");

        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new DownloadRequest(candidate.FileId), options: SerializerOptions),
            },
            cancellationToken);
        EnsureAccepted(response, "The subtitle provider rejected the download request.");

        var link = await ReadDownloadLinkAsync(response, cancellationToken);
        EnsurePermitted(
            link,
            Options.DownloadAllowedHosts,
            "The subtitle provider returned a download link that is not an allowed https host.");

        using var payload = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, link), cancellationToken);
        EnsureAccepted(payload, "The subtitle provider returned an unusable subtitle download.");
        return await ReadBoundedContentAsync(payload, cancellationToken);
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
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, Options.ApiKey);
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
            attributes.MovieHashMatch ?? hashSearch);
    }

    private static async Task<Uri> ReadDownloadLinkAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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

        return link;
    }

    private async Task<byte[]> ReadBoundedContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var declaredLength = response.Content.Headers.ContentLength;
        if (declaredLength is { } length && length > Options.MaxDownloadBytes)
        {
            throw new SubtitleProviderFailureException("The downloaded subtitle exceeded the configured size bound.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[Options.MaxDownloadBytes];
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
        if (!SubtitlePayloadValidator.IsAcceptable(payload, Options.MaxDownloadBytes))
        {
            throw new SubtitleProviderFailureException("The downloaded subtitle is not a valid SRT document.");
        }

        return payload.ToArray();
    }

    private static async Task<bool> HasMoreAsync(System.IO.Stream stream, CancellationToken cancellationToken)
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

    private static void EnsurePermitted(Uri? uri, IReadOnlyList<string> allowedHosts, string message)
    {
        if (!SubtitleRequestUriPolicy.IsPermitted(uri, allowedHosts))
        {
            throw new SubtitleProviderFailureException(message);
        }
    }

    private bool IsProviderApiHost(Uri? uri)
    {
        var api = SubtitleRequestUriPolicy.TryBuildEndpoint(Options.BaseUrl, SearchPath);
        return uri is not null
            && api is not null
            && string.Equals(uri.Authority, api.Authority, StringComparison.OrdinalIgnoreCase);
    }

    private static string JoinLanguages(IReadOnlyList<string> preferredLanguages) =>
        string.Join(
            LanguageSeparator,
            preferredLanguages
                .Where(language => !string.IsNullOrWhiteSpace(language))
                .Select(language => language.Trim().ToLowerInvariant()));

    private sealed record SearchRequest(
        [property: JsonPropertyName("moviehash")] string? MovieHash,
        [property: JsonPropertyName("query")] string? Query,
        [property: JsonPropertyName("languages")] string Languages);

    private sealed record DownloadRequest([property: JsonPropertyName("file_id")] long FileId);

    private sealed record SearchResponse([property: JsonPropertyName("data")] SearchEntry[]? Data);

    private sealed record SearchEntry([property: JsonPropertyName("attributes")] SearchAttributes? Attributes);

    private sealed record SearchAttributes(
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("download_count")] int? DownloadCount,
        [property: JsonPropertyName("moviehash_match")] bool? MovieHashMatch,
        [property: JsonPropertyName("files")] SearchFile[]? Files);

    private sealed record SearchFile([property: JsonPropertyName("file_id")] long? FileId);

    private sealed record DownloadResponse([property: JsonPropertyName("link")] string? Link);
}
