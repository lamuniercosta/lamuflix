using System;
using System.Collections.Generic;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

/// <summary>
/// One fixed pair of library movies used to pin exact pre-Crowd scores for the For you and Related
/// scoring paths. Provenance: captured from revision df855da3 (the last revision with the pre-Crowd
/// scorer), same library inputs, same ordering, no seed inference from the changed scorer.
/// </summary>
public static class CrowdParityFixture
{
    private static RecommendationOptions Options { get; } = new();

    private static GenreWeights Genres { get; } = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
            }));

    private static KeywordCounts Keywords { get; } = new(0, new Dictionary<string, long>());

    private static MovieFeatures Seed { get; } = new(
        genres: ["Drama"],
        directors: ["Ana"],
        cast: ["Dora", "Emil"],
        collectionId: 11,
        collectionName: "Saga",
        plotEmbedding: [1f, 0f]);

    private static MovieFeatures Candidate { get; } = new(
        genres: ["Drama", "Comedy"],
        directors: ["Ana"],
        cast: ["Dora", "Emil", "Fay"],
        plotEmbedding: [0.44f, 0.9f]);

    private static RecommendationLibraryEntry SeedEntry { get; } = new(
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

    private static RecommendationLibraryEntry CandidateEntry { get; } = new(
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

    /// <summary>Stored Crowd links for the fixture pair; unused whenever Crowd is omitted.</summary>
    public static CrowdLinks Links { get; } = CrowdLinks.Create(
        [(new MovieId(90), new MovieId(2))]);

    /// <summary>The For you path: nearest-seed ranking over the prepared scorer.</summary>
    public static NearestSeedMatch ForYou() => ForYouWith(CrowdScoring.Disabled);

    /// <summary>The For you path with an explicit Crowd state.</summary>
    public static NearestSeedMatch ForYouWith(CrowdScoring crowdScoring)
    {
        var rank = NearestSeedScorer.Rank(
            [CandidateEntry],
            [SeedEntry, CandidateEntry],
            new TasteProfile(System.Collections.Immutable.ImmutableDictionary<string, double>.Empty),
            Options,
            new RecommendationRecencyDecay(TimeProvider.System, Options),
            Genres,
            SimilarityScorer.PrepareKeywordWeights(Options, Keywords),
            CancellationToken.None,
            crowdScoring);

        return rank.Count == 1
            ? rank[0]
            : throw new InvalidOperationException("The For you parity fixture must produce exactly one match.");
    }

    /// <summary>The Related path: the same pair scored through the shared plain scorer.</summary>
    public static SimilarityResult Related() => RelatedWith(CrowdScoring.Disabled);

    /// <summary>The Related path with an explicit Crowd state.</summary>
    public static SimilarityResult RelatedWith(CrowdScoring crowdScoring) =>
        SimilarityScorer.Score(
            Seed,
            Candidate,
            Options,
            Genres,
            Keywords,
            crowdScoring,
            SeedEntry.Id,
            CandidateEntry.Id);
}
