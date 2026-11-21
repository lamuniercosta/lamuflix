using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetForYouRecommendationsQueryHandlerTests
{
    private const int DefaultTake = 10;
    private const string FallbackReasonText = "No shared features identified";

    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    private static double DramaPreferenceWeight => GenreWeights.Calculate(Counts).GetWeight("Drama");


    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IRecommendationCandidateSource candidateSource = Substitute.For<IRecommendationCandidateSource>();
    private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();
    private Movie[] libraryMovies = [];
    private RecommendationProfileInputs currentInputs = Inputs();

    [Fact]
    public async Task HandleAsync_ExcludingEligibleMoviePromotesNextMovieAndPagesWithHasMore()
    {
        var first = NewMovie(2, genres: ["Drama"], collectionId: 44);
        var second = NewMovie(3, genres: ["Drama"], collectionId: 44);
        var handler = NewHandler();
        ArrangeLibrary(first, second);
        ArrangeProfile(Inputs(PlayedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(1, excludedMovieIds: ImmutableHashSet.Create(first.Id)),
            CancellationToken.None);

        result.Items.Select(item => item.Summary.Id).ShouldBe([second.Id]);
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_HugeOffsetReturnsEmptyWithoutMmrSelection()
    {
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(PlayedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(10, int.MaxValue),
            CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_PagesAreStableDisjointSlicesWithExactHasMoreBoundary()
    {
        var handler = NewHandler();
        ArrangeLibrary(
            NewMovie(4, genres: ["Drama"]),
            NewMovie(2, genres: ["Drama"], collectionId: 44),
            NewMovie(3, genres: ["Drama"], collectionId: 44),
            NewMovie(5, genres: ["Drama"], collectionId: 45));
        ArrangeProfile(Inputs(PlayedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        var firstPage = await handler.HandleAsync(new GetForYouRecommendationsQuery(2), CancellationToken.None);
        var secondPage = await handler.HandleAsync(new GetForYouRecommendationsQuery(2, 2), CancellationToken.None);
        var beyondEnd = await handler.HandleAsync(new GetForYouRecommendationsQuery(2, 3), CancellationToken.None);
        var fullRanking = await handler.HandleAsync(new GetForYouRecommendationsQuery(3), CancellationToken.None);

        firstPage.Items.Count.ShouldBe(2);
        firstPage.HasMore.ShouldBeTrue();
        secondPage.Items.Count.ShouldBe(1);
        secondPage.HasMore.ShouldBeFalse();
        beyondEnd.Items.ShouldBeEmpty();
        beyondEnd.HasMore.ShouldBeFalse();
        firstPage.Items.Select(item => item.Summary.Id)
            .Intersect(secondPage.Items.Select(item => item.Summary.Id)).ShouldBeEmpty();
        firstPage.Items.Concat(secondPage.Items).Select(item => item.Summary.Id).ShouldNotContain(new MovieId(3));
        firstPage.Items.Concat(secondPage.Items).Select(item => item.Summary.Id)
            .ShouldBe(fullRanking.Items.Select(item => item.Summary.Id));
    }

    [Fact]
    public async Task HandleAsync_UnknownValidExclusionDoesNotChangeResults()
    {
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]), NewMovie(3, genres: ["Drama"]));
        ArrangeProfile(Inputs(PlayedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        var baseline = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);
        var withUnknownId = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(excludedMovieIds: ImmutableHashSet.Create(new MovieId(999))),
            CancellationToken.None);

        withUnknownId.Items.Select(item => item.Summary.Id).ShouldBe(baseline.Items.Select(item => item.Summary.Id));
    }

    public GetForYouRecommendationsQueryHandlerTests()
    {
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Counts));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new KeywordCounts(0, new Dictionary<string, long>())));
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<RecommendationLibraryEntry>>(LibraryEntries()));
    }

    private List<RecommendationLibraryEntry> LibraryEntries() =>
    [
        .. libraryMovies.Select(movie => new RecommendationLibraryEntry(
            MovieFeaturesMapper.From(
                movie,
                includeSignals: true,
                plotEmbedding: VectorFor(movie.Id)),
            Summary(movie))),
    ];

    private ImmutableArray<float> VectorFor(MovieId id) =>
        currentInputs.CandidateEmbeddings.TryGetValue(id, out var candidateVector)
            ? candidateVector
            : currentInputs.LikedSourceEmbeddings.GetValueOrDefault(id, ImmutableArray<float>.Empty);

    [Fact]
    public async Task HandleAsync_NoHistory_ReturnsEmptyBeforeScoring()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForYouOperation_ResolvesOneKeywordSnapshotForTheWholeMmrPass()
    {
        // arrange
        var cache = new KeywordWeightsCache();
        var handler = NewHandler(keywordCache: cache);
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama", "Comedy"]),
            NewMovie(4, genres: ["Comedy"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var first = await handler.HandleAsync(new GetForYouRecommendationsQuery(2), CancellationToken.None);
        cache.Invalidate();
        var second = await handler.HandleAsync(new GetForYouRecommendationsQuery(2), CancellationToken.None);

        // assert
        first.Items.Count.ShouldBe(2);
        await catalog.Received(2).GetKeywordCountsAsync(Arg.Any<CancellationToken>());
        second.Items.Select(item => item.Score).ShouldBe(first.Items.Select(item => item.Score));
        second.Items.Select(item => item.Summary.Id).ShouldBe(first.Items.Select(item => item.Summary.Id));
    }

    [Fact]
    public async Task HandleAsync_NotInterestedOnlyLibrary_ReturnsEmptyWithoutAcquiringProfile()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"], isNotInterested: true),
            NewMovie(3, genres: ["Drama"], isNotInterested: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await profileSource.DidNotReceive().GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WatchedOnlyLibrary_ReturnsEmptyWithoutAcquiringProfile()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"], isWatched: true),
            NewMovie(3, genres: ["Drama"], isWatched: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyLibrary_ReturnsEmptyWithoutAcquiringProfile()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary();

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await profileSource.DidNotReceive().GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PlaybackOnlyHistory_IsNotColdStart()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(PlayedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Summary.Id.ShouldBe(new MovieId(2));
    }

    [Fact]
    public async Task HandleAsync_WatchedOnlyHistory_IsNotColdStart()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Summary.Id.ShouldBe(new MovieId(2));
    }

    [Fact]
    public async Task HandleAsync_RealHistoryWithEmptyFeatures_IsNotColdStart()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, withMetadata: false));
        ArrangeProfile(Inputs(WatchedHistory(90)), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Score.ShouldBe(0.0);
        result.Items[0].Reasons.ShouldBe([Reason.Structural(FallbackReasonText)]);
    }

    [Fact]
    public async Task HandleAsync_PositiveMatchProfile_EmitsPersonalGenreReason()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items[0].Reasons.ShouldBe([Reason.Personal("Matches your Drama interest")]);
        result.Items[0].Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Personal);
    }

    [Fact]
    public async Task HandleAsync_DisabledProfileWeight_EmitsStructuralFallbackOnly()
    {
        // arrange
        var handler = NewHandler(blendProfileWeight: 0.0);
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items[0].Score.ShouldBe(0.0);
        result.Items[0].Reasons.ShouldBe([Reason.Structural(FallbackReasonText)]);
    }

    [Fact]
    public async Task HandleAsync_NoProfileOverlap_EmitsStructuralFallback()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Mystery"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items[0].Score.ShouldBe(0.0);
        result.Items[0].Reasons.ShouldBe([Reason.Structural(FallbackReasonText)]);
    }

    [Fact]
    public async Task HandleAsync_LiteralBlend_IgnoresSelectedSignalWeight()
    {
        // arrange
        var handler = NewHandler(blendSelectedWeight: 7.0, blendProfileWeight: 2.0);
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), MatchProfile());

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items[0].Score.ShouldBe(2.0, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_WatchedCandidate_IsExcludedFromScoredResults()
    {
        // arrange
        var handler = NewHandler(blendProfileWeight: 1.0, alreadyWatchedPenalty: 5.0);
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"], isWatched: true));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), MatchProfile());

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(2), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2 });
        result.Items[0].Score.ShouldBe(1.0, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_ConfiguredMmrLambda_ChangesOrderWithoutMutatingScoresOrReasons()
    {
        // arrange
        ArrangeLibrary(DiversificationLibrary());
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));
        var relevanceOnly = await NewHandler(mmrLambda: 1.0)
            .HandleAsync(new GetForYouRecommendationsQuery(6), CancellationToken.None);
        var diversified = await NewHandler(mmrLambda: 0.3)
            .HandleAsync(new GetForYouRecommendationsQuery(6), CancellationToken.None);
        var original = relevanceOnly.Items.ToDictionary(item => item.Summary.Id);

        // assert
        relevanceOnly.Items.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3, 4, 5, 6, 7 });
        diversified.Items.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 6, 7, 3, 4, 5 });
        foreach (var item in diversified.Items)
        {
            var expected = original[item.Summary.Id];
            item.Summary.ShouldBe(expected.Summary);
            item.Score.ShouldBe(expected.Score, 1e-12);
            item.Reasons.ShouldBe(expected.Reasons);
        }
    }

    [Fact]
    public async Task HandleAsync_LibraryBeyondCandidatePoolSize_IsScoredFromOneLibraryLoad()
    {
        // arrange
        var handler = NewHandler(candidatePoolSize: 50);
        var library = Enumerable.Range(2, 130)
            .Select(id => NewMovie(id, genres: ["Drama"]))
            .ToArray();
        ArrangeLibrary(library);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(60), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(50);
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DislikedEntry_IsExcludedAndNoSecondProfileReadHappens()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"], isNotInterested: true),
            NewMovie(3, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([3]);
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FullLibrary_IsReadOnceAndScored()
    {
        // arrange
        var handler = NewHandler(candidatePoolSize: 200);
        var library = Enumerable.Range(1, 101)
            .Select(id => NewMovie(id, genres: ["Drama"]))
            .ToArray();
        ArrangeLibrary(library);
        ArrangeProfile(Inputs(WatchedHistory(900, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(150), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(101);
        result.Items.Select(item => item.Summary.Id.Value).Distinct().Count().ShouldBe(101);
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SingleLoadSource_ExcludesDislikedAndKeepsEligible()
    {
        // arrange
        var handler = NewHandler();
        var excluded = NewMovie(2, genres: ["Drama"], isNotInterested: true);
        var eligible = NewMovie(3, genres: ["Drama"]);
        ArrangeLibrary(excluded, eligible);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([3]);
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnwatchedNotDislikedCandidate_IsRetained()
    {
        // arrange
        var handler = NewHandler();
        var eligible = NewMovie(4, genres: ["Drama"]);
        ArrangeLibrary(eligible);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([4]);
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_VectorlessCandidate_IsRetainedWithGenreOnlyProfileScore()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), MatchProfile());

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Score.ShouldBe(1.0, 1e-12);
        result.Items[0].Reasons.ShouldBe([Reason.Personal("Matches your Drama interest")]);
    }

    [Fact]
    public async Task HandleAsync_CollectionCutKeepsEarliestKnownYearThenLowestMovieId()
    {
        var handler = NewHandler();
        ArrangeLibrary(
            NewMovie(3, genres: ["Drama"], collectionId: 7),
            NewMovie(4, genres: ["Drama"], collectionId: 7, releaseYear: 2000),
            NewMovie(2, genres: ["Drama"], collectionId: 7, releaseYear: 2000),
            NewMovie(5, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        result.Items.Select(item => item.Summary.Id.Value).ShouldContain(2);
        result.Items.Select(item => item.Summary.Id.Value).ShouldNotContain(3);
        result.Items.Select(item => item.Summary.Id.Value).ShouldNotContain(4);
        result.Items.Select(item => item.Summary.Id.Value).ShouldContain(5);
    }

    [Fact]
    public async Task HandleAsync_CandidatePoolSizeBoundsTakeWithoutTruncatingEligibleCandidates()
    {
        var handler = NewHandler(candidatePoolSize: 2, take: 2, mmrLambda: 1.0);
        ArrangeLibrary(
            NewMovie(2, genres: ["Comedy"]),
            NewMovie(3, genres: ["Comedy"]),
            NewMovie(4, genres: ["Drama"]),
            NewMovie(10, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 4, 10 });
    }

    [Fact]
    public async Task HandleAsync_SeedSaturationDoesNotBackfillCollectionMember()
    {
        var handler = NewHandler(weights: PlotOnlyWeights, blendSelectedWeight: 0.0, maxPerBestSeed: 1);
        var first = NewMovie(2, genres: ["Drama"]);
        var representative = NewMovie(3, genres: ["Drama"], releaseYear: 2000, collectionId: 7);
        var laterMember = NewMovie(4, genres: ["Drama"], releaseYear: 2001, collectionId: 7);
        ArrangeLibrary(
            first,
            representative,
            laterMember,
            NewMovie(90, genres: ["Drama"], isWatched: true),
            NewMovie(91, genres: ["Drama"], isWatched: true));

        var firstVector = PlotVector(primary: 1.0f);
        var laterVector = PlotVector(primary: 0.0f, secondary: 1.0f);
        var inputs = new RecommendationProfileInputs(
            [WatchedHistory(90, "Drama"), WatchedHistory(91, "Drama")],
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty
                .Add(first.Id, firstVector)
                .Add(representative.Id, firstVector)
                .Add(laterMember.Id, laterVector),
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty
                .Add(new MovieId(90), firstVector)
                .Add(new MovieId(91), laterVector));
        ArrangeProfile(inputs, TwoLikedSources(firstVector));

        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(3), CancellationToken.None);

        result.Items.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2 });
    }

    [Fact]
    public async Task HandleAsync_CorruptCandidateVector_PropagatesScoringFailure()
    {
        // arrange
        var handler = NewHandler();
        var candidate = NewMovie(2, genres: ["Drama"]);
        ArrangeLibrary(candidate);
        var embeddings = ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty
            .Add(candidate.Id, ImmutableArray.Create(1.0f, 0.0f, 0.0f));
        ArrangeProfile(
            new RecommendationProfileInputs([WatchedHistory(90, "Drama")], embeddings),
            Profile(("Drama", 1.0)));

        // act / assert
        await Should.ThrowAsync<ArgumentException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_OmittedTake_ReturnsTen()
    {
        // arrange
        var handler = NewHandler();
        var library = Enumerable.Range(2, 12)
            .Select(id => NewMovie(id, genres: ["Drama"]))
            .ToArray();
        ArrangeLibrary(library);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_FewerEligibleThanTake_ReturnsEveryEligibleMovie()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]), NewMovie(3, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(5), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ResultSummary_CarriesEveryField()
    {
        // arrange
        var handler = NewHandler();
        var candidate = NewMovie(
            7,
            title: "Seven",
            posterUrl: "https://posters/seven.jpg",
            releaseYear: 1999,
            runtimeMinutes: 101,
            imdbRating: 7.5m,
            genres: ["Drama"],
            isInWatchlist: true);
        ArrangeLibrary(candidate);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(1), CancellationToken.None);

        // assert
        var expected = new MovieSummary(
            new MovieId(7),
            "Seven",
            "https://posters/seven.jpg",
            1999,
            101,
            7.5m,
            EnrichmentStatus.Enriched,
            true,
            false,
            Review.None);
        result.Items[0].Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_ProfileInputs_AreAcquiredOnceWithAllEligibleIds()
    {
        // arrange
        var handler = NewHandler();
        var library = new[]
        {
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Comedy"]),
            NewMovie(4, genres: ["Horror"]),
        };
        ArrangeLibrary(library);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PreCancelledToken_PerformsNoDependencyCalls()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await candidateSource.DidNotReceive().GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LibraryLoadCancelsToken_ThrowsBeforeProfileAcquisition()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult<IReadOnlyList<RecommendationLibraryEntry>>([]);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LibraryLoadWithoutEligibleCandidates_NeverReadsTheProfile()
    {
        // arrange
        var handler = NewHandler();
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"], isNotInterested: true),
            NewMovie(3, genres: ["Drama"], isWatched: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProfileCancelsToken_ThrowsBeforeGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(WithPlayback(Profile(("Drama", 1.0)), Inputs(WatchedHistory(90, "Drama"))));
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsCancelToken_ThrowsAfterProfileAcquisition()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Counts);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CachedProfileCancelsToken_ThrowsBeforeScoring()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Profile(("Drama", 1.0)));
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.Received(1).GetTasteProfileAsync(cts.Token);
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForwardsActiveProfileLikedIdentities_ToProfileInputs()
    {
        // arrange
        var handler = NewHandler();
        var candidate = NewMovie(2, genres: ["Drama"]);
        var liked = NewMovie(90, genres: ["Drama"], isWatched: true);
        ArrangeLibrary(candidate, liked);
        var vector = PlotVector(primary: 1.0f);
        var inputs = new RecommendationProfileInputs(
            [WatchedHistory(90, "Drama")],
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty.Add(candidate.Id, vector),
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty.Add(new MovieId(90), vector));
        ArrangeProfile(inputs, ProfileWithLikedSource(vector));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldHaveSingleItem();
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    private GetForYouRecommendationsQueryHandler NewHandler(
        int candidatePoolSize = 50,
        int take = 10,
        int maxPerBestSeed = 3,
        double blendSelectedWeight = 1.0,
        double blendProfileWeight = 1.0,
        double alreadyWatchedPenalty = 0.25,
        double mmrLambda = 0.7,
        SimilarityWeights? weights = null,
        KeywordWeightsCache? keywordCache = null) =>
        new(
            candidateSource,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, keywordCache ?? new KeywordWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
            {
                Take = take,
                CandidatePoolSize = candidatePoolSize,
                MaxPerBestSeed = maxPerBestSeed,
                BlendSelectedWeight = blendSelectedWeight,
                BlendProfileWeight = blendProfileWeight,
                AlreadyWatchedPenalty = alreadyWatchedPenalty,
                MmrLambda = mmrLambda,
                Weights = weights ?? new SimilarityWeights(),
            }));

    private void ArrangeLibrary(params Movie[] library) => libraryMovies = library;

    private void ArrangeProfile(RecommendationProfileInputs inputs, TasteProfile profile)
    {
        currentInputs = inputs;
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WithPlayback(profile, inputs)));
    }

    private static TasteProfile WithPlayback(TasteProfile profile, RecommendationProfileInputs inputs)
    {
        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<DateTimeOffset>>();

        foreach (var movie in inputs.Movies)
        {
            if (!movie.PlayedAt.IsDefaultOrEmpty)
            {
                builder[movie.MovieId] = [.. movie.PlayedAt];
            }
        }

        return profile with { PlaybackByMovieId = builder.ToImmutable() };
    }

    private static Movie[] DiversificationLibrary() =>
    [
        NewMovie(2, genres: ["Drama"]),
        NewMovie(3, genres: ["Drama"]),
        NewMovie(4, genres: ["Drama"]),
        NewMovie(5, genres: ["Drama"]),
        NewMovie(6, genres: ["Mystery"]),
        NewMovie(7, genres: ["Western"]),
    ];

    private static RecommendationProfileInputs Inputs(params TasteProfileMovie[] history) =>
        new([.. history], ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty);

    private static TasteProfile Profile(params (string Genre, double Weight)[] preferences) =>
        new(
            preferences.ToImmutableDictionary(
                preference => preference.Genre,
                preference => preference.Weight,
                StringComparer.OrdinalIgnoreCase),
            []);

    [Fact]
    public async Task HandleAsync_LikedSourcePlotEmbeddingFromInputs_DrivesExactScoreAndReason()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights, blendSelectedWeight: 0.0);
        var candidate = NewMovie(2, genres: ["Drama"]);
        var liked = NewMovie(90, genres: ["Drama"], isWatched: true);
        ArrangeLibrary(candidate, liked);
        var candidateVector = PlotVector(primary: 1.0f);
        var inputs = new RecommendationProfileInputs(
            [WatchedHistory(90, "Drama")],
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty.Add(candidate.Id, candidateVector),
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty.Add(new MovieId(90), candidateVector));
        ArrangeProfile(inputs, ProfileWithLikedSource(candidateVector));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Summary.Id.ShouldBe(candidate.Id);
        item.Score.ShouldBe(1.0, 1e-12);
        PersonalTexts(item).ShouldBe(["Because you liked About Time"]);
    }

    [Fact]
    public async Task HandleAsync_AbsentLikedSourceEmbeddings_LeavesNonPlotBehaviourUnchanged()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights, blendSelectedWeight: 0.0);
        var candidate = NewMovie(2, genres: ["Drama"]);
        var liked = NewMovie(90, genres: ["Drama"], isWatched: true);
        ArrangeLibrary(candidate, liked);
        var candidateVector = PlotVector(primary: 1.0f);
        var inputs = new RecommendationProfileInputs(
            [WatchedHistory(90, "Drama")],
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty.Add(candidate.Id, candidateVector));
        ArrangeProfile(inputs, ProfileWithLikedSource(candidateVector));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Score.ShouldBe(1.0, 1e-12);
        PersonalTexts(item).ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_TwoLikedSourcesWithInputVectors_PicksGreatestAffinitySource()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights, blendSelectedWeight: 0.0);
        var candidate = NewMovie(2, genres: ["Drama"]);
        ArrangeLibrary(
            candidate,
            NewMovie(90, genres: ["Drama"], isWatched: true),
            NewMovie(91, genres: ["Drama"], isWatched: true));
        var candidateVector = PlotVector(primary: 1.0f, secondary: 0.0f);
        var weakVector = PlotVector(primary: 1.0f, secondary: 1.0f);
        var inputs = new RecommendationProfileInputs(
            [WatchedHistory(90, "Drama"), WatchedHistory(91, "Drama")],
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty.Add(candidate.Id, candidateVector),
            ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty
                .Add(new MovieId(90), weakVector)
                .Add(new MovieId(91), candidateVector));
        ArrangeProfile(inputs, TwoLikedSources(candidateVector));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        PersonalTexts(result.Items.ShouldHaveSingleItem()).ShouldBe(["Because you liked Perfect Match"]);
    }

    [Fact]
    public async Task HandleAsync_PopulatedCollectionAndKeywordSignals_ShapeMmrSelection()
    {
        // arrange
        var handler = NewHandler(mmrLambda: 0.0);
        var keywordSignals = new MovieSignalSnapshot(
            keywordAvailability: SignalAvailability.Available,
            keywords: [new MovieKeyword(1, "heist")]);
        ArrangeLibrary(
            NewMovie(2, genres: ["Drama"], collectionId: 7, collectionName: "Franchise", signals: keywordSignals),
            NewMovie(3, genres: ["Drama"], collectionId: 7, collectionName: "Franchise", signals: keywordSignals),
            NewMovie(4, genres: ["Drama"]));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new KeywordCounts(10, new Dictionary<string, long> { ["heist"] = 2 })));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(2), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([2, 4]);
        await catalog.Received(1).GetKeywordCountsAsync(Arg.Any<CancellationToken>());
    }

    private static TasteProfile ProfileWithLikedSource(ImmutableArray<float> aggregateVector) =>
        new(ImmutableDictionary<string, double>.Empty, [])
        {
            Liked = new ReviewProfileChannels(
                aggregateVector,
                ImmutableDictionary<string, double>.Empty,
                ImmutableDictionary<string, double>.Empty,
                ImmutableDictionary<string, double>.Empty),
            LikedSources =
            [
                new LikedReviewSource(
                    new MovieId(90),
                    "About Time",
                    ImmutableDictionary<string, double>.Empty,
                    ImmutableDictionary<string, double>.Empty,
                    ImmutableDictionary<string, double>.Empty),
            ],
        };

    private static TasteProfile TwoLikedSources(ImmutableArray<float> aggregateVector) =>
        new(ImmutableDictionary<string, double>.Empty, [])
        {
            Liked = new ReviewProfileChannels(
                aggregateVector,
                ImmutableDictionary<string, double>.Empty,
                ImmutableDictionary<string, double>.Empty,
                ImmutableDictionary<string, double>.Empty),
            LikedSources =
            [
                new LikedReviewSource(
                    new MovieId(90),
                    "Weak Match",
                    ImmutableDictionary<string, double>.Empty,
                    ImmutableDictionary<string, double>.Empty,
                    ImmutableDictionary<string, double>.Empty),
                new LikedReviewSource(
                    new MovieId(91),
                    "Perfect Match",
                    ImmutableDictionary<string, double>.Empty,
                    ImmutableDictionary<string, double>.Empty,
                    ImmutableDictionary<string, double>.Empty),
            ],
        };

    private static SimilarityWeights PlotOnlyWeights => new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 1.0,
        Cast = 0.0,
        Genres = 0.0,
        Collection = 0.0,
        ReviewPlotWeight = 1.0,
    };

    private static ImmutableArray<float> PlotVector(float primary, float secondary = 0.0f)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(index switch
            {
                0 => primary,
                1 => secondary,
                _ => 0.0f,
            });
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<string> PersonalTexts(RecommendedMovie movie) =>
        [.. movie.Reasons.Where(reason => reason.Kind == ReasonKind.Personal).Select(reason => reason.Text)];

    private static TasteProfile MatchProfile() => Profile(("Drama", DramaPreferenceWeight));

    private static TasteProfileMovie WatchedHistory(int id, params string[] genres) =>
        new(new MovieId(id), [.. genres], [], [Now], IsWatched: true);

    private static TasteProfileMovie PlayedHistory(int id, params string[] genres) =>
        new(new MovieId(id), [.. genres], [], [Now], IsWatched: false);

    private static MovieSummary Summary(Movie movie, bool? isNotInterested = null) =>
        MovieSummaryMapper.From(movie) with { Review = ResolveReview(movie, isNotInterested) };

    private static Review ResolveReview(Movie movie, bool? isNotInterested) =>
        isNotInterested is null ? movie.Review : isNotInterested.Value ? Review.Disliked : Review.None;

    private static Movie NewMovie(
        int id,
        string? title = null,
        string? posterUrl = null,
        int? releaseYear = null,
        int? runtimeMinutes = null,
        decimal? imdbRating = null,
        string[]? genres = null,
        string[]? directors = null,
        string[]? actors = null,
        string[]? writers = null,
        bool withMetadata = true,
        bool isInWatchlist = false,
        bool isWatched = false,
        bool isNotInterested = false,
        int? collectionId = null,
        string? collectionName = null,
        MovieSignalSnapshot? signals = null)
    {
        var movieTitle = title ?? $"Movie {id}";
        var metadata = withMetadata
            ? BuildMetadata(movieTitle, posterUrl, releaseYear, runtimeMinutes, imdbRating, genres, directors, actors, writers, collectionId, collectionName, signals)
            : null;

        return Movie.Rehydrate(
            new MovieId(id),
            movieTitle,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist,
            metadata,
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            review: isNotInterested ? Review.Disliked : Review.None);
    }

    private static MovieMetadata BuildMetadata(
        string movieTitle,
        string? posterUrl,
        int? releaseYear,
        int? runtimeMinutes,
        decimal? imdbRating,
        string[]? genres,
        string[]? directors,
        string[]? actors,
        string[]? writers,
        int? collectionId = null,
        string? collectionName = null,
        MovieSignalSnapshot? signals = null) =>
        new(
            movieTitle,
            posterUrl: posterUrl,
            releaseYear: releaseYear is null ? null : new ReleaseYear(releaseYear.Value, Now),
            runtime: runtimeMinutes is null ? null : new Runtime(runtimeMinutes.Value),
            imdbRating: imdbRating is null ? null : new ImdbRating(imdbRating.Value),
            directors: directors,
            actors: actors,
            genres: genres,
            writers: writers,
            collectionId: collectionId,
            collectionName: collectionName ?? (collectionId.HasValue ? $"Collection {collectionId}" : null),
            signals: signals);
}

