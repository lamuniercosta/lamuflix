using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WireMock;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace LamuFlix.IntegrationTests;

public sealed class OpenSubtitlesEmulator : IDisposable
{
    public const string SearchPath = "/api/v1/subtitles";
    public const string DownloadPath = "/api/v1/download";

    private const string UserAgentHeader = "User-Agent";
    private const string ApiKeyHeader = "Api-Key";
    private const string AcceptHeader = "Accept";
    private const string JsonMediaType = "application/json";
    private const string MovieType = "movie";
    private const string MovieFeatureType = "Movie";
    private const string SearchPathPattern = "^/api/v1/subtitles/?$";
    private const string LinkPathPattern = "^/download/[^/]+/subfile/[^/]+$";
    private const int DefaultPriority = 100;
    private const int PerPage = 50;

    private const string MovedPermanentlyHtml =
        "<!DOCTYPE html><html><head><title>301 Moved Permanently</title></head><body><h1>301 Moved Permanently</h1></body></html>";

    private const string ServiceUnavailableHtml =
        "<!DOCTYPE html><html><head><title>Error 503 - OpenSubtitles.com</title></head><body><h1>Error 503</h1></body></html>";

    private static readonly (string Name, string Value)[] RateLimitHeaders =
    [
        ("RateLimit-Limit", "5"),
        ("RateLimit-Remaining", "4"),
        ("RateLimit-Reset", "1"),
        ("X-RateLimit-Limit-Second", "5"),
        ("X-RateLimit-Remaining-Second", "4"),
    ];

    private readonly string apiKey;
    private readonly OpenSubtitlesScenario scenario;
    private int overridePriority;

