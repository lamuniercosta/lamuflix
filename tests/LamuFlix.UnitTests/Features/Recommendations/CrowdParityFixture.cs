using System;
using System.Collections.Generic;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public static class CrowdParityFixture
{
    private static RecommendationOptions Options => new();

    private static GenreWeights Genres => GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
            }));

    private static KeywordCounts Keywords => new(0, new Dictionary<string, long>());

    private static MovieFeatures Seed => new(
        genres: ["Drama"],
        directors: ["Ana"],
        cast: ["Dora", "Emil"],
        collectionId: 11,
        collectionName: "Saga",
        plotEmbedding: [1f, 0f]);

    private static MovieFeatures Candidate => new(
        genres: ["Drama", "Comedy"],
        directors: ["Ana"],
        cast: ["Dora", "Emil", "Fay"],
        plotEmbedding: [0.44f, 0.9f]);

    private static RecommendationLibraryEntry SeedEntry => new(
        Seed,
        new MovieSummary(
            new MovieId(90),
            "Saga Starter",
            null,
            null,
            null,
            null,
            EnrichmentStatus.Enriched,
            false,
            true,
            Review.Liked));

    private static RecommendationLibraryEntry CandidateEntry => new(
        Candidate,
        new MovieSummary(
            new MovieId(2),
            "Candidate",
            null,
            null,
            null,
            null,
            EnrichmentStatus.Enriched,
            false,
            false,
            Review.None));

    public static CrowdLinks Links => CrowdLinks.Create(
        [(new MovieId(90), new MovieId(2))]);

    public static NearestSeedMatch ForYou() => ForYouWith(CrowdScoring.Disabled);

    public static NearestSeedMatch ForYouWith(CrowdScoring crowdScoring)
    {
        var options = Options;
        var seedEntry = SeedEntry;
        var candidateEntry = CandidateEntry;
        var rank = NearestSeedScorer.Rank(
            [candidateEntry],
            [seedEntry, candidateEntry],
            new TasteProfile(System.Collections.Immutable.ImmutableDictionary<string, double>.Empty),
            options,
            new RecommendationRecencyDecay(TimeProvider.System, options),
            Genres,
            SimilarityScorer.PrepareKeywordWeights(options, Keywords),
            CancellationToken.None,
            crowdScoring);

        return rank.Count == 1
            ? rank[0]
            : throw new InvalidOperationException("The For you parity fixture must produce exactly one match.");
    }

    public static SimilarityResult Related() => RelatedWith(CrowdScoring.Disabled);

    public static SimilarityResult RelatedWith(CrowdScoring crowdScoring)
    {
        var options = Options;
        var seed = Seed;
        var candidate = Candidate;
        var seedEntry = SeedEntry;
        var candidateEntry = CandidateEntry;
        return SimilarityScorer.Score(
            seed,
            candidate,
            options,
            Genres,
            Keywords,
            crowdScoring,
            seedEntry.Id,
            candidateEntry.Id);
    }
}

