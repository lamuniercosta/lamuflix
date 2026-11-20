using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerReasonThresholdTests
{
    private const double ThemesCeiling = 0.9;

    private static readonly string[] CriterionOrder =
        ["Collection", "Director", "Themes", "Plot", "Cast", "Genre"];

    [Fact]
    public void Score_ThemesBelowDisplayThreshold_OmitsSharedThemesReasonButKeepsContribution()
    {
        // arrange
        var first = Features(keywordNames: ["alpha", "beta"]);
        var second = Features(keywordNames: ["alpha", "gamma"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            ThemesOnlyOptions(themesThreshold: 0.6),
            EmptyGenreTable(),
            KeywordCounts());

        // assert
        Text(result).ShouldNotContain("Shared themes: alpha");
        var themes = result.Breakdown.Single(entry => entry.Name == "Themes");
        themes.Value.ShouldBe(0.5555555555555556, 1e-9);
        themes.Contribution.ShouldBeGreaterThan(0.0);
        themes.Evidence.ShouldBe(["alpha"]);
    }

    [Fact]
    public void Score_ThemesAtDisplayThreshold_KeepsSharedThemesReason()
    {
        // arrange
        var first = Features(keywordNames: ["alpha", "beta"]);
        var second = Features(keywordNames: ["alpha", "gamma"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            ThemesOnlyOptions(themesThreshold: 0.5),
            EmptyGenreTable(),
            KeywordCounts());

        // assert
        Text(result).ShouldContain("Shared themes: alpha");
    }

    [Fact]
    public void Score_PlotBelowDisplayThreshold_OmitsSimilarStoryReasonButKeepsContribution()
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f]);
        var second = Features(plotEmbedding: [0.3f, 0.9539392f]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            PlotOnlyOptions(),
            EmptyGenreTable(),
            NoKeywords());

        // assert
        Text(result).ShouldNotContain("Similar story");
        var plot = result.Breakdown.Single(entry => entry.Name == "Plot");
        plot.Value.ShouldBe(0.48275862068965514, 1e-6);
        plot.Contribution.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Score_PlotAtDisplayThreshold_KeepsSimilarStoryReason()
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f]);
        var second = Features(plotEmbedding: [0.35f, 0.9367497f]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            PlotOnlyOptions(),
            EmptyGenreTable(),
            NoKeywords());

        // assert
        Text(result).ShouldContain("Similar story");
    }

    [Fact]
    public void Score_GenreBelowDisplayThreshold_OmitsSameGenresReasonButKeepsContribution()
    {
        // arrange
        var first = Features(genres: ["Drama"]);
        var second = Features(genres: ["Drama", "Comedy", "Doc"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            GenreOnlyOptions(genreThreshold: 0.5),
            WeightedGenreTable(),
            NoKeywords());

        // assert
        Text(result).ShouldNotContain("Same genres: Drama");
        var genre = result.Breakdown.Single(entry => entry.Name == "Genre");
        genre.Value.ShouldBe(0.37590606347464106, 1e-9);
        genre.Contribution.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Score_GenreAtDisplayThreshold_KeepsSameGenresReason()
    {
        // arrange
        var first = Features(genres: ["Drama"]);
        var second = Features(genres: ["Drama", "Comedy", "Doc"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            GenreOnlyOptions(genreThreshold: 0.35),
            WeightedGenreTable(),
            NoKeywords());

        // assert
        Text(result).ShouldContain("Same genres: Drama");
    }

    [Fact]
    public void Score_ZeroDisplayThresholds_EmitReasonsForAnyPositiveValue()
    {
        // arrange
        var first = Features(genres: ["Drama"], plotEmbedding: [1f, 0f]);
        var second = Features(genres: ["Drama", "Comedy", "Doc"], plotEmbedding: [0.3f, 0.9539392f]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            new RecommendationOptions
            {
                Weights = new SimilarityWeights
                {
                    Collection = 0.0,
                    Director = 0.0,
                    Themes = 0.0,
                    Plot = 1.0,
                    Cast = 0.0,
                    Genres = 1.0,
                },
                ThemesDisplayThreshold = 0.0,
                PlotDisplayThreshold = 0.0,
                GenreDisplayThreshold = 0.0,
            },
            WeightedGenreTable(),
            NoKeywords());

        // assert
        Text(result).ShouldBe(["Similar story", "Same genres: Drama"]);
    }

    [Fact]
    public void Score_EveryCriterionAboveItsThreshold_EmitsReasonsInTableOrder()
    {
        // arrange
        var first = Features(
            genres: ["Drama"],
            directors: ["Alpha"],
            cast: ["Actor A", "Actor B"],
            keywordNames: ["alpha", "beta"],
            collectionId: 7,
            collectionName: "Saga",
            plotEmbedding: [1f, 0f]);
        var second = Features(
            genres: ["Drama"],
            directors: ["Alpha"],
            cast: ["Actor A", "Actor B"],
            keywordNames: ["alpha", "beta"],
            collectionId: 7,
            collectionName: "Saga",
            plotEmbedding: [1f, 0f]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            new RecommendationOptions
            {
                Weights = new SimilarityWeights(),
                ThemesCeiling = ThemesCeiling,
            },
            WeightedGenreTable(),
            KeywordCounts());

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(CriterionOrder);
        Text(result).ShouldBe(
        [
            "Same collection: Saga",
            "Same director: Alpha",
            "Shared themes: alpha, beta",
            "Similar story",
            "Shares cast: Actor A, Actor B",
            "Same genres: Drama",
        ]);
    }

    private static string[] Text(SimilarityResult result) =>
        [.. result.Reasons.Select(reason => reason.Text)];

    private static RecommendationOptions ThemesOnlyOptions(double themesThreshold) =>
        new()
        {
            Weights = new SimilarityWeights
            {
                Collection = 0.0,
                Director = 0.0,
                Themes = 1.0,
                Plot = 0.0,
                Cast = 0.0,
                Genres = 0.0,
            },
            ThemesCeiling = ThemesCeiling,
            ThemesDisplayThreshold = themesThreshold,
        };

    private static RecommendationOptions PlotOnlyOptions() =>
        new()
        {
            Weights = new SimilarityWeights
            {
                Collection = 0.0,
                Director = 0.0,
                Themes = 0.0,
                Plot = 1.0,
                Cast = 0.0,
                Genres = 0.0,
            },
        };

    private static RecommendationOptions GenreOnlyOptions(double genreThreshold) =>
        new()
        {
            Weights = new SimilarityWeights
            {
                Collection = 0.0,
                Director = 0.0,
                Themes = 0.0,
                Plot = 0.0,
                Cast = 0.0,
                Genres = 1.0,
            },
            GenreDisplayThreshold = genreThreshold,
        };

    private static KeywordCounts KeywordCounts() => new(
        100,
        new Dictionary<string, long>
        {
            ["alpha"] = 10,
            ["beta"] = 10,
            ["gamma"] = 10,
        });

    private static KeywordCounts NoKeywords() => new(0, new Dictionary<string, long>());

    private static GenreWeights EmptyGenreTable() =>
        GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));

    private static GenreWeights WeightedGenreTable() =>
        GenreWeights.Calculate(
            new GenreCounts(
                100,
                new Dictionary<string, long>
                {
                    ["Drama"] = 10,
                    ["Comedy"] = 18,
                    ["Doc"] = 18,
                }));

    private static MovieFeatures Features(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? cast = null,
        IEnumerable<string>? keywordNames = null,
        int? collectionId = null,
        string? collectionName = null,
        IEnumerable<float>? plotEmbedding = null) =>
        new(
            genres: genres,
            directors: directors,
            cast: cast,
            collectionId: collectionId,
            keywordNames: keywordNames,
            collectionName: collectionName,
            plotEmbedding: plotEmbedding);
}