public sealed class GetForYouRecommendationsQueryHandlerPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    [Property(MaxTest = 100, Replay = "357001,100003")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedLibrary_ReturnsAtMostTakeUniqueEligibleIds(int[] ids, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids);
        var take = Take(takeSeed);

        // act
        var result = Handle(library, take);

        // assert
        var eligible = library.Where(movie => !movie.IsNotInterested && !movie.IsWatched).Select(movie => movie.Id).ToHashSet();
        return result.Count <= take
            && result.Select(item => item.Summary.Id).Distinct().Count() == result.Count
            && result.All(item => eligible.Contains(item.Summary.Id));
    }

    [Property(MaxTest = 100, Replay = "357002,200009")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedLibrary_CountIsMinOfTakeAndEligibleCount(int[] ids, bool[] notInterested, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids, notInterested);
        var take = Take(takeSeed);
        var eligibleCount = library.Count(movie => !movie.IsNotInterested && !movie.IsWatched);

        // act
        var result = Handle(library, take);

        // assert
        return result.Count == Math.Min(take, eligibleCount);
    }

    [Property(MaxTest = 100, Replay = "357003,300011")]
    [Trait("Category", "Property")]
    public bool HandleAsync_PermutedLibrary_ReturnsIdenticalOrdering(int[] ids, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids);
        var take = Take(takeSeed);

        // act
        var forward = Handle(library, take);
        var reversed = Handle([.. library.Reverse()], take);
        var shuffled = Handle([.. library.OrderBy(movie => movie.Id.Value)], take);

        // assert
        return SameSequence(forward, reversed) && SameSequence(forward, shuffled);
    }

    [Property(MaxTest = 100, Replay = "357004,400017")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedLibrary_ScoresAndReasonsMatchIndependentBlend(int[] ids, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids);
        var take = Take(takeSeed);
        var options = new RecommendationOptions();
        var genreWeights = GenreWeights.Calculate(Counts);
        var profile = ProfileWithDramaPreference();

        // act
        var result = Handle(library, take);

        // assert
        foreach (var item in result)
        {
            var movie = library.Single(candidate => candidate.Id == item.Summary.Id);
            var features = MovieFeaturesMapper.From(movie);
            var expectedScore = BlendScorer.Score(
                0.0,
                ProfileSimilarityCalculator.Similarity(
                    [.. features.Genres.Order(StringComparer.Ordinal)],
                    default,
                    profile,
                    genreWeights),
                movie.IsWatched ? 1.0 : 0.0,
                0.0,
                options);

            if (Math.Abs(item.Score - expectedScore) > 1e-12)
            {
                return false;
            }
        }

        return true;
    }

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<Movie> library, int take)
    {
        var catalog = Substitute.For<IMovieCatalog>();
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Counts));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new KeywordCounts(0, new Dictionary<string, long>())));

        var candidates = Substitute.For<IRecommendationCandidateSource>();
        candidates.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<RecommendationLibraryEntry>>(
            [
                .. library.Select(movie => new RecommendationLibraryEntry(
                    MovieFeaturesMapper.From(movie, includeSignals: true),
                    Summary(movie))),
            ]));

        var profileSource = Substitute.For<IRecommendationProfileSource>();
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProfileWithDramaPreference() with
            {
                PlaybackByMovieId = ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>
                    .Empty.Add(new MovieId(900), [Now]),
            }));

        var handler = new GetForYouRecommendationsQueryHandler(
            candidates,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions()));

        return handler
            .HandleAsync(new GetForYouRecommendationsQuery(take), CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Items;
    }

    private static TasteProfile ProfileWithDramaPreference() =>
        new(
            ImmutableDictionary<string, double>.Empty
                .Add("Drama", 1.0),
            []);

    private static MovieSummary Summary(Movie movie)
    {
        var metadata = movie.Metadata;
        if (metadata is null)
        {
            return new(
                movie.Id,
                movie.Title,
                null,
                null,
                null,
                null,
                movie.Status,
                movie.IsInWatchlist,
                movie.IsWatched,
                movie.Review);
        }

        return new(
            movie.Id,
            movie.Title,
            metadata.PosterUrl,
            metadata.ReleaseYear?.Value,
            metadata.Runtime?.Minutes,
            metadata.ImdbRating?.Value,
            movie.Status,
            movie.IsInWatchlist,
            movie.IsWatched,
            movie.Review);
    }

    private static Movie[] BuildLibrary(int[] ids, bool[]? notInterested = null)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var genres = new[] { "Drama", "Comedy", "Mystery" };
        var library = new Movie[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var isNotInterested = notInterested is { Length: > 0 } && notInterested[index % notInterested.Length];
            library[index] = NewMovie(
                distinct[index],
                genres: [genres[index % genres.Length]],
                isWatched: index % 4 == 0,
                isNotInterested: isNotInterested);
        }

        return library;
    }

    private static bool SameSequence(ImmutableList<RecommendedMovie> first, ImmutableList<RecommendedMovie> second) =>
        first.Count == second.Count
        && first.Zip(second).All(pair =>
            pair.First.Summary.Id == pair.Second.Summary.Id
            && Math.Abs(pair.First.Score - pair.Second.Score) < 1e-12
            && pair.First.Reasons.SequenceEqual(pair.Second.Reasons));

    private static int Take(int takeSeed) => (Math.Abs(takeSeed % 20)) + 1;

    private static Movie NewMovie(
        int id,
        string[]? genres = null,
        bool isWatched = false,
        bool isNotInterested = false,
        int? collectionId = null)
    {
        var title = $"Movie {id}";

        return Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            new MovieMetadata(
                title,
                genres: genres,
                directors: null,
                actors: null,
                writers: null,
                collectionId: collectionId,
                collectionName: collectionId is null ? null : $"Collection {collectionId}"),
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            review: isNotInterested ? Review.Disliked : Review.None);
    }
}
