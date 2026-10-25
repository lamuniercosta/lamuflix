using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace LamuFlix.Evaluation.Tests.TestSupport;

/// <summary>
/// Fixture payloads exercised by the command and publication tests. Every
/// payload is derived from the Phase 3 nonempty baseline or is a deliberately
/// invalid/empty variant of it; nothing here re-derives expected metrics.
/// </summary>
internal static class EvaluationCommandFixtures
{
    public const string BaselineFixtureId = "dev-356-baseline";
    public const int BaselineObservationCount = 12;
    public const int BaselineHitCount = 9;
    public const double BaselineHitRateAt10 = 0.75;

    private const string EmptyEventsFixture = """
        {
          "fixtureId": "dev-356-empty",
          "ranking": {
            "lambda": 0.5,
            "similarityWeights": {
              "director": 1.0,
              "writers": 1.0,
              "cast": 1.0,
              "genres": 1.0,
              "yearProximity": 1.0,
              "runtimeProximity": 1.0,
              "ratingBoost": 1.0
            }
          },
          "catalog": [
            { "movieId": 1, "features": { "genres": ["Drama"] } }
          ],
          "events": []
        }
        """;

    private const string DuplicateEventFixture = """
        {
          "fixtureId": "dev-356-duplicate",
          "ranking": {
            "lambda": 0.5,
            "similarityWeights": {
              "director": 1.0,
              "writers": 1.0,
              "cast": 1.0,
              "genres": 1.0,
              "yearProximity": 1.0,
              "runtimeProximity": 1.0,
              "ratingBoost": 1.0
            }
          },
          "catalog": [
            { "movieId": 1, "features": { "genres": ["Drama"] } },
            { "movieId": 2, "features": { "genres": ["Drama"] } }
          ],
          "events": [
            { "eventId": "e01", "userId": "u1", "movieId": 1, "timestamp": "2026-01-01T00:00:00Z" },
            { "eventId": "e01", "userId": "u1", "movieId": 2, "timestamp": "2026-01-02T00:00:00Z" }
          ]
        }
        """;

    public static string BaselineJson() => File.ReadAllText(EvaluationCommand.FixturePath);

    public static string WriteEmptyEventsFixture(string path)
    {
        File.WriteAllText(path, EmptyEventsFixture);
        return path;
    }

    public static string WriteDuplicateEventFixture(string path)
    {
        File.WriteAllText(path, DuplicateEventFixture);
        return path;
    }

    public static string WriteMalformedFixture(string path)
    {
        File.WriteAllText(path, "{ \"fixtureId\": ");
        return path;
    }

    public static string WriteBaselineCopy(string path)
    {
        File.WriteAllText(path, BaselineJson());
        return path;
    }

    /// <summary>
    /// Reverses the presentation order of the baseline events (and of the catalog
    /// entries) without changing any identity, timestamp or feature value. This is
    /// the "equivalent original/permuted fixture" of the same complete baseline.
    /// </summary>
    public static string WritePresentationPermutedBaseline(string path)
    {
        var root = JsonNode.Parse(BaselineJson())
            ?? throw new InvalidOperationException("The baseline fixture could not be parsed.");
        root["events"] = Reverse(root["events"]!.AsArray());
        root["catalog"] = Reverse(root["catalog"]!.AsArray());
        File.WriteAllText(path, root.ToJsonString());
        return path;
    }

    private static JsonArray Reverse(JsonArray source)
    {
        var copies = source
            .Select(item => JsonNode.Parse(
                item!.ToJsonString(),
                documentOptions: default,
                nodeOptions: default) ?? throw new InvalidOperationException("Fixture node could not be copied."))
            .ToList();
        copies.Reverse();

        return [.. copies];
    }
}
