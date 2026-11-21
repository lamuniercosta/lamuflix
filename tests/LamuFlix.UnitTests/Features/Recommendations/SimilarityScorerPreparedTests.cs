using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Xunit;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerPreparedTests
{
    private static readonly RecommendationOptions Options = new()
    {
        MinimumSimilarity = 0.225,
        StrongReasonCriteria = ["Collection", "Director", "Themes"],
    };

    private static readonly GenreWeights Genres = GenreWeights.Calculate(
        new GenreCounts(100, new Dictionary<string, long> { ["Drama"] = 40, ["Comedy"] = 30 }));

    private static readonly IReadOnlyDictionary<string, double> Keywords =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["space"] = 1.5,
            ["SPACE"] = 2.5,
            ["heist"] = 3.0,
        };

    public static IEnumerable<object[]> Pairs()
    {
        var movies = Movies();
        foreach (var left in movies)
        {
            foreach (var right in movies)
            {
                if (!ReferenceEquals(left, right))
                {
                    yield return [left, right];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void PreparedValues_MatchFullScoreExactly(MovieFeatures left, MovieFeatures right)
    {
        // arrange
        var context = new ScoringContext(Options, Genres, Keywords);
        var preparedLeft = new PreparedMovie(left, context);
        var preparedRight = new PreparedMovie(right, context);

        // act
        var fast = SimilarityScorer.ScorePreparedValues(preparedLeft, preparedRight, context);
        var full = SimilarityScorer.ScoreWithoutValidation(left, right, Options, Genres, Keywords);

        // assert
        fast.Score.ShouldBe(full.Score);
        Value(full, "Collection").ShouldBe(fast.Collection);
        Value(full, "Director").ShouldBe(fast.Director);
        Value(full, "Themes").ShouldBe(fast.Themes);
        Value(full, "Plot").ShouldBe(fast.Plot);
        Value(full, "Cast").ShouldBe(fast.Cast);
        Value(full, "Genre").ShouldBe(fast.Genre);
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void PreparedFullResult_MatchesFullResult(MovieFeatures left, MovieFeatures right)
    {
        // arrange
        var context = new ScoringContext(Options, Genres, Keywords);

        // act
        var fast = SimilarityScorer.ScorePreparedFull(
            new PreparedMovie(left, context),
            new PreparedMovie(right, context),
            context);
        var full = SimilarityScorer.ScoreWithoutValidation(left, right, Options, Genres, Keywords);

        // assert
        fast.Score.ShouldBe(full.Score);
        fast.Reasons.Select(reason => (reason.Kind, reason.Text))
            .ShouldBe(full.Reasons.Select(reason => (reason.Kind, reason.Text)));
        fast.Breakdown.Select(entry => (entry.Name, entry.Value, entry.Weight, entry.Contribution, string.Join("|", entry.Evidence)))
            .ShouldBe(full.Breakdown.Select(entry => (entry.Name, entry.Value, entry.Weight, entry.Contribution, string.Join("|", entry.Evidence))));
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void PreparedAdmission_MatchesFullAdmission(MovieFeatures left, MovieFeatures right)
    {
        // arrange
        var context = new ScoringContext(Options, Genres, Keywords);
        var criteria = new HashSet<string>(Options.StrongReasonCriteria ?? [], StringComparer.OrdinalIgnoreCase);

        // act
        var fast = SimilarityScorer.ScorePreparedValues(
            new PreparedMovie(left, context),
            new PreparedMovie(right, context),
            context);
        var full = SimilarityScorer.ScoreWithoutValidation(left, right, Options, Genres, Keywords);

        // assert
        SimilarityScorer.IsStrongPrepared(fast, context, criteria).ShouldBe(IsStrong(full));
    }

    private static bool IsStrong(SimilarityResult similarity)
    {
        if (similarity.Score >= Options.MinimumSimilarity)
        {
            return true;
        }

        var criteria = new HashSet<string>(Options.StrongReasonCriteria ?? [], StringComparer.OrdinalIgnoreCase);
        return similarity.Breakdown.Any(contribution =>
            contribution.Weight > 0.0
            && contribution.Value >= 1.0
            && criteria.Contains(contribution.Name));
    }

    private static double Value(SimilarityResult result, string name) =>
        result.Breakdown.Single(contribution => contribution.Name == name).Value;

    private static List<MovieFeatures> Movies() =>
        [
            new(
                genres: ["Drama", "Comedy"],
                directors: ["Nolan"],
                cast: ["Bale", "Caine", "Ledger"],
                keywordNames: ["space", "heist"],
                collectionId: 7,
                collectionName: "Dark Knight",
                plotEmbedding: Vector(1.0f, 0.0f)),
            new(
                genres: ["drama "],
                directors: ["nolan "],
                cast: ["bale"],
                keywordNames: ["SPACE", "Heist"],
                collectionId: 7,
                collectionName: "Dark Knight",
                plotEmbedding: Vector(1.0f, 0.0f)),
            new(
                genres: ["Horror"],
                directors: ["Someone Else"],
                cast: ["Unknown"],
                keywordNames: ["unknown-theme"],
                collectionId: 9,
                plotEmbedding: Vector(0.0f, 1.0f)),
            new(
                genres: ["Drama"],
                directors: ["Nolan"],
                cast: ["Bale", "Caine"],
                keywordNames: ["space"],
                plotEmbedding: null),
            new(
                genres: ["Comedy"],
                keywordNames: ["heist"],
                plotEmbedding: Vector(float.NaN, 0.0f)),
            new(
                genres: ["Drama", "Comedy"],
                directors: ["Nolan"],
                cast: ["Bale", "Caine", "Ledger", "Eckhart"],
                keywordNames: ["space", "heist"],
                collectionId: 7,
                plotEmbedding: Vector(1.0f, 0.0f, 0.0f)),
            new(
                genres: ["drama", "comedy"],
                directors: ["nolan"],
                cast: ["BALE"],
                keywordNames: ["SPACE"],
                collectionId: 7,
                collectionName: "Dark Knight",
                plotEmbedding: Vector(1.0f, 0.0f)),
        ];

    private static ImmutableArray<float> Vector(params float[] values) => [.. values];
}
