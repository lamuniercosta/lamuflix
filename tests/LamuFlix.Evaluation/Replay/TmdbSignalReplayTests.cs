using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Evaluation.Entry;
using LamuFlix.Evaluation.Fixtures;

namespace LamuFlix.Evaluation.Replay;

public sealed class TmdbSignalReplayTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 12, 0, 0, 0, TimeSpan.Zero);
    private static readonly int Seed = EvaluationBaselineFixture.DefaultSeed;

    [Fact]
    public void EnrichedFixture_RetainsTheDev356BaselineIdentitiesAndRankingInputs()
    {
        // arrange
        var baseline = EvaluationBaselineFixture.Load();

        // act
        var enriched = LoadEnriched();

        // assert
        enriched.FixtureId.ShouldBe(baseline.FixtureId);
        enriched.Catalog.Select(movie => movie.MovieId).ShouldBe(baseline.Catalog.Select(movie => movie.MovieId));
        enriched.Events.ShouldBe(baseline.Events);
        enriched.RankingInputs.Lambda.ShouldBe(baseline.RankingInputs.Lambda);
        enriched.RankingInputs.SimilarityWeights.ShouldSatisfyAllConditions(
            value => value.Director.ShouldBe(baseline.RankingInputs.SimilarityWeights.Director),
            value => value.Writers.ShouldBe(baseline.RankingInputs.SimilarityWeights.Writers),
            value => value.Cast.ShouldBe(baseline.RankingInputs.SimilarityWeights.Cast),
            value => value.Genres.ShouldBe(baseline.RankingInputs.SimilarityWeights.Genres),
            value => value.YearProximity.ShouldBe(baseline.RankingInputs.SimilarityWeights.YearProximity),
            value => value.RuntimeProximity.ShouldBe(baseline.RankingInputs.SimilarityWeights.RuntimeProximity),
            value => value.RatingBoost.ShouldBe(baseline.RankingInputs.SimilarityWeights.RatingBoost),
            value => value.Keyword.ShouldBe(1.0),
            value => value.Collection.ShouldBe(2.0));

        foreach (var (movie, index) in enriched.Catalog.Select((movie, index) => (movie, index)))
        {
            movie.Features.Genres.ShouldBe(baseline.Catalog[index].Features.Genres);
        }
    }

    [Fact]
    public void Run_DisabledMode_ReproducesTheDev356BaselineObservationsAndMetrics()
    {
        // arrange
        var expected = EvaluationBaselineFixture.LoadExpected();
        var fixture = LoadEnriched();

        // act
        var result = EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(Seed));

        // assert
        result.Observations.Select(observation => observation.EventId)
            .ShouldBe(expected.Observations.Select(observation => observation.EventId));

        foreach (var (observation, index) in result.Observations.Select(
                     (observation, index) => (observation, index)))
        {
            var worked = expected.Observations[index];

            observation.Target.Value.ShouldBe(worked.Target);
            EvaluationBaselineFixture.Ids(observation.Ranking).ShouldBe(worked.Ranking);
            EvaluationBaselineFixture.RankOf(observation).ShouldBe(worked.Rank);
        }

        result.Metrics.ShouldSatisfyAllConditions(
            metrics => metrics.ObservationCount.ShouldBe(expected.ObservationCount),
            metrics => metrics.HitCount.ShouldBe(expected.HitCount),
            metrics => metrics.HitRateAt10.ShouldBe(expected.HitRateAt10),
            metrics => metrics.ReciprocalRankSum.ShouldBe(
                expected.ReciprocalRankSum,
                EvaluationBaselineFixture.Tolerance),
            metrics => metrics.Mrr.ShouldBe(expected.Mrr, EvaluationBaselineFixture.Tolerance));
    }

    [Fact]
    public void FeaturesFor_DisabledMode_StripsSignalsAndUsesBaseCast()
    {
        // act
        var features = LoadEnriched().Catalog.Select(movie => movie.FeaturesFor(TmdbSignalsMode.Disabled));

        // assert
        foreach (var movie in features)
        {
            movie.Keywords.ShouldBeEmpty();
            movie.CollectionId.ShouldBeNull();
            movie.Cast.ShouldBeEmpty();
        }
    }

    [Fact]
    public void FeaturesFor_EnabledMode_AddsSignalsAndEffectiveCast()
    {
        // arrange
        var catalog = LoadEnriched().Catalog;
        var first = catalog[0];

        // act
        var enabled = first.FeaturesFor(TmdbSignalsMode.Enabled);

        // assert
        enabled.Keywords.ShouldBe([1001, 1002], ignoreOrder: true);
        enabled.CollectionId.ShouldBe(101);
        enabled.Cast.ShouldBe(first.TmdbFullCast, ignoreOrder: true);
        enabled.Genres.ShouldBe(first.Features.Genres);
        enabled.FullPlot.ShouldBe(first.Features.FullPlot);
    }

    [Fact]
    public void EnrichedFixture_CarriesNonzeroKeywordAndCollectionCoverage()
    {
        // act
        var catalog = LoadEnriched().Catalog;

        // assert
        catalog.ShouldAllBe(movie => movie.TmdbKeywords.Count > 0);
        catalog.ShouldAllBe(movie => movie.CollectionId > 0);
        catalog.ShouldAllBe(movie => movie.TmdbFullCast.Count > 0);

        var enabled = catalog.Select(movie => movie.FeaturesFor(TmdbSignalsMode.Enabled)).ToArray();
        enabled.SelectMany(movie => movie.Keywords).Distinct().Count().ShouldBe(8);
        enabled.Select(movie => movie.CollectionId).Distinct().Count().ShouldBe(4);
        enabled.SelectMany(movie => movie.Cast).Distinct().Count().ShouldBe(8);
    }

    [Fact]
    public void Run_EnabledMode_KeepsTheIdenticalNonzeroDenominatorAndDoesNotLoseHits()
    {
        // arrange
        var fixture = LoadEnriched();

        // act
        var disabled = EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(Seed));
        var enabled = EvaluationReplay.Run(
            fixture,
            EvaluationReplayOptions.Default(Seed, nameof(TmdbSignalsMode.Enabled)));

        // assert
        enabled.Metrics.ObservationCount.ShouldBe(disabled.Metrics.ObservationCount);
        enabled.Metrics.ObservationCount.ShouldBeGreaterThan(0);
        enabled.Metrics.HitCount.ShouldBeGreaterThanOrEqualTo(disabled.Metrics.HitCount);
        enabled.Metrics.HitRateAt10.ShouldBe(
            (double)enabled.Metrics.HitCount / enabled.Metrics.ObservationCount);
        enabled.Observations.Select(observation => observation.EventId)
            .ShouldBe(disabled.Observations.Select(observation => observation.EventId));
    }

    [Fact]
    public void SimilarityScorer_ExercisesKeywordAndCollectionTermsIndependentlyWithPositiveWeights()
    {
        // arrange
        var anchor = ReplayFixtureFactory.SignalMovie(
            1,
            ["Drama"],
            keywords: [1001],
            fullCast: ["Actor One"],
            collectionId: 201);
        var castTwin = ReplayFixtureFactory.SignalMovie(
            2,
            ["Comedy"],
            keywords: [1002],
            fullCast: ["Actor One"],
            collectionId: 202);
        var collectionTwin = ReplayFixtureFactory.SignalMovie(
            3,
            ["Horror"],
            keywords: [3001],
            fullCast: ["Actor Two"],
            collectionId: 201);
        var keywordTwin = ReplayFixtureFactory.SignalMovie(
            4,
            ["Thriller"],
            keywords: [1001],
            fullCast: ["Actor Three"],
            collectionId: 203);
        var genreWeights = AsOfHistory.CatalogGenreWeights(
            [anchor, castTwin, collectionTwin, keywordTwin]);

        var noWeights = new SimilarityWeights
        {
            Director = 0.0,
            Writers = 0.0,
            Cast = 0.0,
            Genres = 0.0,
            YearProximity = 0.0,
            RuntimeProximity = 0.0,
            RatingBoost = 0.0,
            Keyword = 0.0,
            Collection = 0.0,
        };
        var keywordOnly = noWeights with { Keyword = 1.0 };
        var collectionOnly = noWeights with { Collection = 2.0 };
        var castOnly = noWeights with { Cast = 2.0 };

        // act / assert
        Score(anchor, keywordTwin, keywordOnly, genreWeights).ShouldBe(1.0);
        Score(anchor, castTwin, keywordOnly, genreWeights).ShouldBe(0.0);
        Score(anchor, keywordTwin, noWeights, genreWeights).ShouldBe(0.0);

        Score(anchor, collectionTwin, collectionOnly, genreWeights).ShouldBe(1.0);
        Score(anchor, castTwin, collectionOnly, genreWeights).ShouldBe(0.0);
        Score(anchor, collectionTwin, noWeights, genreWeights).ShouldBe(0.0);

        Score(anchor, castTwin, castOnly, genreWeights).ShouldBe(1.0);
    }

    [Fact]
    public void Options_Default_ParsesTmdbSignalsStrictly()
    {
        // act / assert
        EvaluationReplayOptions.Default(Seed).TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
        EvaluationReplayOptions.Default(Seed, null).TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
        EvaluationReplayOptions.Default(Seed, string.Empty).TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
        EvaluationReplayOptions.Default(Seed, "Disabled").TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
        EvaluationReplayOptions.Default(Seed, "Enabled").TmdbSignals.ShouldBe(TmdbSignalsMode.Enabled);

        foreach (var invalid in new[] { "enabled", "ENABLED", "On", "1", " Enabled", "Enabled " })
        {
            var failure = Should.Throw<InvalidOperationException>(
                () => EvaluationReplayOptions.Default(Seed, invalid));
            failure.Message.ShouldContain("Enabled");
        }
    }

    [Fact]
    public void Entry_ResolveOptions_ReadsOnlyTheChildTransportVariable()
    {
        // act
        var unset = EvaluationEntry.ResolveOptions(_ => null, Seed);
        var enabled = EvaluationEntry.ResolveOptions(
            name => name == EvaluationEntry.TmdbSignalsVariable ? "Enabled" : null,
            Seed);
        var unrelated = EvaluationEntry.ResolveOptions(
            name => name == "SOME_OTHER_VARIABLE" ? "Enabled" : null,
            Seed);

        // assert
        unset.TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
        enabled.TmdbSignals.ShouldBe(TmdbSignalsMode.Enabled);
        unrelated.TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
        enabled.Seed.ShouldBe(Seed);

        Should.Throw<InvalidOperationException>(
            () => EvaluationEntry.ResolveOptions(
                name => name == EvaluationEntry.TmdbSignalsVariable ? "sideways" : null,
                Seed));
    }

    [Fact]
    public void Options_ExplicitConstruction_IsEnvironmentIndependent()
    {
        // act
        var explicitOptions = new EvaluationReplayOptions(
            Seed,
            _ => new FixedUtcTimeProvider(Now),
            TmdbSignalsMode.Enabled);

        // assert
        explicitOptions.TmdbSignals.ShouldBe(TmdbSignalsMode.Enabled);
        explicitOptions.Seed.ShouldBe(Seed);
        EvaluationReplayOptions.Default(Seed).TmdbSignals.ShouldBe(TmdbSignalsMode.Disabled);
    }

    [Fact]
    public void Run_EnabledMode_FutureEventsLeaveEarlierObservationsUnchanged()
    {
        // arrange
        var catalog = ImmutableArray.Create(
            ReplayFixtureFactory.SignalMovie(1, ["Drama"], keywords: [1001], collectionId: 201),
            ReplayFixtureFactory.SignalMovie(2, ["Drama"], keywords: [1001], collectionId: 201),
            ReplayFixtureFactory.SignalMovie(3, ["Comedy"], keywords: [2001], collectionId: 202));
        var baselineEvents = new[]
        {
            ReplayFixtureFactory.Event("e1", "u1", 1, dayOffset: 0),
            ReplayFixtureFactory.Event("e2", "u2", 2, dayOffset: 1),
        };
        var futureEvents = new[]
        {
            ReplayFixtureFactory.Event("f1", "u1", 3, dayOffset: 2),
            ReplayFixtureFactory.Event("f2", "u2", 3, dayOffset: 3),
        };
        var baselineFixture = ReplayFixtureFactory.Fixture(baselineEvents, catalog);
        var extendedFixture = ReplayFixtureFactory.Fixture([.. baselineEvents, .. futureEvents], catalog);

        // act
        var baseline = EvaluationReplay.Run(baselineFixture, ReplayFixtureFactory.EnabledOptions(Seed));
        var extended = EvaluationReplay.Run(extendedFixture, ReplayFixtureFactory.EnabledOptions(Seed));

        // assert
        extended.Observations.Length.ShouldBe(4);
        for (var index = 0; index < baseline.Observations.Length; index++)
        {
            var expected = baseline.Observations[index];
            var actual = extended.Observations[index];

            actual.EventId.ShouldBe(expected.EventId);
            actual.Ranking.ShouldBe(expected.Ranking);
            actual.Score.ShouldBe(expected.Score);
        }
    }

    [Fact]
    public void Provenance_SourcesRecordScrubbedEndpointsAndCanonicalPayloadDigests()
    {
        // arrange
        var fixture = LoadEnriched();
        var sourcesText = ReadFixtureFile("baseline.tmdb.sources.json");

        // act
        using var document = JsonDocument.Parse(sourcesText);
        var root = document.RootElement;
        var movies = root.GetProperty("movies").EnumerateArray().ToArray();

        // assert
        sourcesText.ShouldNotContain("api_key");
        sourcesText.ShouldNotContain("bearer");
        root.GetProperty("fixtureId").GetString().ShouldBe(fixture.FixtureId);
        root.GetProperty("provenance").GetString().ShouldBe("offline-q7-contract");
        movies.Length.ShouldBe(fixture.Catalog.Length);

        for (var index = 0; index < movies.Length; index++)
        {
            var movie = fixture.Catalog[index];
            var source = movies[index];
            var tmdbMovieId = 1000 + movie.MovieId.Value;
            var payloads = CanonicalPayloads(movie, tmdbMovieId);

            source.GetProperty("movieId").GetInt32().ShouldBe(movie.MovieId.Value);
            source.GetProperty("tmdbMovieId").GetInt32().ShouldBe(tmdbMovieId);

            var endpoints = source.GetProperty("endpoints");
            endpoints.GetProperty("details").GetString().ShouldBe($"/3/movie/{tmdbMovieId}");
            endpoints.GetProperty("keywords").GetString().ShouldBe($"/3/movie/{tmdbMovieId}/keywords");
            endpoints.GetProperty("credits").GetString().ShouldBe($"/3/movie/{tmdbMovieId}/credits");

            source.GetProperty("detailsSha256").GetString().ShouldBe(Sha256(payloads.Details));
            source.GetProperty("keywordsSha256").GetString().ShouldBe(Sha256(payloads.Keywords));
            source.GetProperty("creditsSha256").GetString().ShouldBe(Sha256(payloads.Credits));
        }
    }

    [Fact]
    public void Provenance_CanonicalPayloadsNormalizeToTheEnrichedFixtureSignals()
    {
        // arrange
        var fixture = LoadEnriched();

        // act / assert
        foreach (var movie in fixture.Catalog)
        {
            var payloads = CanonicalPayloads(movie, 1000 + movie.MovieId.Value);

            using var details = JsonDocument.Parse(payloads.Details);
            using var keywords = JsonDocument.Parse(payloads.Keywords);
            using var credits = JsonDocument.Parse(payloads.Credits);

            details.RootElement.GetProperty("id").GetInt32().ShouldBe(1000 + movie.MovieId.Value);
            details.RootElement.GetProperty("belongs_to_collection").GetProperty("id").GetInt32()
                .ShouldBe(movie.CollectionId ?? 0);

            var payloadKeywords = keywords.RootElement.GetProperty("keywords")
                .EnumerateArray()
                .Select(item => item.GetProperty("id").GetInt32())
                .ToArray();
            payloadKeywords.ShouldBe([.. movie.TmdbKeywords.Order()]);

            var payloadCast = credits.RootElement.GetProperty("cast")
                .EnumerateArray()
                .Select(item => item.GetProperty("name").GetString())
                .ToArray();
            payloadCast.ShouldBe([.. movie.TmdbFullCast.Order(StringComparer.Ordinal)]);
        }
    }

    private static double Score(
        EvaluationCatalogMovie first,
        EvaluationCatalogMovie second,
        SimilarityWeights weights,
        GenreWeights genreWeights) =>
        SimilarityScorer.Score(
            first.FeaturesFor(TmdbSignalsMode.Enabled),
            second.FeaturesFor(TmdbSignalsMode.Enabled),
            weights,
            genreWeights).Score;

    private static EvaluationFixture LoadEnriched() =>
        EvaluationFixtureLoader.LoadFromJson(
            ReadFixtureFile("baseline.tmdb.json"),
            new FixedUtcTimeProvider(Now));

    private static string ReadFixtureFile(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));

    private static (string Details, string Keywords, string Credits) CanonicalPayloads(
        EvaluationCatalogMovie movie,
        int tmdbMovieId)
    {
        var genre = movie.Features.Genres.Single();
        var collectionId = movie.CollectionId
            ?? throw new InvalidOperationException("The enriched fixture declares a collection id.");
        var cluster = collectionId - 101;

        var keywordEntries = string.Join(
            ",",
            movie.TmdbKeywords
                .Order()
                .Select(keywordId => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{{\"id\":{keywordId},\"name\":\"{genre} Keyword {keywordId}\"}}")));

        var castEntries = string.Join(
            ",",
            movie.TmdbFullCast
                .Order(StringComparer.Ordinal)
                .Select((name, order) => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{{\"id\":{100000 + (cluster * 10) + order},\"name\":\"{name}\",\"order\":{order}}}")));

        return (
            string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"id\":{tmdbMovieId},\"belongs_to_collection\":{{\"id\":{collectionId},\"name\":\"{genre} Collection\"}}}}"),
            string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"id\":{tmdbMovieId},\"keywords\":[{keywordEntries}]}}"),
            string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"id\":{tmdbMovieId},\"cast\":[{castEntries}]}}"));
    }

    private static string Sha256(string payload) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
}
