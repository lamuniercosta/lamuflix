using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetRecommendationsQueryHandlerTests
{
    private const int DefaultTake = 10;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly SimilarityWeights Weights = new();
    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
        });

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IRecommendationCandidateSource candidateSource = Substitute.For<IRecommendationCandidateSource>();
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();

    public GetRecommendationsQueryHandlerTests()
    {
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
    }

    [Fact]
    public async Task HandleAsync_MissingSelectedMovie_ThrowsNotFoundException()
    {
        // arrange
        var handler = NewHandler();
        var query = new GetRecommendationsQuery(new MovieId(1));
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((Movie?)null);

        // act / assert
        await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(query, CancellationToken.None));
        await candidateSource.DidNotReceive().GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SelectedMovieNotEnriched_ReturnsRecommendations()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, withMetadata: false, status: EnrichmentStatus.Pending);
        var pool = new[] { NewMovie(2, genres: ["Drama"]) };
        Arrange(selected, pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(1);
        result[0].Summary.Id.ShouldBe(new MovieId(2));
    }

    [Fact]
    public async Task HandleAsync_EmptyPool_ReturnsEmptyImmutableList()
    {
        // arrange
        var handler = NewHandler();
        Arrange(Selected(), []);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EmptyPool_DoesNotLoadGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        Arrange(Selected(), []);

        // act
        await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        await catalog.Received(0).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FewerCandidatesThanTake_ReturnsEveryCandidate()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(3);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id, DefaultTake), CancellationToken.None);

        // assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_CandidatesEqualToTake_ReturnsTake()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(DefaultTake);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id, DefaultTake), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_MoreCandidatesThanTake_ReturnsTake()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(25);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id, DefaultTake), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_OmittedTake_DefaultsToExactlyTen()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(12);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_OptionsTakeDiffersFromTen_OmittedQueryTakeStillReturnsTen()
    {
        // arrange
        var handler = NewHandler(optionsTake: 3);
        var pool = Pool(12);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Theory]
    [InlineData(3, 5, 7, 5)]
    [InlineData(9, 2, 7, 2)]
    public async Task HandleAsync_ExplicitTakeDiffersFromOptionsTake_ReturnsMinOfTakeAndCount(
        int optionsTake,
        int queryTake,
        int candidateCount,
        int expected)
    {
        // arrange
        var handler = NewHandler(optionsTake);
        var pool = Pool(candidateCount);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id, queryTake), CancellationToken.None);

        // assert
        result.Count.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_EqualScores_OrdersByAscendingMovieId()
    {
        // arrange
        var handler = NewHandler();
        var pool = new[] { NewMovie(9, genres: ["Drama"]), NewMovie(4, genres: ["Drama"]), NewMovie(11, genres: ["Drama"]), NewMovie(7, genres: ["Drama"]) };
        Arrange(NewMovie(1, genres: ["Drama"]), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 4, 7, 9, 11 });
    }

    [Fact]
    public async Task HandleAsync_DifferentScores_OrdersByDescendingScore()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var identical = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m);
        var partial = NewMovie(4, genres: ["Drama"]);
        var zero = NewMovie(3, withMetadata: false);
        Arrange(selected, new[] { zero, partial, identical });

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 2, 4, 3 });
        result[0].Score.ShouldBe(1.0);
        result[2].Score.ShouldBe(0.0);
    }

    [Fact]
    public async Task HandleAsync_ZeroScoreCandidate_IsRetainedWithinTake()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var pool = new[] { NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m), NewMovie(3, withMetadata: false) };
        Arrange(selected, pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 5), CancellationToken.None);

        // assert
        result.Count.ShouldBe(2);
        result[1].Summary.Id.ShouldBe(new MovieId(3));
        result[1].Score.ShouldBe(0.0);
    }

    [Fact]
    public async Task HandleAsync_Candidate_SummaryCarriesAllTenFields()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(
            7,
            title: "Seven",
            posterUrl: "https://posters/seven.jpg",
            releaseYear: 1999,
            runtimeMinutes: 101,
            imdbRating: 7.5m,
            genres: ["Drama"],
            isInWatchlist: true,
            isWatched: true,
            isNotInterested: true);
        Arrange(selected, new[] { candidate });

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

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
            true);
        result[0].Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_CandidateWithoutMetadata_SummaryKeepsNullMetadataScalars()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(8, title: "Eight", withMetadata: false);
        Arrange(selected, new[] { candidate });

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        var expected = new MovieSummary(new MovieId(8), "Eight", null, null, null, null, EnrichmentStatus.Enriched, false, false, false);
        result[0].Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_ScoredCandidate_PassesScoreAndReasonsThroughUnchanged()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m);
        Arrange(selected, new[] { candidate });
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            Weights,
            GenreWeights.Calculate(Counts));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        expected.Reasons.ShouldNotBeEmpty();
        result[0].Score.ShouldBe(expected.Score);
        result[0].Reasons.Count.ShouldBe(expected.Reasons.Count);
        for (var index = 0; index < expected.Reasons.Count; index++)
        {
            result[0].Reasons[index].ShouldBe(expected.Reasons[index]);
        }
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
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await candidateSource.DidNotReceive().GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SelectedLookupCancelsToken_ThrowsBeforeCandidateLoad()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromResult<Movie?>(Selected());
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await candidateSource.DidNotReceive().GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CandidateLoadCancelsToken_ThrowsBeforeGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        IReadOnlyList<Movie> pool = Pool(2);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Selected());
        candidateSource.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromResult(pool);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsCancelToken_ThrowsBeforeScoring()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        Arrange(Selected(), Pool(2));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromResult(Counts);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await catalog.Received(1).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledAfterFirstCandidateScored_StopsBeforeScoringRest()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        var pool = new CancellingCandidates(Pool(3), cts);
        Arrange(Selected(), pool);

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        pool.Yielded.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_SelectedLookupFails_PropagatesError()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("selected lookup failed");
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Movie?>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
    }

    [Fact]
    public async Task HandleAsync_CandidateLoadFails_PropagatesError()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("candidate load failed");
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Selected());
        candidateSource.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Movie>>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsFail_PropagatesError()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("genre counts failed");
        Arrange(Selected(), Pool(2));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GenreCounts>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
    }

    private GetRecommendationsQueryHandler NewHandler(int optionsTake = DefaultTake) =>
        new(
            movies,
            candidateSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions { Take = optionsTake, Weights = Weights }));

    private void Arrange(Movie selected, IReadOnlyList<Movie> pool)
    {
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(pool);
    }

    private static Movie Selected() => NewMovie(
        1,
        genres: ["Drama", "Action"],
        directors: ["Director A"],
        actors: ["Actor A"],
        writers: ["Writer A"],
        releaseYear: 2000,
        runtimeMinutes: 120,
        imdbRating: 8.0m);

    private static Movie[] Pool(int count) =>
        [.. Enumerable.Range(2, count).Select(id => NewMovie(id, genres: ["Drama"], directors: ["Director A"]))];

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
        EnrichmentStatus? status = null,
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
            status ?? EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            isNotInterested: isNotInterested);
    }

    private sealed class CancellingCandidates(IReadOnlyList<Movie> items, CancellationTokenSource source)
        : IReadOnlyList<Movie>
    {
        public int Yielded { get; private set; }

        public int Count => items.Count;

        public Movie this[int index] => items[index];

        public IEnumerator<Movie> GetEnumerator()
        {
            for (var index = 0; index < items.Count; index++)
            {
                if (index == 1)
                {
                    source.Cancel();
                }

                Yielded++;
                yield return items[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
