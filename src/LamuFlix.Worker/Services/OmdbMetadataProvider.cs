using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LamuFlix.Worker.Models;

namespace LamuFlix.Worker.Services;

public class OmdbMetadataProvider : IMetadataProvider
{
    private const string ApiKeyConfigKey = "Movies:OMDB_API_KEY";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OmdbMetadataProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(configuration);

        var apiKey = configuration[ApiKeyConfigKey];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"OMDb API key not configured. Set '{ApiKeyConfigKey}' via user secrets " +
                $"(dotnet user-secrets set \"{ApiKeyConfigKey}\" <key> --project LamuFlix.Work) " +
                $"or the Movies__OMDB_API_KEY environment variable.");
        }

        _apiKey = apiKey;
    }

    public async Task<MovieMetadata?> FetchMetadataAsync(string title, int? year = null, string? imdbId = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(title, year, imdbId);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return !CheckResponse(root) ? null : ParseModel(root);
    }

    private string BuildUrl(string title, int? year, string? imdbId)
    {
        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            return $"http://www.omdbapi.com/?apikey={_apiKey}&i={Uri.EscapeDataString(imdbId)}";
        }

        var yearParam = year.HasValue ? $"&y={year.Value}" : string.Empty;
        return $"http://www.omdbapi.com/?apikey={_apiKey}&t={Uri.EscapeDataString(title)}{yearParam}";
    }

    private static bool CheckResponse(JsonElement root)
    {
        return root.TryGetProperty("Response", out var respProp) && string.Equals(respProp.GetString(), "True", StringComparison.OrdinalIgnoreCase);
    }

    private static MovieMetadata ParseModel(JsonElement root)
    {
        var title = GetString(root, "Title");
        var synopsis = GetString(root, "Plot");
        var year = ParseInt(GetString(root, "Year"));
        var duration = ParseRuntime(GetString(root, "Runtime"));
        var poster = GetString(root, "Poster");
        var imdbRating = ParseDecimalRating(GetString(root, "imdbRating"));
        var rottenTomatoes = ExtractRottenTomatoes(root);
        var metaScore = ParseInt(GetString(root, "Metascore"));
        var imdbId = GetString(root, "imdbID");
        var genres = ParseList(GetString(root, "Genre"));
        var actors = ParseList(GetString(root, "Actors"));
        var directors = ParseList(GetString(root, "Director"));

        return new MovieMetadata(title, synopsis, year, duration, poster, imdbRating, rottenTomatoes, metaScore, imdbId, genres, actors, directors);
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value?.Replace("N/A", "0").Trim(), out var result))
        {
            return result;
        }
        return null;
    }

    private static decimal? ParseDecimalRating(string? value)
    {
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result / 10m;
        }
        return null;
    }

    private static int? ParseRuntime(string? runtime)
    {
        if (string.IsNullOrWhiteSpace(runtime)) return null;
        var clean = runtime.Replace(" min", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (int.TryParse(clean, out var res)) return res;
        return null;
    }

    private static int? ExtractRottenTomatoes(JsonElement root)
    {
        return !TryGetRatingsArray(root, out var ratings) ? null : FindRottenTomatoesValue(ratings);
    }

    private static bool TryGetRatingsArray(JsonElement root, out JsonElement ratings)
    {
        if (root.TryGetProperty("Ratings", out ratings) && ratings.ValueKind == JsonValueKind.Array)
        {
            return true;
        }
        ratings = default;
        return false;
    }

    private static int? FindRottenTomatoesValue(JsonElement ratings)
    {
        foreach (var rating in ratings.EnumerateArray())
        {
            if (IsRottenTomatoesRating(rating, out var rt))
            {
                return rt;
            }
        }
        return null;
    }

    private static bool IsRottenTomatoesRating(JsonElement rating, out int rt)
    {
        rt = 0;
        if (!rating.TryGetProperty("Source", out var source) ||
            !string.Equals(source.GetString(), "Rotten Tomatoes", StringComparison.OrdinalIgnoreCase) ||
            !rating.TryGetProperty("Value", out var valProp)) return false;
        var val = valProp.GetString()?.Replace("%", "").Trim();
        return int.TryParse(val, out rt);
    }

    private static IReadOnlyList<string>? ParseList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A") return null;
        var parts = value.Split(',');
        return [.. parts.Select(p => p.Trim()).Where(trimmed => !string.IsNullOrEmpty(trimmed))];
    }
}