    public OpenSubtitlesEmulator(string apiKey, OpenSubtitlesScenario? scenario = null)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        this.apiKey = apiKey;
        this.scenario = scenario ?? new OpenSubtitlesScenario();
        Api = LoopbackWireMock.StartHttps();
        Links = LoopbackWireMock.StartHttps();
        RegisterApiRules();
        RegisterLinkRules();
    }

    public WireMockServer Api { get; }

    public WireMockServer Links { get; }

    public string BaseUrl => Api.Urls[0];

    public string DownloadLinkFor(int fileId) => LinkFor(fileId);

    public static string CanonicalQuery(string rawQuery)
    {
        ArgumentNullException.ThrowIfNull(rawQuery);
        return string.Join(
            '&',
            rawQuery
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(CanonicalPair)
                .OrderBy(pair => pair.Name, StringComparer.Ordinal)
                .Select(pair => pair.Text));
    }

    public void OverrideSearch(IResponseBuilder response) =>
        AddOverride(Api, Request.Create().WithPath(new RegexMatcher(SearchPathPattern)).UsingGet(), response);

    public void OverrideDownload(IResponseBuilder response) =>
        AddOverride(Api, Request.Create().WithPath(DownloadPath).UsingPost(), response);

    public void OverrideLink(IResponseBuilder response) =>
        AddOverride(Links, Request.Create().WithPath(new RegexMatcher(LinkPathPattern)).UsingGet(), response);

    public void Dispose()
    {
        Api.Stop();
        Api.Dispose();
        Links.Stop();
        Links.Dispose();
    }

    private void RegisterApiRules()
    {
        Api.Given(Request.Create().WithPath(new RegexMatcher(SearchPathPattern)).UsingGet())
            .AtPriority(DefaultPriority)
            .RespondWith(Response.Create().WithCallback(HandleSearch));
        Api.Given(Request.Create().WithPath(new RegexMatcher(SearchPathPattern)).UsingPost())
            .AtPriority(DefaultPriority)
            .RespondWith(Response.Create().WithCallback(HandleCreateSubtitle));
        Api.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .AtPriority(DefaultPriority)
            .RespondWith(Response.Create().WithCallback(HandleDownload));
    }

    private void RegisterLinkRules() =>
        Links.Given(Request.Create().WithPath(new RegexMatcher(LinkPathPattern)).UsingGet())
            .AtPriority(DefaultPriority)
            .RespondWith(SrtResponse());

    private void AddOverride(WireMockServer server, IRequestBuilder request, IResponseBuilder response)
    {
        overridePriority--;
        server.Given(request).AtPriority(overridePriority).RespondWith(response);
    }

    private ResponseMessage HandleSearch(IRequestMessage request)
    {
        var rawQuery = RawQueryOf(request);
        var canonical = CanonicalQuery(rawQuery);
        if (!string.Equals(rawQuery, canonical, StringComparison.Ordinal))
        {
            return Redirect(canonical);
        }

        return Gate(request) ?? SearchOk(rawQuery);
    }

    private ResponseMessage HandleCreateSubtitle(IRequestMessage request) =>
        Gate(request) ?? Json(
            404,
            new JsonObject { ["status"] = 404, ["error"] = "Not Found" },
            rateLimited: true);

    private ResponseMessage HandleDownload(IRequestMessage request) =>
        Gate(request) ?? RequireJsonAccept(request) ?? (ReadFileId(request.Body) is { } fileId
            ? Json(200, DownloadBody(fileId), rateLimited: true)
            : Json(400, new JsonObject { ["message"] = "file_id is required" }, rateLimited: false));

    private static ResponseMessage? RequireJsonAccept(IRequestMessage request) =>
        AcceptsJson(request)
            ? null
            : Message(503, [("Content-Type", "text/html"), ("Server", "cloudflare")], ServiceUnavailableHtml);

    private static bool AcceptsJson(IRequestMessage request) =>
        request.Headers?.Any(header =>
            string.Equals(header.Key, AcceptHeader, StringComparison.OrdinalIgnoreCase)
            && header.Value.Any(value => value.Contains(JsonMediaType, StringComparison.OrdinalIgnoreCase))) == true;

    private ResponseMessage? Gate(IRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(HeaderValue(request, UserAgentHeader)))
        {
            return Json(403, new JsonObject { ["error"] = "User agent required" }, rateLimited: false);
        }

        if (!string.Equals(HeaderValue(request, ApiKeyHeader), apiKey, StringComparison.Ordinal))
        {
            return Json(
                403,
                new JsonObject { ["message"] = "You cannot consume this service" },
                rateLimited: false);
        }

        return null;
    }

    private ResponseMessage SearchOk(string rawQuery)
    {
        var typeFilter = QueryValue(rawQuery, "type");
        var hashSearch = QueryValue(rawQuery, "moviehash") is not null;
        var entries = scenario.Subtitles
            .Where(subtitle => !string.Equals(typeFilter, MovieType, StringComparison.Ordinal)
                || subtitle.FeatureType == MovieFeatureType)
            .ToArray();
        return Json(200, SearchBody(entries, hashSearch), rateLimited: true);
    }

    private JsonObject SearchBody(IReadOnlyList<OpenSubtitlesSubtitle> entries, bool hashSearch) => new()
    {
        ["total_pages"] = entries.Count == 0 ? 0 : 1,
        ["total_count"] = entries.Count,
        ["per_page"] = PerPage,
        ["page"] = 1,
        ["data"] = new JsonArray(entries.Select(entry => (JsonNode)SearchEntry(entry, hashSearch)).ToArray()),
    };

    private JsonObject SearchEntry(OpenSubtitlesSubtitle subtitle, bool hashSearch)
    {
        var fileId = subtitle.FileId.ToString(CultureInfo.InvariantCulture);
        var attributes = new JsonObject
        {
            ["subtitle_id"] = fileId,
            ["language"] = subtitle.Language,
            ["download_count"] = subtitle.DownloadCount,
            ["hd"] = false,
            ["fps"] = 23.976,
            ["release"] = subtitle.Release,
            ["feature_details"] = new JsonObject
            {
                ["feature_type"] = subtitle.FeatureType,
                ["title"] = subtitle.FeatureTitle,
                ["year"] = 2021,
            },
            ["url"] = $"https://www.opensubtitles.com/subtitles/{fileId}",
            ["files"] = new JsonArray(new JsonObject
            {
                ["file_id"] = subtitle.FileId,
                ["cd_number"] = 1,
                ["file_name"] = SubfileName(subtitle.FileId),
            }),
        };
        if (hashSearch)
        {
            attributes["moviehash_match"] = subtitle.MovieHashMatch;
        }

        return new JsonObject
        {
            ["id"] = fileId,
            ["type"] = "subtitle",
            ["attributes"] = attributes,
        };
    }

    private JsonObject DownloadBody(long fileId) => new()
    {
        ["link"] = LinkFor(fileId),
        ["file_name"] = SubfileName(fileId),
        ["requests"] = 1,
        ["remaining"] = scenario.DownloadsRemaining,
        ["message"] = "Placeholder quota sentence; the wording is not recorded.",
        ["reset_time"] = "17 hours and 9 minutes",
        ["reset_time_utc"] = scenario.ResetTimeUtc.UtcDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture),
        ["uk"] = "x",
        ["uid"] = 1,
        ["ts"] = 1,
    };

    private IResponseBuilder SrtResponse() => Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "text/plain")
        .WithHeader("Content-Disposition", "attachment")
        .WithBody(Encoding.UTF8.GetBytes(scenario.SrtText));

    private string LinkFor(long fileId) =>
        $"{Links.Urls[0]}/download/{Token(fileId)}/subfile/{Uri.EscapeDataString(SubfileName(fileId))}";

    private static string Token(long fileId) => $"token-{fileId.ToString(CultureInfo.InvariantCulture)}";

    private string SubfileName(long fileId) =>
        scenario.Subtitles.FirstOrDefault(subtitle => subtitle.FileId == fileId) is { } match
            ? $"{match.Release}.srt"
            : $"subtitle-{fileId.ToString(CultureInfo.InvariantCulture)}.srt";

    private static ResponseMessage Redirect(string canonical) => Message(
        301,
        [
            ("Location", $"{SearchPath}?{canonical}"),
            ("X-OS-Rule", "canonical"),
            ("X-Canonical", "1"),
            ("Content-Type", "text/html"),
        ],
        MovedPermanentlyHtml);

    private static ResponseMessage Json(int statusCode, JsonObject body, bool rateLimited)
    {
        var headers = new List<(string Name, string Value)> { ("Content-Type", "application/json; charset=utf-8") };
        if (rateLimited)
        {
            headers.AddRange(RateLimitHeaders);
        }

        return Message(statusCode, headers, body.ToJsonString());
    }

    private static ResponseMessage Message(int statusCode, IEnumerable<(string Name, string Value)> headers, string body)
    {
        var dictionary = new Dictionary<string, WireMockList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in headers)
        {
            dictionary[name] = new WireMockList<string>(value);
        }

        var bytes = Encoding.UTF8.GetBytes(body);
        return new ResponseMessage
        {
            StatusCode = statusCode,
            Headers = dictionary,
            BodyData = new BodyData
            {
                DetectedBodyType = BodyType.Bytes,
                BodyAsBytes = bytes,
                Encoding = Encoding.UTF8,
            },
        };
    }

    private static (string Name, string Text) CanonicalPair(string pair)
    {
        var separator = pair.IndexOf('=', StringComparison.Ordinal);
        if (separator < 0)
        {
            var bareName = pair.ToLowerInvariant();
            return (bareName, bareName);
        }

        var name = pair[..separator].ToLowerInvariant();
        return (name, string.Concat(name, "=", CanonicalValue(pair[(separator + 1)..])));
    }

    private static string CanonicalValue(string raw) =>
        IsPureAscii(raw)
            ? LowercaseOutsideEscapes(raw.Replace("%20", "+", StringComparison.Ordinal))
            : raw;

    private static bool IsPureAscii(string raw) =>
        Uri.UnescapeDataString(raw).All(static character => character < 0x80);

    private static string LowercaseOutsideEscapes(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (IsEscapeAt(value, index))
            {
                builder.Append(value, index, 3);
                index += 2;
            }
            else
            {
                builder.Append(char.ToLowerInvariant(value[index]));
            }
        }

        return builder.ToString();
    }

    private static bool IsEscapeAt(string value, int index) =>
        value[index] == '%'
        && index + 2 < value.Length
        && Uri.IsHexDigit(value[index + 1])
        && Uri.IsHexDigit(value[index + 2]);

    private static string RawQueryOf(IRequestMessage request) =>
        request.RawQuery.TrimStart('?');

    private static string? QueryValue(string rawQuery, string name)
    {
        foreach (var pair in rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            var key = separator < 0 ? pair : pair[..separator];
            if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return separator < 0
                ? string.Empty
                : Uri.UnescapeDataString(pair[(separator + 1)..].Replace('+', ' '));
        }

        return null;
    }

    private static string? HeaderValue(IRequestMessage request, string name)
    {
        var headers = request.Headers;
        if (headers is null)
        {
            return null;
        }

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value.FirstOrDefault();
            }
        }

        return null;
    }

    private static long? ReadFileId(string? body)
    {
        try
        {
            using var document = JsonDocument.Parse(body ?? string.Empty);
            return document.RootElement.TryGetProperty("file_id", out var property) && property.TryGetInt64(out var fileId)
                ? fileId
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed record OpenSubtitlesSubtitle(
    long FileId,
    string Language,
    int DownloadCount,
    string Release,
    string FeatureType,
    string FeatureTitle,
    bool MovieHashMatch = false);

public sealed record OpenSubtitlesScenario
{
    public const string DefaultSrt =
        "1\n00:00:01,000 --> 00:00:03,500\nI have seen things.\n\n2\n00:00:04,000 --> 00:00:06,000\nYou people would not believe.\n";

    public IReadOnlyList<OpenSubtitlesSubtitle> Subtitles { get; init; } = [];

    public string SrtText { get; init; } = DefaultSrt;

    public int DownloadsRemaining { get; init; } = 99;

    public DateTimeOffset ResetTimeUtc { get; init; } = new(2026, 3, 4, 23, 59, 59, 999, TimeSpan.Zero);
}
