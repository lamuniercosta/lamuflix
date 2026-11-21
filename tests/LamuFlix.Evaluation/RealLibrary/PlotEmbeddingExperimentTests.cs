using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class PlotEmbeddingExperimentTests
{
    [Fact]
    public void Compose_BaselineVariant_ReturnsPlotOnly()
    {
        // arrange
        var movie = MovieWithSignals(
            title: "About Time",
            synopsis: "A young man travels through time.",
            genres: ["Comedy", "Drama"],
            keywords: [Keyword(1, "time travel"), Keyword(2, "love")]);

        // act
        var input = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.BaselinePlot, Weights());

        // assert
        input.ShouldBe("A young man travels through time.");
    }

    [Fact]
    public void Compose_AlternateModelVariant_ReturnsSamePlotOnlyInputAsBaseline()
    {
        // arrange
        var movie = MovieWithSignals(
            title: "About Time",
            synopsis: "A young man travels through time.",
            genres: ["Comedy"],
            keywords: [Keyword(1, "time travel")]);

        // act
        var alternate = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.AlternateModel, Weights());
        var baseline = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.BaselinePlot, Weights());

        // assert
        alternate.ShouldBe(baseline);
    }

    [Fact]
    public void Compose_EnrichedVariant_OrdersTitleGenresKeywordsThenPlot()
    {
        // arrange
        var movie = MovieWithSignals(
            title: "About Time",
            synopsis: "A young man travels through time.",
            genres: ["Romance", "Comedy"],
            keywords: [Keyword(1, "time travel"), Keyword(2, "love")]);
        var weights = Weights(
            ("time travel", 2L, 10L),
            ("love", 3L, 10L));

        // act
        var input = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.EnrichedText, weights);

        // assert
        input.ShouldBe(
            "Title: About Time"
            + "\nGenres: Comedy, Romance"
            + "\nKeywords: time travel, love"
            + "\nPlot: A young man travels through time.");
    }

    [Fact]
    public void Compose_EnrichedVariant_CapsKeywordsAtThreeByDescendingRarity()
    {
        // arrange
        var movie = MovieWithSignals(
            title: "Ensemble",
            synopsis: "Plot.",
            genres: ["Drama"],
            keywords: [Keyword(1, "common"), Keyword(2, "rare-a"), Keyword(3, "rare-b"), Keyword(4, "mid")]);
        var weights = Weights(
            ("common", 9L, 10L),
            ("mid", 4L, 10L),
            ("rare-a", 2L, 10L),
            ("rare-b", 2L, 10L));

        // act
        var input = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.EnrichedText, weights);

        // assert
        input.ShouldContain("\nKeywords: rare-a, rare-b, mid\nPlot: ");
    }

    [Fact]
    public void Compose_EnrichedVariant_BreaksRarityTiesByOrdinalKeywordName()
    {
        // arrange
        var movie = MovieWithSignals(
            title: "Ties",
            synopsis: "Plot.",
            genres: ["Drama"],
            keywords: [Keyword(1, "beta"), Keyword(2, "Alpha")]);
        var weights = Weights(
            ("beta", 2L, 10L),
            ("Alpha", 2L, 10L));

        // act
        var input = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.EnrichedText, weights);

        // assert
        input.ShouldContain("\nKeywords: Alpha, beta\nPlot: ");
    }

    [Fact]
    public void Compose_EnrichedVariant_ExcludesStoplistAndSingleMovieKeywords()
    {
        // arrange
        var movie = MovieWithSignals(
            title: "Filtered",
            synopsis: "Plot.",
            genres: ["Drama"],
            keywords: [Keyword(1, "sequel"), Keyword(2, "lonely"), Keyword(3, "kept")]);
        var counts = new KeywordCounts(10L, new Dictionary<string, long>
        {
            ["sequel"] = 5L,
            ["lonely"] = 1L,
            ["kept"] = 3L,
        });
        var weights = SimilarityScorer.PrepareKeywordWeights(new RecommendationOptions(), counts);

        // act
        var input = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.EnrichedText, weights);

        // assert
        input.ShouldContain("\nKeywords: kept\nPlot: ");
    }

    [Fact]
    public void Compose_MovieWithoutMetadata_ReturnsEmptyInput()
    {
        // arrange
        var movie = Movie.Create(new MovieId(7), "Untitled", new LibraryPath("movies/untitled"), new MediaFormat("mkv"));

        // act
        var baseline = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.BaselinePlot, Weights());
        var enriched = EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.EnrichedText, Weights());

        // assert
        baseline.ShouldBe(string.Empty);
        enriched.ShouldBe(
            "Title: Untitled"
            + "\nGenres: "
            + "\nKeywords: "
            + "\nPlot: ");
    }

    [Fact]
    public void Compose_EnrichedVariantWithoutWeights_Throws()
    {
        // arrange
        var movie = MovieWithSignals("T", "P", ["Drama"], [Keyword(1, "kept")]);

        // act
        Action action = () => EmbeddingInputComposer.Compose(movie, PlotEmbeddingVariant.EnrichedText, null);

        // assert
        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Compose_UnknownVariant_Throws()
    {
        // arrange
        var movie = MovieWithSignals("T", "P", ["Drama"], [Keyword(1, "kept")]);

        // act
        Action action = () => EmbeddingInputComposer.Compose(movie, (PlotEmbeddingVariant)99, Weights());

        // assert
        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SelectKeywords_RespectsCapAndOrdersByRarityThenName()
    {
        // arrange
        var weights = Weights(
            ("common", 9L, 10L),
            ("mid", 4L, 10L),
            ("rare-b", 2L, 10L),
            ("rare-a", 2L, 10L));

        // act
        var selected = EmbeddingInputComposer.SelectKeywords(
            ["common", "mid", "rare-b", "rare-a"],
            weights,
            cap: 2);

        // assert
        selected.ShouldBe(["rare-a", "rare-b"]);
    }

    [Fact]
    public void SelectKeywords_NonPositiveCap_Throws()
    {
        // act
        Action action = () => EmbeddingInputComposer.SelectKeywords(["kept"], Weights(), cap: 0);

        // assert
        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Preflight_AllPrerequisitesMissing_ReportsCouldNotRunWithEachPrerequisite()
    {
        // act
        var result = PlotEmbeddingPreflight.Check(LocalEndpoint(), _ => false, ScratchConnection(), () => false);

        // assert
        result.CanRun.ShouldBeFalse();
        result.MissingPrerequisites.ShouldBe(
        [
            "Ollama model 'all-minilm' is not served at the configured endpoint.",
            "Ollama model 'nomic-embed-text:v1.5' is not served at the configured endpoint.",
            "Scratch evaluation store is unreachable.",
        ]);
    }

    [Fact]
    public void Preflight_AllPrerequisitesPresent_CanRunWithNoMissingPrerequisites()
    {
        // act
        var result = PlotEmbeddingPreflight.Check(LocalEndpoint(), _ => true, ScratchConnection(), () => true);

        // assert
        result.CanRun.ShouldBeTrue();
        result.MissingPrerequisites.ShouldBeEmpty();
    }

    [Fact]
    public void Preflight_OnlyBaselineModelMissing_ListsOnlyThatPrerequisite()
    {
        // act
        var result = PlotEmbeddingPreflight.Check(LocalEndpoint(), model => model != "all-minilm", ScratchConnection(), () => true);

        // assert
        result.CanRun.ShouldBeFalse();
        result.MissingPrerequisites.ShouldBe(
            ["Ollama model 'all-minilm' is not served at the configured endpoint."]);
    }

    [Fact]
    public void Preflight_NonLocalEndpoint_SkipsProbesAndRefusesReadiness()
    {
        // arrange
        var probes = 0;

        // act
        var result = PlotEmbeddingPreflight.Check(
            "http://ollama.example.com:11434/",
            _ =>
            {
                probes++;
                return true;
            },
            ScratchConnection(),
            () => true);

        // assert
        result.CanRun.ShouldBeFalse();
        result.MissingPrerequisites.ShouldContain(PlotEmbeddingPreflight.NonLocalEndpointMessage);
        probes.ShouldBe(0);
    }

    [Fact]
    public void Preflight_NonScratchConnection_RefusesReadinessWithoutProbingStore()
    {
        // arrange
        var probes = 0;

        // act
        var result = PlotEmbeddingPreflight.Check(
            LocalEndpoint(),
            _ => true,
            "Server=prod-db;Database=LamuFlix;",
            () =>
            {
                probes++;
                return true;
            });

        // assert
        result.CanRun.ShouldBeFalse();
        result.MissingPrerequisites.ShouldContain(PlotEmbeddingPreflight.ScratchIdentityMessage);
        probes.ShouldBe(0);
    }

    [Theory]
    [InlineData("http://localhost:11434/")]
    [InlineData("http://127.0.0.1:11434/")]
    [InlineData("http://[::1]:11434/")]
    public void EndpointPolicy_LocalEndpoints_AreAccepted(string endpoint)
    {
        // act
        var local = PlotEmbeddingEndpointPolicy.IsLocalEndpoint(endpoint);

        // assert
        local.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://ollama.example.com:11434/")]
    [InlineData("http://192.168.1.20:11434/")]
    [InlineData("not-a-uri")]
    [InlineData(null)]
    public void EndpointPolicy_NonLocalEndpoints_AreRejected(string? endpoint)
    {
        // act
        var local = PlotEmbeddingEndpointPolicy.IsLocalEndpoint(endpoint);

        // assert
        local.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Server=scratch-db;Database=LamuFlixEvaluation;")]
    [InlineData("Host=localhost;Database=lamuflix_evaluation;")]
    [InlineData("Data Source=:memory:;")]
    public void ScratchPolicy_ScratchConnections_AreAccepted(string connection)
    {
        // act
        var scratch = PlotEmbeddingScratchPolicy.IsScratchConnectionString(connection);

        // assert
        scratch.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Server=prod-db;Database=LamuFlix;")]
    [InlineData(null)]
    public void ScratchPolicy_ProductionConnections_AreRejected(string? connection)
    {
        // act
        var scratch = PlotEmbeddingScratchPolicy.IsScratchConnectionString(connection);

        // assert
        scratch.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(256)]
    public void TokenBudget_AtOrBelowLimit_IsWithinLimit(int tokens)
    {
        // act
        var within = PlotEmbeddingTokenBudget.IsWithinLimit(tokens);

        // assert
        within.ShouldBeTrue();
    }

    [Theory]
    [InlineData(257)]
    [InlineData(-1)]
    public void TokenBudget_AboveLimitOrNegative_IsNotWithinLimit(int tokens)
    {
        // act
        var within = PlotEmbeddingTokenBudget.IsWithinLimit(tokens);

        // assert
        within.ShouldBeFalse();
    }

    [Fact]
    public void Experiment_PinsVariantModelNamesKeywordCapAndTokenLimit()
    {
        // assert
        PlotEmbeddingExperiment.BaselineModelName.ShouldBe("all-minilm");
        PlotEmbeddingExperiment.AlternateModelName.ShouldBe("nomic-embed-text:v1.5");
        PlotEmbeddingExperiment.EnrichedKeywordCap.ShouldBe(3);
        PlotEmbeddingExperiment.ProposedTokenLimit.ShouldBe(256);
    }

    private static string LocalEndpoint() => "http://localhost:11434/";

    private static string ScratchConnection() => "Server=localhost;Database=LamuFlixEvaluationScratch;";

    private static MovieKeyword Keyword(int id, string name) => new(id, name);

    private static IReadOnlyDictionary<string, double> Weights(params (string Name, long Count, long Total)[] entries)
    {
        var counts = new Dictionary<string, long>();
        var total = 10L;

        foreach (var (name, count, entryTotal) in entries)
        {
            counts[name] = count;
            total = entryTotal;
        }

        if (entries.Length == 0)
        {
            counts["placeholder"] = total;
        }

        return SimilarityScorer.PrepareKeywordWeights(new RecommendationOptions(), new KeywordCounts(total, counts));
    }

    private static Movie MovieWithSignals(
        string title,
        string synopsis,
        string[] genres,
        MovieKeyword[] keywords)
    {
        var signals = new MovieSignalSnapshot(
            keywordAvailability: SignalAvailability.Available,
            keywords: keywords,
            fullCastAvailability: SignalAvailability.Unavailable,
            fullCast: null,
            collectionAvailability: SignalAvailability.Unavailable);
        var metadata = new MovieMetadata(
            title,
            synopsis,
            genres: genres,
            signals: signals);

        return Movie.Rehydrate(
            new MovieId(518),
            title,
            new LibraryPath("movies/about-time"),
            new MediaFormat("mkv"),
            isInWatchlist: false,
            metadata,
            EnrichmentStatus.Enriched,
            enrichedAt: null,
            enrichmentAttempts: 1,
            lastFailureCategory: null,
            lastAttemptAt: null);
    }
}
