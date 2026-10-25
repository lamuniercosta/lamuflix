using System;
using LamuFlix.Evaluation.Fixtures;
using Shouldly;

namespace LamuFlix.Evaluation.Tests.Fixtures;

public sealed class EvaluationFixtureLoaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    private const string DefaultEvents = """
            { "eventId": "e1", "userId": "u1", "movieId": 1, "timestamp": "2026-01-01T00:00:00Z" },
            { "eventId": "e2", "userId": "u2", "movieId": 2, "timestamp": "2026-01-02T00:00:00Z" }
        """;

    [Fact]
    public void LoadFromJson_ValidFixture_ReadsDeclaredFixtureValues()
    {
        // act
        var fixture = Load(Fixture());

        // assert
        fixture.ShouldSatisfyAllConditions(
            f => f.FixtureId.ShouldBe("baseline"),
            f => f.RankingInputs.Lambda.ShouldBe(0.7),
            f => f.RankingInputs.SimilarityWeights.Director.ShouldBe(3.0),
            f => f.RankingInputs.SimilarityWeights.RatingBoost.ShouldBe(0.5),
            f => f.Catalog.Length.ShouldBe(2),
            f => f.Events.Length.ShouldBe(2));
    }

    [Fact]
    public void LoadFromJson_ValidFixture_ReadsImmutableCatalogFeatures()
    {
        // act
        var fixture = Load(Fixture());

        // assert
        var movie = fixture.Catalog[0];
        movie.MovieId.Value.ShouldBe(1);
        movie.Features.Genres.ShouldBe(["Drama", "Noir"], ignoreOrder: true);
        movie.Features.Directors.ShouldBe(["Ada"], ignoreOrder: true);
        movie.Features.Writers.ShouldBe(["Bea"], ignoreOrder: true);
        movie.Features.Cast.ShouldBe(["Cy"], ignoreOrder: true);
        movie.Features.ReleaseYear!.Value.ShouldBe(2010);
        movie.Features.Runtime!.Minutes.ShouldBe(102);
        movie.Features.ImdbRating!.Value.ShouldBe(7.5m);
        movie.Features.MetaScore.ShouldBe(81);
        movie.Features.FullPlot.ShouldBe("Plot");
    }

    [Fact]
    public void LoadFromJson_ValidFixture_ReadsEventRowsAsDeclared()
    {
        // act
        var fixture = Load(Fixture());

        // assert
        var first = fixture.Events[0];
        first.EventId.ShouldBe("e1");
        first.UserId.ShouldBe("u1");
        first.MovieId.Value.ShouldBe(1);
        first.Timestamp.ShouldBe(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        fixture.Events[1].EventId.ShouldBe("e2");
    }

    [Fact]
    public void LoadFromJson_EmptyEventArray_IsAccepted()
    {
        // act
        var fixture = Load(Fixture(events: string.Empty));

        // assert
        fixture.Events.ShouldBeEmpty();
        fixture.Catalog.Length.ShouldBe(2);
    }

    [Fact]
    public void LoadFromJson_DistinctEventIdsWithIdenticalRows_PreservesEveryRow()
    {
        // arrange
        const string duplicatedRows = """
                { "eventId": "e1", "userId": "u1", "movieId": 1, "timestamp": "2026-01-01T00:00:00Z" },
                { "eventId": "e2", "userId": "u1", "movieId": 1, "timestamp": "2026-01-01T00:00:00Z" }
            """;

        // act
        var fixture = Load(Fixture(events: duplicatedRows));

        // assert
        fixture.Events.Length.ShouldBe(2);
    }

    [Fact]
    public void LoadFromJson_MalformedJson_IsRejected()
    {
        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load("{"));

        // assert
        failure.Message.ShouldContain("malformed");
    }

    [Fact]
    public void LoadFromJson_MissingFixtureId_IsRejected()
    {
        // arrange
        var json = Fixture().Replace("\"fixtureId\": \"baseline\",", string.Empty, StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("fixtureId");
    }

    [Fact]
    public void LoadFromJson_BlankFixtureId_IsRejected()
    {
        // arrange
        var json = Fixture().Replace("\"baseline\"", "\"  \"", StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("fixtureId");
    }

    [Fact]
    public void LoadFromJson_DuplicateEventIds_AreRejected()
    {
        // arrange
        var json = Fixture().Replace("\"eventId\": \"e2\"", "\"eventId\": \"e1\"", StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("e1");
    }

    [Fact]
    public void LoadFromJson_DuplicateCatalogMovieIds_AreRejected()
    {
        // arrange
        var json = Fixture().Replace(
            "{ \"movieId\": 2, \"features\": { \"genres\": [\"Comedy\"] } }",
            "{ \"movieId\": 1, \"features\": { \"genres\": [\"Comedy\"] } }",
            StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("catalog");
    }

    [Fact]
    public void LoadFromJson_UnresolvedEventMovieReference_IsRejected()
    {
        // arrange
        const string unresolvedEvents = """
                { "eventId": "e1", "userId": "u1", "movieId": 9, "timestamp": "2026-01-01T00:00:00Z" }
            """;

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(Fixture(events: unresolvedEvents)));

        // assert
        failure.Message.ShouldContain("9");
    }

    [Fact]
    public void LoadFromJson_NonFiniteLambda_IsRejected()
    {
        // arrange
        var json = Fixture().Replace("\"lambda\": 0.7", "\"lambda\": 1e400", StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("lambda");
    }

    [Fact]
    public void LoadFromJson_NonFiniteSimilarityWeight_IsRejected()
    {
        // arrange
        var json = Fixture().Replace("\"ratingBoost\": 0.5", "\"ratingBoost\": 1e400", StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("ratingBoost");
    }

    [Fact]
    public void LoadFromJson_OutOfRangeCatalogFeatureValue_IsRejected()
    {
        // arrange
        var json = Fixture().Replace("\"imdbRating\": 7.5", "\"imdbRating\": 11.0", StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("imdbRating");
    }

    [Fact]
    public void LoadFromJson_MissingEventTimestamp_IsRejected()
    {
        // arrange
        var json = Fixture().Replace(
            ", \"timestamp\": \"2026-01-01T00:00:00Z\"",
            string.Empty,
            StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("timestamp");
    }

    [Fact]
    public void LoadFromJson_TimestampWithoutExplicitUtc_IsRejected()
    {
        // arrange
        var json = Fixture().Replace(
            "\"2026-01-01T00:00:00Z\"",
            "\"2026-01-01T00:00:00\"",
            StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("timestamp");
    }

    [Fact]
    public void LoadFromJson_TimestampWithNonZeroOffset_IsRejected()
    {
        // arrange
        var json = Fixture().Replace(
            "\"2026-01-01T00:00:00Z\"",
            "\"2026-01-01T00:00:00+02:00\"",
            StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("timestamp");
    }

    [Fact]
    public void LoadFromJson_NonNumericMovieId_IsRejected()
    {
        // arrange
        var json = Fixture().Replace("\"movieId\": 1,", "\"movieId\": \"1\",", StringComparison.Ordinal);

        // act
        var failure = Should.Throw<EvaluationFixtureValidationException>(() => Load(json));

        // assert
        failure.Message.ShouldContain("movieId");
    }

    private static EvaluationFixture Load(string json) => EvaluationFixtureLoader.LoadFromJson(json, new FixedTimeProvider());

    private static string Fixture(string events = DefaultEvents) => $$"""
        {
          "fixtureId": "baseline",
          "ranking": {
            "lambda": 0.7,
            "similarityWeights": {
              "director": 3.0,
              "writers": 2.0,
              "cast": 2.0,
              "genres": 3.0,
              "yearProximity": 1.0,
              "runtimeProximity": 0.5,
              "ratingBoost": 0.5
            }
          },
          "catalog": [
            {
              "movieId": 1,
              "features": {
                "genres": ["Drama", "Noir"],
                "directors": ["Ada"],
                "writers": ["Bea"],
                "cast": ["Cy"],
                "releaseYear": 2010,
                "runtimeMinutes": 102,
                "imdbRating": 7.5,
                "metaScore": 81,
                "fullPlot": "Plot"
              }
            },
            { "movieId": 2, "features": { "genres": ["Comedy"] } }
          ],
          "events": [
        {{events}}
          ]
        }
        """;

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
