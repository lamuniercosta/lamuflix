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

        public static bool IsFailed(Response response) =>
            response.Success == false && !IsNoResult(response);

        public static int? MapYear(string? releaseDate) =>
            TryLeadingDigits(releaseDate, out var value) ? value : null;

        public static bool IsMalformedCollection(Response response) =>
            response.BelongsToCollection is { } collection
            && (collection.Id is not { } id || id <= 0 || string.IsNullOrWhiteSpace(collection.Name));

        public static MovieMetadata? Map(Response response, DateTimeOffset now)
        {
            if (!TryTitle(response.Title, out var title))
            {
                return null;
            }

            var collection = MapCollection(response.BelongsToCollection);
            return new MovieMetadata(
                title,
                synopsis: MapSynopsis(response.Overview),
                releaseYear: MapReleaseYear(response.ReleaseDate, now),
                runtime: MapRuntime(response.Runtime),
                imdbId: MapImdbId(response.ImdbId),
                posterUrl: MapPosterUrl(response.PosterPath),
                collectionId: collection.Id,
                collectionName: collection.Name);
        }

        private static (int? Id, string? Name) MapCollection(CollectionPayload? collection) =>
            collection is null ? (null, null) : (collection.Id, collection.Name);

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

    /// <summary>The selector input: one TMDb video entry already reduced to the fields selection needs.</summary>
    internal readonly record struct TrailerCandidate(
        string? Key,
        string? Site,
        string? Type,
        bool? Official,
        string? Language,
        string? Region);

    /// <summary>
    /// Pure, provider-independent Trailer key selection over the union of the documented videos requests.
    /// </summary>
    internal static class TrailerSelector
    {
        private const string YouTubeSite = "YouTube";
        private const string TrailerType = "Trailer";
        private const string PortugueseIsoCode = "pt";
        private const string BrazilianRegionIsoCode = "BR";
        private const int BrazilianPortugueseTier = 0;
        private const int OriginalLanguageTier = 1;
        private const int OfficialRank = 0;
        private const int NonOfficialRank = 1;

        public static string? Select(IEnumerable<TrailerCandidate>? candidates, string? originalLanguage)
        {
            if (candidates is null)
            {
                return null;
            }

            var original = NormalizedOriginalLanguage(originalLanguage);
            var ranked = new Dictionary<string, (int Tier, int Officialness)>(StringComparer.Ordinal);
            foreach (var candidate in candidates)
            {
                if (candidate.Key is not { } key
                    || !MovieMetadata.IsValidTrailerKey(key)
                    || !IsYouTubeTrailer(candidate)
                    || LanguageTier(candidate, original) is not { } tier)
                {
                    continue;
                }

                var rank = (Tier: tier, Officialness: candidate.Official == true ? OfficialRank : NonOfficialRank);
                if (!ranked.TryGetValue(key, out var existing) || IsBetterRank(rank, existing))
                {
                    ranked[key] = rank;
                }
            }

            return ranked.Count == 0
                ? null
                : ranked
                    .OrderBy(pair => pair.Value.Tier)
                    .ThenBy(pair => pair.Value.Officialness)
                    .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                    .First()
                    .Key;
        }

        private static string? NormalizedOriginalLanguage(string? originalLanguage) =>
            string.IsNullOrWhiteSpace(originalLanguage) ? null : originalLanguage.Trim();

        private static bool IsBetterRank((int Tier, int Officialness) rank, (int Tier, int Officialness) existing) =>
            rank.Tier < existing.Tier
            || (rank.Tier == existing.Tier && rank.Officialness < existing.Officialness);

        private static bool IsYouTubeTrailer(TrailerCandidate candidate) =>
            Matches(candidate.Site, YouTubeSite) && Matches(candidate.Type, TrailerType);

        private static int? LanguageTier(TrailerCandidate candidate, string? originalLanguage) =>
            IsBrazilianPortuguese(candidate)
                ? BrazilianPortugueseTier
                : Matches(candidate.Language, originalLanguage) ? OriginalLanguageTier : null;

        private static bool IsBrazilianPortuguese(TrailerCandidate candidate) =>
            Matches(candidate.Language, PortugueseIsoCode) && Matches(candidate.Region, BrazilianRegionIsoCode);

        private static bool Matches(string? value, string? expected) =>
            expected is not null && string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Response
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("keywords")]
        public List<KeywordPayload>? Keywords { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("cast")]
        public List<CastPayload>? Cast { get; init; }

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
        [JsonPropertyName("belongs_to_collection")]
        public CollectionPayload? BelongsToCollection { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("success")]
        public bool? Success { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("status_code")]
        public int? StatusCode { get; init; }
    }

    private sealed class KeywordPayload
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class CastPayload
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class CollectionPayload
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class VideosResponse
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("results")]
        public List<VideoPayload>? Results { get; init; }
    }

    private sealed class VideoPayload
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("key")]
        public string? Key { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("site")]
        public string? Site { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("official")]
        public bool? Official { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("iso_639_1")]
        public string? Language { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local - populated only by System.Text.Json deserialization.
        [JsonPropertyName("iso_3166_1")]
        public string? Region { get; init; }

        public TrailerCandidate ToCandidate() =>
            new(Key, Site, Type, Official, Language, Region);
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
