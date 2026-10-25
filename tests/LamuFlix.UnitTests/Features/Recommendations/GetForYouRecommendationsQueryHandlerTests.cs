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

    private static readonly double DramaPreferenceWeight = GenreWeights.Calculate(Counts).GetWeight("Drama");

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();
    private readonly List<MovieQuery> browseQueries = [];

    public GetForYouRecommendationsQueryHandlerTests()
    {
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Counts));
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Movie?>(null));
    }

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
        result.ShouldBeEmpty();
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotInterestedOnlyLibrary_ReturnsEmptyWithoutAcquiringProfile()
    {
        // arrange
        var handler = NewHandler();
        ArrangeBrowse(
            Summary(NewMovie(2, genres: ["Drama"]), isNotInterested: true),
            Summary(NewMovie(3, genres: ["Drama"]), isNotInterested: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyLibrary_ReturnsEmptyWithoutAcquiringProfile()
    {
        // arrange
        var handler = NewHandler();
        ArrangeBrowse();

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
        await profileSource.DidNotReceive().GetProfileInputsAsync(
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
        result.Count.ShouldBe(1);
        result[0].Summary.Id.ShouldBe(new MovieId(2));
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
        result.Count.ShouldBe(1);
        result[0].Summary.Id.ShouldBe(new MovieId(2));
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
        result.Count.ShouldBe(1);
        result[0].Score.ShouldBe(0.0);
        result[0].Reasons.ShouldBe([Reason.Structural(FallbackReasonText)]);
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
        result[0].Reasons.ShouldBe([Reason.Personal("Matches your Drama interest")]);
        result[0].Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Personal);
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
        result[0].Score.ShouldBe(0.0);
        result[0].Reasons.ShouldBe([Reason.Structural(FallbackReasonText)]);
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
        result[0].Score.ShouldBe(0.0);
        result[0].Reasons.ShouldBe([Reason.Structural(FallbackReasonText)]);
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
        result[0].Score.ShouldBe(2.0, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_WatchedCandidate_AppliesConfiguredPenalty()
    {
        // arrange
        var handler = NewHandler(blendProfileWeight: 2.0, alreadyWatchedPenalty: 0.5);
        ArrangeLibrary(NewMovie(2, genres: ["Drama"], isWatched: true));
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), MatchProfile());

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe(1.5, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_NegativeFiniteScores_ArePreservedThroughSelectorRebase()
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
        result.Count.ShouldBe(2);
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3 });
        result[0].Score.ShouldBe(1.0, 1e-12);
        result[1].Score.ShouldBe(-4.0, 1e-12);
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
        var original = relevanceOnly.ToDictionary(item => item.Summary.Id);

        // assert
        relevanceOnly.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3, 4, 5, 6, 7 });
        diversified.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 6, 7, 3, 4, 5 });
        foreach (var item in diversified)
        {
            var expected = original[item.Summary.Id];
            item.Summary.ShouldBe(expected.Summary);
            item.Score.ShouldBe(expected.Score, 1e-12);
            item.Reasons.ShouldBe(expected.Reasons);
        }
    }

    [Fact]
    public async Task HandleAsync_AllLibraryBeyondCandidatePoolSize_IsPagedAndScored()
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
        result.Count.ShouldBe(60);
        browseQueries.Count.ShouldBe(2);
        browseQueries[0].Page.Number.ShouldBe(1);
        browseQueries[1].Page.Number.ShouldBe(2);
        browseQueries.ShouldAllBe(query => query.Sort == MovieSort.Title);
        browseQueries.ShouldAllBe(query => query.Direction == SortDirection.Ascending);
        browseQueries.ShouldAllBe(query => query.Page.Size >= 1 && query.Page.Size <= 100);
        await movies.Received(130).GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotInterestedSummary_IsExcludedWithoutLoadingDomainRow()
    {
        // arrange
        var handler = NewHandler();
        var excluded = NewMovie(2, genres: ["Drama"]);
        var eligible = NewMovie(3, genres: ["Drama"]);
        ArrangeBrowse(Summary(excluded, isNotInterested: true), Summary(eligible));
        ArrangeMovies(excluded, eligible);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 3 });
        await movies.DidNotReceive().GetAsync(excluded.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotInterestedDomainRow_IsRecheckedAndExcluded()
    {
        // arrange
        var handler = NewHandler();
        var excluded = NewMovie(2, genres: ["Drama"], isNotInterested: true);
        var eligible = NewMovie(3, genres: ["Drama"]);
        ArrangeBrowse(Summary(excluded, isNotInterested: false), Summary(eligible));
        ArrangeMovies(excluded, eligible);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 3 });
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 1 && ids.Single() == eligible.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NullDomainRow_IsSkipped()
    {
        // arrange
        var handler = NewHandler();
        var missing = NewMovie(3, genres: ["Drama"]);
        var eligible = NewMovie(4, genres: ["Drama"]);
        ArrangeBrowse(Summary(missing), Summary(eligible));
        ArrangeMovies(eligible);
        ArrangeProfile(Inputs(WatchedHistory(90, "Drama")), Profile(("Drama", 1.0)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 4 });
        await movies.Received(1).GetAsync(missing.Id, Arg.Any<CancellationToken>());
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
        result.Count.ShouldBe(1);
        result[0].Score.ShouldBe(1.0, 1e-12);
        result[0].Reasons.ShouldBe([Reason.Personal("Matches your Drama interest")]);
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
        result.Count.ShouldBe(DefaultTake);
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
        result.Count.ShouldBe(2);
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
            isInWatchlist: true,
            isWatched: true);
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
            true,
            false);
        result[0].Summary.ShouldBe(expected);
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
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 3 && ids.Contains(new MovieId(2)) && ids.Contains(new MovieId(3)) && ids.Contains(new MovieId(4))),
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
        await catalog.DidNotReceive().BrowseAsync(Arg.Any<MovieQuery>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BrowseCancelsToken_ThrowsBeforeLoadingDomainRows()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        catalog.BrowseAsync(Arg.Any<MovieQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(new PagedResult<MovieSummary>(
                    [Summary(NewMovie(2, genres: ["Drama"]))],
                    1));
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DomainRowLoadCancelsToken_ThrowsBeforeProfileAcquisition()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        var candidate = NewMovie(2, genres: ["Drama"]);
        ArrangeBrowse(Summary(candidate));
        movies.GetAsync(candidate.Id, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult<Movie?>(candidate);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProfileInputsCancelToken_ThrowsBeforeGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        ArrangeLibrary(NewMovie(2, genres: ["Drama"]));
        profileSource.GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Inputs(WatchedHistory(90, "Drama")));
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsCancelToken_ThrowsBeforeCachedProfile()
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
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
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
    }

    private GetForYouRecommendationsQueryHandler NewHandler(
        int candidatePoolSize = 50,
        double blendSelectedWeight = 1.0,
        double blendProfileWeight = 1.0,
        double alreadyWatchedPenalty = 0.25,
        double mmrLambda = 0.7,
        SimilarityWeights? weights = null) =>
        new(
            catalog,
            movies,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
            {
                CandidatePoolSize = candidatePoolSize,
                BlendSelectedWeight = blendSelectedWeight,
                BlendProfileWeight = blendProfileWeight,
                AlreadyWatchedPenalty = alreadyWatchedPenalty,
                MmrLambda = mmrLambda,
                Weights = weights ?? new SimilarityWeights(),
            }));

    private void ArrangeLibrary(params Movie[] library)
    {
        ArrangeBrowse([.. library.Select(movie => Summary(movie))]);
        ArrangeMovies(library);
    }

    private void ArrangeBrowse(params MovieSummary[] summaries)
    {
        catalog.BrowseAsync(Arg.Any<MovieQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<MovieQuery>();
                browseQueries.Add(query);
                var items = summaries
                    .Skip((query.Page.Number - 1) * query.Page.Size)
                    .Take(query.Page.Size)
                    .ToImmutableArray();
                return Task.FromResult(new PagedResult<MovieSummary>(items, summaries.Length));
            });
    }

    private void ArrangeMovies(params Movie[] library)
    {
        foreach (var movie in library)
        {
            var captured = movie;
            movies.GetAsync(Arg.Is<MovieId>(id => id == captured.Id), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Movie?>(captured));
        }
    }

    private void ArrangeProfile(RecommendationProfileInputs inputs, TasteProfile profile)
    {
        profileSource
            .GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(inputs));
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(profile));
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

    private static TasteProfile MatchProfile() => Profile(("Drama", DramaPreferenceWeight));

    private static TasteProfileMovie WatchedHistory(int id, params string[] genres) =>
        new(new MovieId(id), [.. genres], [], [Now], IsWatched: true);

    private static TasteProfileMovie PlayedHistory(int id, params string[] genres) =>
        new(new MovieId(id), [.. genres], [], [Now], IsWatched: false);

    private static MovieSummary Summary(Movie movie, bool? isNotInterested = null)
    {
        var metadata = movie.Metadata;

        return new MovieSummary(
            movie.Id,
            movie.Title,
            metadata?.PosterUrl,
            metadata?.ReleaseYear?.Value,
            metadata?.Runtime?.Minutes,
            metadata?.ImdbRating?.Value,
            movie.Status,
            movie.IsInWatchlist,
            movie.IsWatched,
            isNotInterested ?? movie.IsNotInterested);
    }

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
        bool isNotInterested = false)
    {
        var movieTitle = title ?? $"Movie {id}";
        var metadata = withMetadata
            ? new MovieMetadata(
                movieTitle,
                posterUrl: posterUrl,
                releaseYear: releaseYear is null ? null : new ReleaseYear(releaseYear.Value, Now),
                runtime: runtimeMinutes is null ? null : new Runtime(runtimeMinutes.Value),
                imdbRating: imdbRating is null ? null : new ImdbRating(imdbRating.Value),
                directors: directors,
                actors: actors,
                genres: genres,
                writers: writers)
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
            isNotInterested: isNotInterested);
    }
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
        var eligible = library.Where(movie => !movie.IsNotInterested).Select(movie => movie.Id).ToHashSet();
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
        var eligibleCount = library.Count(movie => !movie.IsNotInterested);

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
        catalog.BrowseAsync(Arg.Any<MovieQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<MovieQuery>();
                var items = library
                    .Skip((query.Page.Number - 1) * query.Page.Size)
                    .Take(query.Page.Size)
                    .Select(Summary)
                    .ToImmutableArray();
                return Task.FromResult(new PagedResult<MovieSummary>(items, library.Count));
            });
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Counts));

        var movies = Substitute.For<IMovieRepository>();
        foreach (var movie in library)
        {
            var captured = movie;
            movies.GetAsync(Arg.Is<MovieId>(id => id == captured.Id), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Movie?>(captured));
        }

        var profileSource = Substitute.For<IRecommendationProfileSource>();
        profileSource
            .GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RecommendationProfileInputs(
                [new TasteProfileMovie(new MovieId(900), ["Drama"], [], [Now], IsWatched: true)],
                ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty)));
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProfileWithDramaPreference()));

        var handler = new GetForYouRecommendationsQueryHandler(
            catalog,
            movies,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions()));

        return handler
            .HandleAsync(new GetForYouRecommendationsQuery(take), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static TasteProfile ProfileWithDramaPreference() =>
        new(
            ImmutableDictionary<string, double>.Empty
                .Add("Drama", 1.0),
            []);

    private static MovieSummary Summary(Movie movie) =>
        new(
            movie.Id,
            movie.Title,
            movie.Metadata?.PosterUrl,
            movie.Metadata?.ReleaseYear?.Value,
            movie.Metadata?.Runtime?.Minutes,
            movie.Metadata?.ImdbRating?.Value,
            movie.Status,
            movie.IsInWatchlist,
            movie.IsWatched,
            movie.IsNotInterested);

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
        bool isNotInterested = false)
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
                writers: null),
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            isNotInterested: isNotInterested);
    }
}
