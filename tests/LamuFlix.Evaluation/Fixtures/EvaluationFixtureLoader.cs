using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.Evaluation.Fixtures;

public static class EvaluationFixtureLoader
{
    public static EvaluationFixture LoadFromJson(string json, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(timeProvider);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new EvaluationFixtureValidationException(
                "Fixture JSON is malformed: " + exception.Message,
                exception);
        }

        using (document)
        {
            return ReadFixture(document.RootElement, timeProvider);
        }
    }

    private static EvaluationFixture ReadFixture(JsonElement root, TimeProvider timeProvider)
    {
        RequireObject(root, "Fixture root");

        var fixtureId = RequiredText(root, "fixtureId", "Fixture fixtureId");
        var rankingInputs = ReadRankingInputs(root);
        var catalog = ReadCatalog(root, timeProvider);
        var events = ReadEvents(root, catalog);

        return new EvaluationFixture(fixtureId, catalog, rankingInputs, events);
    }

    private static EvaluationRankingInputs ReadRankingInputs(JsonElement root)
    {
        var ranking = RequiredProperty(root, "ranking");
        RequireObject(ranking, "Fixture ranking");

        var lambda = RequiredFiniteNumber(ranking, "lambda", "Fixture ranking lambda");
        if (lambda is < 0.0 or > 1.0)
        {
            throw Invalid("Fixture ranking lambda must be within the inclusive range [0, 1].");
        }

        var weights = RequiredProperty(ranking, "similarityWeights");
        RequireObject(weights, "Fixture ranking similarityWeights");

        var similarityWeights = new SimilarityWeights
        {
            Director = ReadWeight(weights, "director"),
            Writers = ReadWeight(weights, "writers"),
            Cast = ReadWeight(weights, "cast"),
            Genres = ReadWeight(weights, "genres"),
            YearProximity = ReadWeight(weights, "yearProximity"),
            RuntimeProximity = ReadWeight(weights, "runtimeProximity"),
            RatingBoost = ReadWeight(weights, "ratingBoost"),
        };

        return new EvaluationRankingInputs(lambda, similarityWeights);
    }

    private static double ReadWeight(JsonElement weights, string name)
    {
        var weight = RequiredFiniteNumber(weights, name, $"Fixture ranking weight '{name}'");
        if (weight < 0.0)
        {
            throw Invalid($"Fixture ranking weight '{name}' must be non-negative.");
        }

        return weight;
    }

    private static ImmutableArray<EvaluationCatalogMovie> ReadCatalog(JsonElement root, TimeProvider timeProvider)
    {
        var catalog = RequiredProperty(root, "catalog");
        if (catalog.ValueKind != JsonValueKind.Array)
        {
            throw Invalid("Fixture catalog must be an array.");
        }

        var now = timeProvider.GetUtcNow();
        var seenMovieIds = new HashSet<MovieId>();
        var movies = ImmutableArray.CreateBuilder<EvaluationCatalogMovie>();

        foreach (var entry in catalog.EnumerateArray())
        {
            RequireObject(entry, "Fixture catalog entry");

            var movieId = ReadMovieId(entry, "Fixture catalog entry movieId");
            if (!seenMovieIds.Add(movieId))
            {
                throw Invalid($"Fixture catalog contains duplicate movie id '{movieId.Value}'.");
            }

            movies.Add(new EvaluationCatalogMovie(movieId, ReadFeatures(entry, now, movieId)));
        }

        return movies.ToImmutable();
    }

    private static MovieFeatures ReadFeatures(JsonElement entry, DateTimeOffset now, MovieId movieId)
    {
        var description = $"Fixture catalog entry '{movieId.Value}'";
        var features = RequiredProperty(entry, "features");
        RequireObject(features, $"{description} features");

        return new MovieFeatures(
            genres: OptionalStringArray(features, "genres", $"{description} genres"),
            directors: OptionalStringArray(features, "directors", $"{description} directors"),
            writers: OptionalStringArray(features, "writers", $"{description} writers"),
            cast: OptionalStringArray(features, "cast", $"{description} cast"),
            releaseYear: ReadReleaseYear(features, now, description),
            runtime: ReadRuntime(features, description),
            imdbRating: ReadImdbRating(features, description),
            metaScore: ReadMetaScore(features, description),
            fullPlot: OptionalString(features, "fullPlot", $"{description} fullPlot"));
    }

    private static ReleaseYear? ReadReleaseYear(JsonElement features, DateTimeOffset now, string description)
    {
        var value = OptionalInt32(features, "releaseYear", $"{description} releaseYear");
        if (value is null)
        {
            return null;
        }

        if (!ReleaseYear.TryCreate(value.Value, now, out var releaseYear))
        {
            throw Invalid($"{description} releaseYear is out of range.");
        }

        return releaseYear;
    }

    private static Runtime? ReadRuntime(JsonElement features, string description)
    {
        var value = OptionalInt32(features, "runtimeMinutes", $"{description} runtimeMinutes");
        if (value is null)
        {
            return null;
        }

        if (!Runtime.TryCreate(value.Value, out var runtime))
        {
            throw Invalid($"{description} runtimeMinutes is out of range.");
        }

        return runtime;
    }

    private static ImdbRating? ReadImdbRating(JsonElement features, string description)
    {
        var value = OptionalDecimal(features, "imdbRating", $"{description} imdbRating");
        if (value is null)
        {
            return null;
        }

        if (!ImdbRating.TryCreate(value.Value, out var rating))
        {
            throw Invalid($"{description} imdbRating is out of range.");
        }

        return rating;
    }

    private static int? ReadMetaScore(JsonElement features, string description)
    {
        var value = OptionalInt32(features, "metaScore", $"{description} metaScore");
        if (value is null)
        {
            return null;
        }

        if (value.Value is < 0 or > 100)
        {
            throw Invalid($"{description} metaScore is out of range.");
        }

        return value;
    }

    private static ImmutableArray<EvaluationEvent> ReadEvents(
        JsonElement root,
        ImmutableArray<EvaluationCatalogMovie> catalog)
    {
        var events = RequiredProperty(root, "events");
        if (events.ValueKind != JsonValueKind.Array)
        {
            throw Invalid("Fixture events must be an array.");
        }

        var catalogIds = catalog.Select(movie => movie.MovieId).ToHashSet();
        var seenEventIds = new HashSet<string>(StringComparer.Ordinal);
        var rows = ImmutableArray.CreateBuilder<EvaluationEvent>();

        foreach (var entry in events.EnumerateArray())
        {
            RequireObject(entry, "Fixture event");

            var eventId = RequiredText(entry, "eventId", "Fixture event eventId");
            if (!seenEventIds.Add(eventId))
            {
                throw Invalid($"Fixture events contain duplicate event id '{eventId}'.");
            }

            var userId = RequiredText(entry, "userId", "Fixture event userId");
            var movieId = ReadMovieId(entry, "Fixture event movieId");
            if (!catalogIds.Contains(movieId))
            {
                throw Invalid(
                    $"Fixture event '{eventId}' references movie id '{movieId.Value}' that is absent from the catalog.");
            }

            rows.Add(new EvaluationEvent(eventId, userId, movieId, ReadTimestamp(entry)));
        }

        return rows.ToImmutable();
    }

    private static DateTimeOffset ReadTimestamp(JsonElement entry)
    {
        var text = RequiredText(entry, "timestamp", "Fixture event timestamp");

        if (!HasExplicitOffset(text)
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)
            || timestamp.Offset != TimeSpan.Zero)
        {
            throw Invalid("Fixture event timestamp must be an explicit UTC timestamp.");
        }

        return timestamp;
    }

    private static bool HasExplicitOffset(string text)
    {
        var marker = text.IndexOf('T', StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        var timePart = text.AsSpan(marker + 1);
        return timePart.Contains('Z')
            || timePart.Contains('z')
            || timePart.Contains('+')
            || timePart.Contains('-');
    }

    private static MovieId ReadMovieId(JsonElement element, string description)
    {
        var value = RequiredInt32(element, "movieId", description);
        if (!MovieId.TryCreate(value, out var movieId))
        {
            throw Invalid($"{description} must be greater than zero.");
        }

        return movieId;
    }

    private static string[]? OptionalStringArray(JsonElement element, string name, string description)
    {
        if (!TryGetPresent(element, name, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw Invalid($"{description} must be an array of strings.");
        }

        var values = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw Invalid($"{description} must contain only strings.");
            }

            values.Add(item.GetString() ?? throw Invalid($"{description} must contain only strings."));
        }

        return [.. values];
    }

    private static string? OptionalString(JsonElement element, string name, string description)
    {
        if (!TryGetPresent(element, name, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw Invalid($"{description} must be a string.");
        }

        return property.GetString();
    }

    private static int? OptionalInt32(JsonElement element, string name, string description)
    {
        if (!TryGetPresent(element, name, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw Invalid($"{description} must be an integer.");
        }

        return value;
    }

    private static decimal? OptionalDecimal(JsonElement element, string name, string description)
    {
        if (!TryGetPresent(element, name, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Number
            || !decimal.TryParse(
                property.GetRawText(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw Invalid($"{description} must be a number.");
        }

        return value;
    }

    private static bool TryGetPresent(JsonElement element, string name, out JsonElement property) =>
        element.TryGetProperty(name, out property) && property.ValueKind != JsonValueKind.Null;

    private static JsonElement RequiredProperty(JsonElement element, string name)
    {
        if (!TryGetPresent(element, name, out var property))
        {
            throw Invalid($"Fixture property '{name}' is required.");
        }

        return property;
    }

    private static string RequiredText(JsonElement element, string name, string description)
    {
        var property = RequiredProperty(element, name);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw Invalid($"{description} must be a string.");
        }

        var text = property.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw Invalid($"{description} must not be blank.");
        }

        return text;
    }

    private static int RequiredInt32(JsonElement element, string name, string description)
    {
        var property = RequiredProperty(element, name);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw Invalid($"{description} must be an integer.");
        }

        return value;
    }

    private static double RequiredFiniteNumber(JsonElement element, string name, string description)
    {
        var property = RequiredProperty(element, name);
        if (property.ValueKind != JsonValueKind.Number
            || !double.TryParse(
                property.GetRawText(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value)
            || !double.IsFinite(value))
        {
            throw Invalid($"{description} must be a finite number.");
        }

        return value;
    }

    private static void RequireObject(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Invalid($"{description} must be a JSON object.");
        }
    }

    private static EvaluationFixtureValidationException Invalid(string message) => new(message);
}
