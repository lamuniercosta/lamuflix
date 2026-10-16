using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Adapters;

public sealed partial class OmdbMetadataProvider
{
    private static class ResponseMapper
    {
        public static bool IsFound(Response response) =>
            string.Equals(response.ResponseFlag, FoundFlag, StringComparison.Ordinal);

        public static bool IsMovieNotFound(Response response) =>
            string.Equals(response.ResponseFlag, NotFoundFlag, StringComparison.Ordinal)
            && string.Equals(response.Error, MovieNotFoundError, StringComparison.Ordinal);

        public static MovieMetadata? Map(Response response, DateTimeOffset now) =>
            TryTitle(response.Title, out var title)
                ? new MovieMetadata(
                    title,
                    MapSynopsis(response.Plot),
                    MapReleaseYear(response.Year, now),
                    MapRuntime(response.Runtime),
                    MapRating(response.Rating),
                    MapIdentifier(response.Identifier),
                    MapPoster(response.Poster),
                    metaScore: MapMetaScore(response.MetaScore),
                    directors: SplitCredits(response.Director),
                    actors: SplitCredits(response.Actors),
                    genres: SplitCredits(response.Genre),
                    writers: SplitCredits(response.Writer))
                : null;

        private static bool TryTitle(string? value, [NotNullWhen(true)] out string? title)
        {
            title = IsMissing(value) ? null : value;
            return title is not null;
        }

        private static string? MapSynopsis(string? plot) => IsMissing(plot) ? null : plot;

        private static string? MapPoster(string? poster)
        {
            var trimmed = poster?.Trim();
            return IsMissing(trimmed) ? null : trimmed;
        }

        private static ReleaseYear? MapReleaseYear(string? year, DateTimeOffset now)
        {
            if (!TryLeadingDigits(year, out var value))
            {
                return null;
            }

            return ReleaseYear.TryCreate(value, now, out var releaseYear) ? releaseYear : null;
        }

        private static Runtime? MapRuntime(string? runtime)
        {
            if (!TryLeadingDigits(runtime, out var value))
            {
                return null;
            }

            return Runtime.TryCreate(value, out var minutes) ? minutes : null;
        }

        private static ImdbRating? MapRating(string? rating)
        {
            if (!decimal.TryParse(rating, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            return ImdbRating.TryCreate(value, out var parsed) ? parsed : null;
        }

        private static ImdbId? MapIdentifier(string? identifier) =>
            ImdbId.TryCreate(identifier, out var parsed) ? parsed : null;

        private static short? MapMetaScore(string? metaScore) =>
            int.TryParse(metaScore, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                && value is >= 0 and <= 100
                ? (short)value
                : null;

        private static IEnumerable<string> SplitCredits(string? credits)
        {
            if (credits is null)
            {
                return [];
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var token in credits.Split(','))
            {
                var trimmed = token.Trim();
                if (IsMissing(trimmed) || !seen.Add(trimmed))
                {
                    continue;
                }

                result.Add(trimmed);
            }

            return result;
        }

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

        private static bool IsMissing(string? value) =>
            string.IsNullOrWhiteSpace(value) || string.Equals(value, NotAvailable, StringComparison.Ordinal);
    }

    private sealed class Response
    {
        [JsonPropertyName("Response")]
        public string? ResponseFlag { get; set; }

        [JsonPropertyName("Error")]
        public string? Error { get; set; }

        [JsonPropertyName("Title")]
        public string? Title { get; set; }

        [JsonPropertyName("Year")]
        public string? Year { get; set; }

        [JsonPropertyName("Runtime")]
        public string? Runtime { get; set; }

        [JsonPropertyName("Plot")]
        public string? Plot { get; set; }

        [JsonPropertyName("imdbRating")]
        public string? Rating { get; set; }

        [JsonPropertyName("imdbID")]
        public string? Identifier { get; set; }

        [JsonPropertyName("Poster")]
        public string? Poster { get; set; }

        [JsonPropertyName("Genre")]
        public string? Genre { get; set; }

        [JsonPropertyName("Director")]
        public string? Director { get; set; }

        [JsonPropertyName("Writer")]
        public string? Writer { get; set; }

        [JsonPropertyName("Actors")]
        public string? Actors { get; set; }

        [JsonPropertyName("Metascore")]
        public string? MetaScore { get; set; }
    }
}