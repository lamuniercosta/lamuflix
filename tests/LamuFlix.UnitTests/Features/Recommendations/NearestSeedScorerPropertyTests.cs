using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class NearestSeedScorerPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
        });

    private static readonly SimilarityWeights PlotOnlyWeights = new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 1.0,
        Cast = 0.0,
        Genres = 0.0,
        Collection = 0.0,
    };

    [Property(MaxTest = 100, Replay = "543001,100003")]
    [Trait("Category", "Property")]
    public bool Rank_PermutedInputOrder_ReturnsIdenticalRanking(int[] ids)
    {
        // arrange
        var library = Library(ids);
        var candidates = library.Where(entry => entry.Review.IsNone).ToArray();

        // act
        var forward = Rank(library, candidates);
        var reversed = Rank([.. library.Reverse()], [.. candidates.Reverse()]);

        // assert
        return forward.Count == reversed.Count
            && forward.Zip(reversed).All(pair =>
                pair.First.Candidate.Id == pair.Second.Candidate.Id
                && Math.Abs(pair.First.Score - pair.Second.Score) < 1e-12);
    }

    [Property(MaxTest = 100, Replay = "543002,200009")]
    [Trait("Category", "Property")]
    public bool Rank_AddingADislikedSeed_NeverIncreasesTheScore(int candidateSeed)
    {
        // arrange
        var candidate = Entry(Math.Abs(candidateSeed % 500) + 2, watched: false, liked: false);
        var seed = Entry(900, watched: false, liked: true);
        var without = Rank([seed, candidate], [candidate]);

        // act
        var disliked = Entry(901, watched: false, liked: false, disliked: true);
        var with = Rank([seed, disliked, candidate], [candidate]);

        // assert
        return without.Count == with.Count
            && without.Zip(with).All(pair => pair.Second.Score <= pair.First.Score + 1e-12);
    }

    [Property(MaxTest = 100, Replay = "543003,300011")]
    [Trait("Category", "Property")]
    public bool Rank_TiedCandidateScores_BreakTiesByAscendingMovieId(int firstSeed, int secondSeed)
    {
        // arrange
        var lower = Math.Abs(firstSeed % 400) + 2;
        var higher = lower + Math.Abs(secondSeed % 400) + 1;
        var seed = Entry(950, watched: false, liked: true);
        var shared = Vector(1.0);
        var lowCandidate = Entry(lower, vector: shared);
        var highCandidate = Entry(higher, vector: shared);

        // act
        var result = Rank([seed, highCandidate, lowCandidate], [highCandidate, lowCandidate]);

        // assert
        return result.Count == 2
            && result[0].Candidate.Id.Value == lower
            && result[1].Candidate.Id.Value == higher;
    }

    private static ImmutableList<NearestSeedMatch> Rank(
        IReadOnlyList<RecommendationLibraryEntry> library,
        IReadOnlyList<RecommendationLibraryEntry> candidates)
    {
        var options = new RecommendationOptions { Weights = PlotOnlyWeights };
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), options);

        return NearestSeedScorer.Rank(
            candidates,
            library,
            new TasteProfile(ImmutableDictionary<string, double>.Empty),
            options,
            decay,
            GenreWeights.Calculate(Counts),
            new Dictionary<string, double>(),
            CancellationToken.None);
    }

    private static RecommendationLibraryEntry[] Library(int[] ids)
    {
        var movies = ids
            .Select(value => (Math.Abs(value % 800)) + 2)
            .Distinct()
            .Take(8)
            .Select(id => Entry(id, watched: id % 4 == 0, liked: id % 4 == 1))
            .ToArray();

        return [.. movies, Entry(900, watched: false, liked: true)];
    }

    private static RecommendationLibraryEntry Entry(
        int id,
        bool watched = false,
        bool liked = false,
        bool disliked = false,
        ImmutableArray<float>? vector = null) =>
        new(
            new MovieFeatures(
                genres: ["Drama"],
                plotEmbedding: vector is { IsDefaultOrEmpty: false } ?
                    vector :
                    id % 2 == 0 ? Vector(1.0) : Vector(0.0, 1.0)),
            new MovieSummary(
                new MovieId(id),
                $"Movie {id}",
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                false,
                watched,
                disliked ? Review.Disliked : liked ? Review.Liked : Review.None));

    private static ImmutableArray<float> Vector(double primary, double secondary = 0.0)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        builder.Add((float)primary);
        builder.Add((float)secondary);

        for (var index = 2; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(0.0f);
        }

        return builder.MoveToImmutable();
    }
}
