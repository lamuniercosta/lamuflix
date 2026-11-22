using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetRecommendationsQueryHandlerPropertyTests
{
    private const int SelectedMovieId = 1;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    private static KeywordCounts EmptyKeywordCounts => new(0, new Dictionary<string, long>());

    [Property(MaxTest = 100, Replay = "340101,100003")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedPool_AtLambdaOne_OrdersByScoreDescendingThenIdAscending(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var result = Handle([.. pool.Select(Entry)], take, new RecommendationOptions { MmrLambda = 1.0, MinimumSimilarity = 0.0 });

        // assert
        return IsOrdered(result);
    }

    [Property(MaxTest = 100, Replay = "340102,200009")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedPool_CountIsMinOfTakeAndPoolSize(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var result = Handle(pool, take);

        // assert
        return result.Count == Math.Min(take, pool.Length);
    }

    [Property(MaxTest = 100, Replay = "340103,300011")]
    [Trait("Category", "Property")]
    public bool HandleAsync_PermutedPool_ReturnsIdenticalOrdering(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var forward = Handle(pool, take);
        var reversed = Handle([.. pool.Reverse()], take);
        var sorted = Handle([.. pool.OrderByDescending(movie => movie.Id.Value)], take);

        // assert
        return SameSequence(forward, reversed) && SameSequence(forward, sorted);
    }

    [Property(MaxTest = 100, Replay = "340105,500021")]
    [Trait("Category", "Property")]
    public bool HandleAsync_EvidenceIsScorerEvidence(int[] ids, int[] cosineSeeds, int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds);
        var byId = pool.ToDictionary(candidate => candidate.Id.Value);
        var genreWeights = GenreWeights.Calculate(Counts);

        // act
        var result = Handle(pool, Take(takeSeed));

        // assert
        foreach (var item in result)
        {
            if (!EvidenceMatch(item, byId, genreWeights))
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 100, Replay = "340107,700031")]
    [Trait("Category", "Property")]
    public bool HandleAsync_CandidateCosine_DoesNotEmitStoryReason(int[] ids, int[] cosineSeeds, int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds);

        // act
        var result = Handle(pool, Take(takeSeed));

        // assert
        return !result.Any(HasStoryReason);
    }

    private static bool EvidenceMatch(
        RecommendedMovie item,
        IReadOnlyDictionary<int, RecommendationLibraryEntry> byId,
        GenreWeights genreWeights)
    {
        var candidate = byId[item.Summary.Id.Value];
        var options = new RecommendationOptions();
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(SelectedMovie()),
            candidate.Features,
            options,
            genreWeights,
            EmptyKeywordCounts);

        return item.Evidence.SequenceEqual(SimilarityScorer.BuildEvidence(expected, options));
    }

    [Property(MaxTest = 100, Replay = "340110,1100041")]
    [Trait("Category", "Property")]
    public bool HandleAsync_NotInterestedCandidates_AreNeverScoredOrReturned(
        int[] ids,
        int[] cosineSeeds,
        bool[] notInterested,
        int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds, notInterested);
        var take = Take(takeSeed);
        var expectedCount = Math.Min(take, pool.Count(candidate => !IsDisliked(candidate)));

        // act
        var result = Handle(pool, take);

        // assert
        return result.Count == expectedCount
            && result.All(item => !pool.Any(candidate =>
                IsDisliked(candidate)
                && candidate.Id.Value == item.Summary.Id.Value));
    }

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<Movie> pool, int take) =>
        Handle([.. pool.Select(Entry)], take);

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<RecommendationLibraryEntry> pool, int take) =>
        Handle(pool, take, new RecommendationOptions { MinimumSimilarity = 0.0 });

    [Property(MaxTest = 100, Replay = "340112,130007")]
    [Trait("Category", "Property")]
    public bool HandleAsync_SeedCollection_LeadsWithFranchiseBlockWithinCapacity(
        int[] ids,
        int[] cosineSeeds,
        int franchiseSeed,
        int takeSeed)
    {
        // arrange
        var pool = FranchiseCandidates(ids, cosineSeeds, franchiseSeed);
        var take = Take(takeSeed);
        var collectionId = FranchiseCandidatesCollectionId;
        var franchiseCount = pool.Count(candidate => candidate.Features.CollectionId == collectionId);
        var expectedBlockSize = Math.Min(Math.Min(10, take), franchiseCount);

        // act
        var result = Handle(SelectedMovie(collectionId), pool, take);

        // assert
        var poolIds = pool.Select(candidate => candidate.Id).ToHashSet();
        return result.Count <= take
            && result.Select(item => item.Summary.Id).Distinct().Count() == result.Count
            && result.All(item => poolIds.Contains(item.Summary.Id))
            && result.Take(expectedBlockSize).All(item =>
                pool.Single(candidate => candidate.Id == item.Summary.Id).Features.CollectionId == collectionId);
    }

    private const int FranchiseCandidatesCollectionId = 900;

    private static RecommendationLibraryEntry[] FranchiseCandidates(int[] ids, int[] cosineSeeds, int franchiseSeed)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();
        var candidates = new RecommendationLibraryEntry[distinct.Length];

        for (var index = 0; index < distinct.Length; index++)
        {
            var seed = cosineSeeds.Length == 0 ? distinct[index] : cosineSeeds[index % cosineSeeds.Length];
            var inFranchise = franchiseSeed % 3 != 0 && index % 3 == 0;
            candidates[index] = Entry(BuildMovie(
                distinct[index],
                seed,
                collectionId: inFranchise ? FranchiseCandidatesCollectionId : null));
        }

        return candidates;
    }

    private static ImmutableList<RecommendedMovie> Handle(Movie selected, IReadOnlyList<RecommendationLibraryEntry> pool, int take) =>
        Handle(selected, pool, take, new RecommendationOptions { MinimumSimilarity = 0.0 });

    private static ImmutableList<RecommendedMovie> Handle(
        IReadOnlyList<RecommendationLibraryEntry> pool,
        int take,
        RecommendationOptions options) =>
        Handle(SelectedMovie(), pool, take, options);

    private static ImmutableList<RecommendedMovie> Handle(
        Movie selected,
        IReadOnlyList<RecommendationLibraryEntry> pool,
        int take,
        RecommendationOptions options)
    {
        var movies = Substitute.For<IMovieRepository>();
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        var source = Substitute.For<IRecommendationCandidateSource>();
        var library = new List<RecommendationLibraryEntry>(pool.Count + 1) { Entry(selected, UnitVector()) };
        library.AddRange(pool);
        source.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(new RecommendationLibrarySnapshot(library, CrowdLinks.Empty));
        var catalog = Substitute.For<IMovieCatalog>();
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(EmptyKeywordCounts);
        var profileSource = Substitute.For<IRecommendationProfileSource>();
        profileSource
            .GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new RecommendationProfileInputs(
                ImmutableArray<TasteProfileMovie>.Empty,
                ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty)));
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new TasteProfile(
                ImmutableDictionary<string, double>.Empty)));
        var handler = new GetRecommendationsQueryHandler(
            movies,
            source,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(options));

        return handler
            .HandleAsync(new GetRecommendationsQuery(new MovieId(SelectedMovieId), take), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static int Take(int takeSeed) => Math.Abs(takeSeed % 20) + 1;

    private const string StoryReasonText = "Similar story";

    private static bool IsDisliked(RecommendationLibraryEntry candidate) => candidate.Review == Review.Disliked;

    private static RecommendationLibraryEntry Entry(Movie movie) => Entry(movie, null);

    private static RecommendationLibraryEntry Entry(Movie movie, ImmutableArray<float>? plotEmbedding) =>
        new(
            MovieFeaturesMapper.From(
                movie,
                includeSignals: true,
                plotEmbedding: plotEmbedding is { IsDefaultOrEmpty: false } embedding ? embedding : null),
            MovieSummaryMapper.From(movie));

    private static ImmutableArray<float> UnitVector()
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(index == 0 ? 1.0f : 0.0f);
        }

        return builder.MoveToImmutable();
    }

    private static RecommendationLibraryEntry[] BuildCandidates(
        int[] ids,
        int[] cosineSeeds,
        bool[]? notInterested = null)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var candidates = new RecommendationLibraryEntry[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var seed = cosineSeeds.Length == 0 ? distinct[index] : cosineSeeds[index % cosineSeeds.Length];
            var isNotInterested = notInterested is { Length: > 0 } && notInterested[index % notInterested.Length];
            candidates[index] = Entry(BuildMovie(distinct[index], seed, isNotInterested));
        }

        return candidates;
    }

    private static bool HasStoryReason(RecommendedMovie movie) =>
        movie.Evidence.Any(evidence => evidence.Text == StoryReasonText);

    private static Movie[] BuildPool(int[] ids, int[] seeds)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var pool = new Movie[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var seed = seeds.Length == 0 ? distinct[index] : seeds[index % seeds.Length];
            pool[index] = BuildMovie(distinct[index], seed);
        }

        return pool;
    }

    private static Movie BuildMovie(int id, int seed, bool isNotInterested = false, int? collectionId = null)
    {
        var magnitude = Math.Abs((long)seed);
        var title = $"Movie {id}";

        if (magnitude % 6 == 0)
        {
            return RehydrateBare(id, title, isNotInterested);
        }

        return RehydrateWithMetadata(id, title, magnitude, isNotInterested, collectionId);
    }

    private static Movie RehydrateBare(int id, string title, bool isNotInterested) =>
        Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            review: isNotInterested ? Review.Disliked : Review.None);

    private static Movie RehydrateWithMetadata(
        int id,
        string title,
        long magnitude,
        bool isNotInterested,
        int? collectionId) =>
        Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            BuildPropertyMetadata(id, title, magnitude, collectionId),
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            review: isNotInterested ? Review.Disliked : Review.None);

    private static MovieMetadata BuildPropertyMetadata(int id, string title, long magnitude, int? collectionId) =>
        new(
            title,
            posterUrl: $"https://posters/{id}",
            releaseYear: new ReleaseYear(1990 + (int)(magnitude % 30), Now),
            runtime: new Runtime(80 + (int)(magnitude % 80)),
            imdbRating: new ImdbRating((magnitude % 101) / 10.0m),
            directors: [(magnitude & 1) == 0 ? "Director A" : "Director B"],
            actors: [(magnitude & 2) == 0 ? "Actor A" : "Actor B"],
            genres: Genres(magnitude),
            writers: [(magnitude & 4) == 0 ? "Writer A" : "Writer B"],
            collectionId: collectionId,
            collectionName: collectionId is null ? null : $"Franchise {collectionId}");

    private static string[] Genres(long magnitude)
    {
        var palette = new[] { "Drama", "Comedy", "Horror", "Action" };
        return [.. palette.Where((_, index) => ((magnitude >> index) & 1) == 1)];
    }

    private static Movie SelectedMovie(int? collectionId = null) => Movie.Rehydrate(
        new MovieId(SelectedMovieId),
        "Selected",
        new LibraryPath("C:/library/selected.mkv"),
        new MediaFormat("mkv"),
        false,
        new MovieMetadata(
            "Selected",
            releaseYear: new ReleaseYear(2000, Now),
            runtime: new Runtime(120),
            imdbRating: new ImdbRating(8.0m),
            directors: ["Director A"],
            actors: ["Actor A"],
            genres: ["Drama", "Action"],
            writers: ["Writer A"],
            collectionId: collectionId,
            collectionName: collectionId is null ? null : $"Franchise {collectionId}"),
        EnrichmentStatus.Enriched,
        Now,
        1,
        null,
        null);

    private static bool IsOrdered(ImmutableList<RecommendedMovie> result)
    {
        for (var index = 1; index < result.Count; index++)
        {
            var previous = result[index - 1];
            var current = result[index];

            if (previous.Score < current.Score)
            {
                return false;
            }

            if (previous.Score.Equals(current.Score) && previous.Summary.Id.Value > current.Summary.Id.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameSequence(ImmutableList<RecommendedMovie> first, ImmutableList<RecommendedMovie> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var index = 0; index < first.Count; index++)
        {
            if (!SameRecommendation(first[index], second[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameRecommendation(RecommendedMovie first, RecommendedMovie second) =>
        first.Summary == second.Summary
        && first.Score.Equals(second.Score)
        && first.Evidence.SequenceEqual(second.Evidence);
}
