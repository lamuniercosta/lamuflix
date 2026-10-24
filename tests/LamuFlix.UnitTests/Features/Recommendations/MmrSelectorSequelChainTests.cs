using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MmrSelectorSequelChainTests
{
    private const double Lambda = 0.7;
    private const int Take = 10;

    private static readonly int[] SequelIds = [1, 2, 3, 4, 5, 6];
    private static readonly double[] SequelRelevances = [0.90, 0.89, 0.88, 0.87, 0.86, 0.85];
    private static readonly int[] OutsiderIds = [10, 11, 12, 13, 14];

    private static readonly SimilarityWeights GenreOnlyWeights = new() { Genres = 1.0 };
    private static readonly GenreWeights UniformGenreWeights = GenreWeights.Calculate(
        new GenreCounts(0, new Dictionary<string, long>()));

    [Fact]
    public void RelevanceBaseline_FirstFiveResults_AreSequels()
    {
        // arrange
        var pool = BuildFixture();

        // act
        var baseline = pool
            .OrderByDescending(candidate => candidate.Relevance)
            .ThenBy(candidate => candidate.MovieId.Value)
            .Take(5)
            .Select(candidate => candidate.MovieId.Value)
            .ToArray();

        // assert
        baseline.ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void Select_LambdaPointSevenTakeTen_ProducesRatifiedDiversifiedOrder()
    {
        // arrange
        var pool = BuildFixture();
        int[] expected = [1, 10, 11, 12, 13, 14, 2, 3, 4, 5];

        // act
        var result = MmrSelector.Select(pool, Take, Lambda, GenreOnlyWeights, UniformGenreWeights)
            .Select(id => id.Value)
            .ToArray();

        // assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Select_DiversifiedResult_CardinalityUniquenessAndOutsiderCoverage()
    {
        // arrange
        var pool = BuildFixture();

        // act
        var result = MmrSelector.Select(pool, Take, Lambda, GenreOnlyWeights, UniformGenreWeights)
            .Select(id => id.Value)
            .ToArray();
        var sequelIdSet = SequelIds.ToHashSet();
        var firstFive = result.Take(5).ToArray();

        // assert
        result.Length.ShouldBe(10);
        result.Distinct().Count().ShouldBe(10);
        result.Count(id => OutsiderIds.Contains(id)).ShouldBeGreaterThanOrEqualTo(1);
        firstFive.All(sequelIdSet.Contains).ShouldBeFalse();
        firstFive.ShouldBe([1, 10, 11, 12, 13]);
    }

    [Fact]
    public void StepTwoMarginalObjectives_MatchDocumentedOutsiderAndNextSequelScores()
    {
        // arrange
        var pool = BuildFixture();
        var firstSequel = CandidateFor(pool, 1);
        var outsider = CandidateFor(pool, 10);
        var nextSequel = CandidateFor(pool, 2);

        // act
        var outsiderSimilarity = Similarity(outsider, firstSequel);
        var sequelSimilarity = Similarity(nextSequel, firstSequel);
        var outsiderObjective = Objective(outsider.Relevance, outsiderSimilarity);
        var sequelObjective = Objective(nextSequel.Relevance, sequelSimilarity);

        // assert
        outsiderSimilarity.ShouldBe(0.0);
        sequelSimilarity.ShouldBe(1.0);
        outsiderObjective.ShouldBe(0.56, 1e-12);
        sequelObjective.ShouldBe(0.323, 1e-12);
        outsiderObjective.ShouldBeGreaterThan(sequelObjective);
    }

    private static List<MmrCandidate> BuildFixture()
    {
        var pool = new List<MmrCandidate>(SequelIds.Length + OutsiderIds.Length);

        for (var index = 0; index < SequelIds.Length; index++)
        {
            pool.Add(new MmrCandidate(
                new MovieId(SequelIds[index]),
                SequelRelevances[index],
                new MovieFeatures(genres: ["Franchise"])));
        }

        for (var index = 0; index < OutsiderIds.Length; index++)
        {
            pool.Add(new MmrCandidate(
                new MovieId(OutsiderIds[index]),
                0.80,
                new MovieFeatures(genres: [$"Outsider-{(char)('A' + index)}"])));
        }

        return pool;
    }

    private static MmrCandidate CandidateFor(IEnumerable<MmrCandidate> pool, int movieId) =>
        pool.Single(candidate => candidate.MovieId.Value == movieId);

    private static double Similarity(MmrCandidate candidate, MmrCandidate selected) =>
        SimilarityScorer.Score(
            candidate.Features,
            selected.Features,
            GenreOnlyWeights,
            UniformGenreWeights).Score;

    private static double Objective(double relevance, double maximumSimilarity) =>
        (Lambda * relevance) - ((1.0 - Lambda) * maximumSimilarity);
}
