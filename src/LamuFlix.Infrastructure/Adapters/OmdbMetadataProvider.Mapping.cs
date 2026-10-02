using System;
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
                    MapIdentifier(response.Identifier))
                : null;

        private static bool TryTitle(string? value, [NotNullWhen(true)] out string? title)
        {
            title = IsMissing(value) ? null : value;
            return title is not null;
        }

        private static string? MapSynopsis(string? plot) => IsMissing(plot) ? null : plot;

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
    }
}