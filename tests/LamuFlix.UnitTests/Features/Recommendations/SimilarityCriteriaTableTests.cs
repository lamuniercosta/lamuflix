using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityCriteriaTableTests
{
    [Fact]
    public void Score_DefaultsAndEvidence_MatchTheSixOrderedNormativeRows()
    {
        // arrange
        var options = new RecommendationOptions();
        var first = new MovieFeatures(
            genres: ["Drama"], directors: ["Director"], cast: ["Actor A", "Actor B"],
            collectionId: 7, collectionName: "Collection", keywordNames: ["theme"], plotEmbedding: [1f, 0f]);
        var second = new MovieFeatures(
            genres: ["Drama"], directors: ["Director"], cast: ["Actor A", "Actor B"],
            collectionId: 7, collectionName: "Collection", keywordNames: ["theme"], plotEmbedding: [1f, 0f]);
        var genres = GenreWeights.Calculate(new GenreCounts(100, new Dictionary<string, long> { ["Drama"] = 10 }));
        var keywords = new KeywordCounts(10, new Dictionary<string, long> { ["theme"] = 2 });

        // act
        var result = SimilarityScorer.Score(first, second, options, genres, keywords);

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(["Collection", "Director", "Themes", "Plot", "Cast", "Genre"]);
        result.Breakdown.Select(entry => entry.Weight).ShouldBe([3.0, 3.0, 3.0, 2.0, 2.0, 2.0]);
        result.Breakdown.Select(entry => entry.Value).ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.Breakdown.Select(entry => entry.Evidence).ShouldBe([
            ImmutableList.Create("Collection"), ImmutableList.Create("Director"), ImmutableList.Create("theme"),
            ImmutableList<string>.Empty, ImmutableList.Create("Actor A", "Actor B"), ImmutableList.Create("Drama"),
        ]);
        result.Breakdown.Select(entry => entry.Contribution).Sum().ShouldBe(result.Score, 1e-12);
        result.Score.ShouldBe(1.0, 1e-12);
        result.Reasons.Select(reason => reason.Text).ShouldBe([
            "Same collection: Collection", "Same director: Director", "Shared themes: theme",
            "Similar story", "Shares cast: Actor A, Actor B", "Same genres: Drama",
        ]);
    }

    [Fact]
    public void Score_MissingFeatures_EmitsSixZeroRowsAndKeepsDefaultDenominator()
    {
        // arrange
        var movie = new MovieFeatures();
        var genres = GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));
        var keywords = new KeywordCounts(0, new Dictionary<string, long>());

        // act
        var result = SimilarityScorer.Score(movie, movie, new RecommendationOptions(), genres, keywords);

        // assert
        result.Breakdown.Count.ShouldBe(6);
        result.Breakdown.ShouldAllBe(row => row.Value == 0.0 && row.Contribution == 0.0);
        result.Score.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_CollectionOnlyMatch_UsesEveryConfiguredWeightInDenominator()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights { Collection = 1.0, Director = 1.0, Themes = 0.0, Plot = 0.0, Cast = 0.0, Genres = 0.0 },
        };
        var first = new MovieFeatures(collectionId: 3, collectionName: "Saga");
        var second = new MovieFeatures(collectionId: 3, collectionName: "Saga");
        var genres = GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));
        var keywords = new KeywordCounts(0, new Dictionary<string, long>());

        // act
        var result = SimilarityScorer.Score(first, second, options, genres, keywords);

        // assert
        result.Breakdown.Select(entry => entry.Contribution).Sum().ShouldBe(0.5, 1e-12);
        result.Score.ShouldBe(0.5, 1e-12);
    }

    [Fact]
    public void Score_SingleSharedActor_UsesHalfCastValue()
    {
        // arrange
        var first = new MovieFeatures(cast: ["Actor"]);
        var second = new MovieFeatures(cast: ["Actor"]);
        var genres = GenreWeights.Calculate(new GenreCounts(10, new Dictionary<string, long> { ["Drama"] = 2 }));
        var keywords = new KeywordCounts(10, new Dictionary<string, long>());

        // act
        var result = SimilarityScorer.Score(first, second, new RecommendationOptions(), genres, keywords);

        // assert
        result.Breakdown[4].Value.ShouldBe(0.5);
        result.Breakdown[2].Value.ShouldBe(0.0);
    }

    [Fact]
    public void Score_GenreOnlyMatch_IsBoundedByTwoFifteenths()
    {
        // arrange
        var first = new MovieFeatures(genres: ["Drama"]);
        var second = new MovieFeatures(genres: ["Drama"]);
        var genres = GenreWeights.Calculate(new GenreCounts(10, new Dictionary<string, long> { ["Drama"] = 2 }));
        var keywords = new KeywordCounts(10, new Dictionary<string, long>());

        // act
        var result = SimilarityScorer.Score(first, second, new RecommendationOptions(), genres, keywords);

        // assert
        result.Breakdown[5].Contribution.ShouldBe(2.0 / 15.0, 1e-12);
        result.Score.ShouldBeLessThanOrEqualTo(2.0 / 15.0);
    }

    [Fact]
    public void Score_Themes_TrimsDeduplicatesAndExcludesStoplistBeforeBuildingEvidence()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights { Collection = 0, Director = 0, Themes = 1, Plot = 0, Cast = 0, Genres = 0 },
            ThemesDisplayThreshold = 1.0,
        };
        var first = new MovieFeatures(keywordNames: [" Theme ", "theme", "SEQUEL"]);
        var second = new MovieFeatures(keywordNames: ["THEME", "sequel"]);
        var genres = GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));
        var keywords = new KeywordCounts(10, new Dictionary<string, long> { ["theme"] = 2, ["sequel"] = 2 });

        // act
        var result = SimilarityScorer.Score(first, second, options, genres, keywords);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
        result.Breakdown[2].Evidence.ShouldBe(["theme"]);
        result.Reasons.Single().Text.ShouldBe("Shared themes: theme");
    }

    [Fact]
    public void Score_ExplicitNullThemeStoplist_AllowsOtherwiseStoplistedTerms()
    {
        // arrange
        var options = new RecommendationOptions
        {
            ThemeStoplist = null,
            Weights = new SimilarityWeights { Collection = 0, Director = 0, Themes = 1, Plot = 0, Cast = 0, Genres = 0 },
        };
        var movie = new MovieFeatures(keywordNames: ["sequel"]);
        var genres = GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));
        var keywords = new KeywordCounts(10, new Dictionary<string, long> { ["sequel"] = 2 });

        // act
        var result = SimilarityScorer.Score(movie, movie, options, genres, keywords);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
        result.Breakdown[2].Evidence.ShouldBe(["sequel"]);
    }

    [Fact]
    public void Score_UbiquitousSharedKeyword_HasZeroNormAndNoReason()
    {
        // arrange
        var movie = new MovieFeatures(keywordNames: ["ubiquitous"]);
        var genres = GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));
        var keywords = new KeywordCounts(10, new Dictionary<string, long> { ["ubiquitous"] = 10 });

        // act
        var result = SimilarityScorer.Score(movie, movie, new RecommendationOptions(), genres, keywords);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Breakdown[2].Contribution.ShouldBe(0.0);
        result.Breakdown[2].Evidence.ShouldBe(["ubiquitous"]);
        result.Reasons.ShouldBeEmpty();
    }
}