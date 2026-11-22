using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;
using LibrarySortDirection = LamuFlix.Core.Library.SortDirection;

namespace LamuFlix.Evaluation.RealLibrary;

public sealed class HiddenSeedEvaluationCopy
{
    private readonly RecommendationOptions options;
    private readonly TimeProvider timeProvider;

    private HiddenSeedEvaluationCopy(
        RealLibrarySnapshot snapshot,
        MovieId hiddenSeed,
        RecommendationOptions options,
        TimeProvider timeProvider)
    {
        this.options = options;
        this.timeProvider = timeProvider;
        HiddenSeed = hiddenSeed;
        Catalog = new SnapshotCatalog(snapshot, hiddenSeed);
        Repository = new SnapshotRepository(snapshot, hiddenSeed);
        Candidates = new SnapshotCandidateSource(snapshot, hiddenSeed);
        ProfileSource = new SnapshotProfileSource(
            [.. snapshot.ProfileMovies.Where(movie => movie.MovieId != hiddenSeed)],
            snapshot.PlotEmbeddings,
            snapshot.GenreWeights,
            timeProvider,
            options);
        GenreWeights = new GenreWeightsCalculator(Catalog, new GenreWeightsCache());
        KeywordWeights = new KeywordWeightsCalculator(Catalog, new KeywordWeightsCache());
    }

    public MovieId HiddenSeed { get; }

    public IMovieCatalog Catalog { get; }

    public IMovieRepository Repository { get; }

    private IRecommendationCandidateSource Candidates { get; }

    public IRecommendationProfileSource ProfileSource { get; }

    private GenreWeightsCalculator GenreWeights { get; }

    private KeywordWeightsCalculator KeywordWeights { get; }

    public static HiddenSeedEvaluationCopy Create(
        RealLibrarySnapshot snapshot,
        MovieId hiddenSeed,
        RecommendationOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(hiddenSeed);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (!snapshot.Contains(hiddenSeed))
        {
            throw new ArgumentException(
                $"Hidden seed {hiddenSeed.Value} is not part of the loaded library snapshot.",
                nameof(hiddenSeed));
        }

        return new HiddenSeedEvaluationCopy(snapshot, hiddenSeed, options, timeProvider);
    }

    public async Task<ImmutableList<RecommendedMovie>> RankAsync(int take, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        return (await CreateHandler().HandleAsync(new GetForYouRecommendationsQuery(take), ct)).Items;
    }

    private GetForYouRecommendationsQueryHandler CreateHandler() =>
        new(
            Candidates,
            ProfileSource,
            GenreWeights,
            KeywordWeights,
            timeProvider,
            Options.Create(options));

    private static Movie HiddenView(Movie movie)
    {
        ArgumentNullException.ThrowIfNull(movie);

        return Movie.Rehydrate(
            id: movie.Id,
            title: movie.Title,
            path: movie.Path,
            format: movie.Format,
            isInWatchlist: movie.IsInWatchlist,
            metadata: movie.Metadata,
            status: movie.Status,
            enrichedAt: movie.EnrichedAt,
            enrichmentAttempts: movie.EnrichmentAttempts,
            lastFailureCategory: movie.LastFailureCategory,
            lastAttemptAt: movie.LastAttemptAt,
            parsedReleaseYear: movie.ParsedReleaseYear,
            manualImdbId: movie.ManualImdbId,
            isWatched: false,
            review: Review.None,
            enrichmentDeferredUntil: movie.EnrichmentDeferredUntil);
    }

    private sealed class SnapshotCatalog(RealLibrarySnapshot snapshot, MovieId hiddenSeed) : IMovieCatalog
    {
        public Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            ct.ThrowIfCancellationRequested();
            RequireUnfiltered(query);

            var ordered = Order(snapshot.Movies, query);
            var page = query.Page;
            var items = page.Size <= 0
                ? ImmutableArray<MovieSummary>.Empty
                : [.. ordered
                    .Skip(Math.Max(0, (page.Number - 1) * page.Size))
                    .Take(page.Size)
                    .Select(movie => MovieSummaryMapper.From(View(movie)))];

            return Task.FromResult(new PagedResult<MovieSummary>(items, ordered.Length));
        }

