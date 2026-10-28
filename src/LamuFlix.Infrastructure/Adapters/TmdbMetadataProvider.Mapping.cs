using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class TmdbMetadataProvider
{
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";
    private const int YearDigits = 4;

    private static readonly char[] UnusablePosterPathCharacters = ['\\', ':'];

    private static class ResponseMapper
    {
        public static bool IsNoResult(Response response) =>
            response.Success == false && response.StatusCode == NoResultStatusCode;

        public static int? MapYear(string? releaseDate) =>
            TryLeadingDigits(releaseDate, out var value) ? value : null;

        public static MovieMetadata? Map(Response response, DateTimeOffset now) =>
            TryTitle(response.Title, out var title)
                ? new MovieMetadata(
                    title,
                    synopsis: MapSynopsis(response.Overview),
                    releaseYear: MapReleaseYear(response.ReleaseDate, now),
                    runtime: MapRuntime(response.Runtime),
                    imdbId: MapImdbId(response.ImdbId),
                    posterUrl: MapPosterUrl(response.PosterPath))
                : null;

        private static bool TryTitle(string? value, [NotNullWhen(true)] out string? title)
        {
            title = string.IsNullOrWhiteSpace(value) ? null : value;
            return title is not null;
        }

        private static string? MapSynopsis(string? overview) =>
            string.IsNullOrWhiteSpace(overview) ? null : overview;

        private static ReleaseYear? MapReleaseYear(string? releaseDate, DateTimeOffset now) =>
            TryLeadingDigits(releaseDate, out var value) && ReleaseYear.TryCreate(value, now, out var releaseYear)
                ? releaseYear
                : null;

        private static Runtime? MapRuntime(int? runtime) =>
            runtime is { } minutes && Runtime.TryCreate(minutes, out var parsed) ? parsed : null;

        private static ImdbId? MapImdbId(string? imdbId) =>
            ImdbId.TryCreate(imdbId, out var parsed) ? parsed : null;

        private static string? MapPosterUrl(string? posterPath) =>
            IsUsablePosterPath(posterPath)
            && Uri.TryCreate(string.Concat(ImageBaseUrl, posterPath), UriKind.Absolute, out var uri)
                ? uri.AbsoluteUri
                : null;

        private static bool IsUsablePosterPath([NotNullWhen(true)] string? posterPath) =>
            posterPath is { Length: > 1 }
            && posterPath[0] == '/'
            && posterPath[1] != '/'
            && posterPath.IndexOfAny(UnusablePosterPathCharacters) < 0
            && !ContainsControlCharacter(posterPath)
            && HasNoRelativeSegments(posterPath);

        private static bool ContainsControlCharacter(string value)
        {
            foreach (var character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNoRelativeSegments(string posterPath) =>
            posterPath[1..].Split('/').All(segment => segment is not ("." or ".."));

        private static bool TryLeadingDigits(string? value, out int result)
        {
            result = 0;
            if (value is null)
            {
                return false;
            }

            var count = Math.Min(DigitRun(value), YearDigits);
            return count > 0
                && int.TryParse(value.AsSpan(0, count), NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        private static int DigitRun(string value)
        {
            var count = 0;
            while (count < value.Length && char.IsAsciiDigit(value[count]))
            {
                count++;
            }

            return count;
        }
    }

    private sealed class Response
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("results")]
        public List<SearchHit>? Results { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("movie_results")]
        public List<SearchHit>? MovieResults { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("runtime")]
        public int? Runtime { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("overview")]
        public string? Overview { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("success")]
        public bool? Success { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("status_code")]
        public int? StatusCode { get; init; }
    }

    private sealed class SearchHit
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; init; }
    }
}