        public Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<MovieDetails?>(null);
        }

        public Task<IReadOnlyList<GenreFacet>> GetGenresAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<GenreFacet>>([]);
        }

        public Task<IReadOnlyList<CollectionFacet>> GetCollectionsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<CollectionFacet>>([]);
        }

        public Task<GenreCounts> GetGenreCountsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(snapshot.GenreCounts);
        }

        public Task<KeywordCounts> GetKeywordCountsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(snapshot.KeywordCounts);
        }

        public Task<IReadOnlyList<PersonFacet>> GetPeopleAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<PersonFacet>>([]);
        }

        public Task<IReadOnlyList<DirectorFacet>> GetDirectorsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<DirectorFacet>>([]);
        }

        private Movie View(Movie movie) => movie.Id == hiddenSeed ? HiddenView(movie) : movie;

        private static void RequireUnfiltered(MovieQuery query)
        {
            if (query.Text is not null
                || !query.GenreIds.IsDefaultOrEmpty
                || !query.ActorIds.IsDefaultOrEmpty
                || query.Runtime is not null
                || query.Year is not null
                || query.Rating is not null
                || !query.Statuses.IsDefaultOrEmpty
                || query.InWatchlist is not null
                || query.CollectionId is not null
                || query.DirectorId is not null)
            {
                throw new NotSupportedException(
                    "The evaluation snapshot catalog only supports the unfiltered, sorted, paged browse "
                    + "the For-you handler performs.");
            }
        }

        private static ImmutableArray<Movie> Order(ImmutableArray<Movie> movies, MovieQuery query) =>
            query.Sort == MovieSort.Title
                ? [.. SortByTitle(movies, query.Direction)]
                : [.. movies.OrderBy(movie => movie.Id.Value)];

        private static IOrderedEnumerable<Movie> SortByTitle(
            ImmutableArray<Movie> movies,
            LibrarySortDirection? direction) =>
            direction == LibrarySortDirection.Descending
                ? movies.OrderByDescending(movie => movie.Title, StringComparer.Ordinal).ThenByDescending(movie => movie.Id.Value)
                : movies.OrderBy(movie => movie.Title, StringComparer.Ordinal).ThenBy(movie => movie.Id.Value);
    }

    private sealed class SnapshotCandidateSource(RealLibrarySnapshot snapshot, MovieId hiddenSeed)
        : IRecommendationCandidateSource
    {
        public Task<RecommendationLibrarySnapshot> GetLibraryFeaturesAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            IReadOnlyList<RecommendationLibraryEntry> entries =
            [
                .. snapshot.Movies.Select(movie =>
                {
                    var view = movie.Id == hiddenSeed ? HiddenView(movie) : movie;
                    return new RecommendationLibraryEntry(
                        MovieFeaturesMapper.From(
                            view,
                            includeSignals: true,
                            plotEmbedding: snapshot.PlotEmbeddings.GetValueOrDefault(view.Id)),
                        MovieSummaryMapper.From(view));
                }),
            ];

            return Task.FromResult(new RecommendationLibrarySnapshot(entries, CrowdLinks.Empty));
        }

        public Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(
            MovieId selectedMovieId,
            int? collectionId,
            CancellationToken ct) =>
            throw new NotSupportedException("The evaluation snapshot candidate source serves library features only.");
    }

    private sealed class SnapshotRepository(RealLibrarySnapshot snapshot, MovieId hiddenSeed) : IMovieRepository
    {
        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(id);
            ct.ThrowIfCancellationRequested();

            var match = snapshot.Movies.FirstOrDefault(movie => movie.Id == id);

            return Task.FromResult(match is null
                ? null
                : match.Id == hiddenSeed ? HiddenView(match) : match);
        }

        public Task AddAsync(Movie movie, CancellationToken ct) => throw ReadOnly();

        public Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct) => throw ReadOnly();

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => throw ReadOnly();

        public Task SaveChangesAsync(CancellationToken ct) => throw ReadOnly();

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => throw ReadOnly();

        public Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
            throw ReadOnly();

        public Task<IReadOnlyList<MovieId>> SnapshotMovieIdsAsync(CancellationToken ct) => throw ReadOnly();

        public Task<IReadOnlyList<LibraryPath>> SnapshotMoviePathsAsync(CancellationToken ct) => throw ReadOnly();

        public Task<IReadOnlyList<Movie>> FindFailedMoviesAsync(CancellationToken ct) => throw ReadOnly();

        public Task<string?> GetPlotAsync(MovieId id, CancellationToken ct) => throw ReadOnly();

        public Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct) => throw ReadOnly();

        private static InvalidOperationException ReadOnly() =>
            new("The evaluation snapshot repository is read-only; no library mutation is allowed.");
    }

    private sealed class SnapshotProfileSource(
        ImmutableArray<TasteProfileMovie> profileMovies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> plotEmbeddings,
        GenreWeights genreWeights,
        TimeProvider timeProvider,
        RecommendationOptions options) : IRecommendationProfileSource
    {
        public Task<RecommendationProfileInputs> GetProfileInputsAsync(
            IReadOnlyCollection<MovieId> candidateMovieIds,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(candidateMovieIds);
            ct.ThrowIfCancellationRequested();

            var candidateEmbeddings = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<float>>();

            foreach (var candidateMovieId in candidateMovieIds)
            {
                if (plotEmbeddings.TryGetValue(candidateMovieId, out var embedding))
                {
                    candidateEmbeddings[candidateMovieId] = embedding;
                }
            }

            return Task.FromResult(new RecommendationProfileInputs(
                profileMovies,
                candidateEmbeddings.ToImmutable()));
        }

        public Task<TasteProfile> GetTasteProfileAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var profile = new TasteProfileCalculator(timeProvider, options)
                .Calculate(profileMovies, genreWeights);

            return Task.FromResult(profile);
        }
    }
}
