using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class GetForYouRecommendationsQueryHandler(
    IMovieCatalog catalog,
    IMovieRepository movies,
    IRecommendationProfileSource profileSource,
    GenreWeightsCalculator genreWeightsCalculator,
    IOptions<RecommendationOptions> options)
    : IQueryHandler<GetForYouRecommendationsQuery, ImmutableList<RecommendedMovie>>
{
    private const int BrowsePageSize = 100;
    private const string NoSharedFeaturesReason = "No shared features identified";
    private const string ViewingHistoryReason = "Matches your viewing history";
    private const string GenreInterestTemplate = "Matches your {0} interest";

    public async Task<ImmutableList<RecommendedMovie>> HandleAsync(
        GetForYouRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var eligible = await AcquireEligibleMoviesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (eligible.Count == 0)
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var profileInputs = await profileSource.GetProfileInputsAsync(
            [.. eligible.Select(movie => movie.Id)],
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (!HasPositiveSignal(profileInputs.Movies))
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profile = await profileSource.GetTasteProfileAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var recommendationOptions = options.Value;
        var scored = Score(
            eligible,
            profile,
            profileInputs.CandidateEmbeddings,
            genreWeights,
            recommendationOptions,
            cancellationToken);

        var relevanceOffset = RelevanceOffset(scored);
        var selectedIds = MmrSelector.Select(
            [.. scored.Select(item => new MmrCandidate(item.Movie.Id, item.Result.Score + relevanceOffset, item.Features))],
            query.Take,
            recommendationOptions.MmrLambda,
            recommendationOptions.Weights,
            genreWeights);

        var byId = scored.ToDictionary(item => item.Movie.Id);

        return
        [
            .. selectedIds
                .Select(id => byId[id])
                .Select(item => new RecommendedMovie(
                    Summarize(item.Movie),
                    item.Result.Score,
                    item.Result.Reasons)),
        ];
    }

    private async Task<List<Movie>> AcquireEligibleMoviesAsync(CancellationToken cancellationToken)
    {
        var eligible = new List<Movie>();
        var seen = new HashSet<MovieId>();

        for (var pageNumber = 1; ; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await catalog.BrowseAsync(
                new MovieQuery
                {
                    Sort = MovieSort.Title,
                    Direction = SortDirection.Ascending,
                    Page = new Page(pageNumber, BrowsePageSize),
                },
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var summary in page.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsEligible(summary.IsWatched, summary.Review))
                {
                    await AddEligibleAsync(eligible, seen, summary.Id, cancellationToken);
                }
            }

            if (page.Items.Length < BrowsePageSize)
            {
                return eligible;
            }
        }
    }

    private async Task AddEligibleAsync(
        List<Movie> eligible,
        HashSet<MovieId> seen,
        MovieId id,
        CancellationToken cancellationToken)
    {
        if (!seen.Add(id))
        {
            return;
        }

        var movie = await movies.GetAsync(id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (movie is not null && IsEligible(movie.IsWatched, movie.Review))
        {
            eligible.Add(movie);
        }
    }

    private static bool IsEligible(bool isWatched, Review review) =>
        !isWatched && !review.IsDisliked;

    private static bool HasPositiveSignal(ImmutableArray<TasteProfileMovie> movies)
    {
        if (movies.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var movie in movies)
        {
            if (movie.IsWatched || !movie.PlayedAt.IsDefaultOrEmpty || movie.Review.IsLiked)
            {
                return true;
            }
        }

        return false;
    }

    private static List<ScoredCandidate> Score(
        IReadOnlyList<Movie> eligible,
        TasteProfile profile,
        ImmutableDictionary<MovieId, ImmutableArray<float>> candidateEmbeddings,
        GenreWeights genreWeights,
        RecommendationOptions options,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(eligible.Count);

        foreach (var movie in eligible)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var features = MovieFeaturesMapper.From(movie);
            var genres = RecommendationScoring.Genres(features);
            var embedding = candidateEmbeddings.GetValueOrDefault(movie.Id, ImmutableArray<float>.Empty);

            var profileSimilarity = ProfileSimilarityCalculator.Evaluate(
                genres,
                embedding,
                profile,
                genreWeights);

            var reviewAffinity = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                genres,
                RecommendationScoring.Cast(features),
                RecommendationScoring.Directors(features),
                embedding,
                profile,
                options.Weights,
                genreWeights);

            var contribution = RecommendationScoring.ProfileContribution(
                profileSimilarity.Similarity,
                reviewAffinity,
                options);

            var score = BlendScorer.ScoreWithProfileContribution(
                0.0,
                contribution,
                movie.IsWatched ? 1.0 : 0.0,
                0.0,
                options);

            scored.Add(new ScoredCandidate(
                movie,
                features,
                new SimilarityResult(
                    score,
                    BuildReasons(profileSimilarity, reviewAffinity, contribution, options))));
        }

        return scored;
    }

    private static double RelevanceOffset(List<ScoredCandidate> scored)
    {
        var minimum = scored[0].Result.Score;

        foreach (var item in scored)
        {
            if (item.Result.Score < minimum)
            {
                minimum = item.Result.Score;
            }
        }

        return minimum < 0.0 ? -minimum : 0.0;
    }

    private static ImmutableList<Reason> BuildReasons(
        ProfileSimilarityEvidence profileSimilarity,
        ReviewAffinityEvidence reviewAffinity,
        double profileContribution,
        RecommendationOptions options)
    {
        var builder = ImmutableList.CreateBuilder<Reason>();

        if (options.BlendProfileWeight > 0.0)
        {
            AddPersonalReasons(builder, profileSimilarity);
            RecommendationReasonBuilder.AddLikedReviewReason(
                builder,
                reviewAffinity,
                profileContribution,
                options);
        }

        EnsureFallbackReason(builder);

        return builder.ToImmutable();
    }

    private static void AddPersonalReasons(
        ImmutableList<Reason>.Builder builder,
        ProfileSimilarityEvidence profileSimilarity)
    {
        foreach (var genre in profileSimilarity.OverlappingGenres)
        {
            var display = RecommendationReasonBuilder.Sanitize(genre);
            if (display.Length > 0)
            {
                builder.Add(Reason.Personal(
                    string.Format(CultureInfo.InvariantCulture, GenreInterestTemplate, display)));
            }
        }

        if (profileSimilarity is { HasPlotChannel: true, PlotSimilarity: > 0.0 })
        {
            builder.Add(Reason.Personal(ViewingHistoryReason));
        }
    }

    private static void EnsureFallbackReason(ImmutableList<Reason>.Builder builder)
    {
        if (builder.Count == 0)
        {
            builder.Add(Reason.Structural(NoSharedFeaturesReason));
        }
    }

    private static MovieSummary Summarize(Movie movie)
    {
        var metadata = movie.Metadata;

        return new MovieSummary(
            movie.Id,
            movie.Title,
            PosterUrl(metadata),
            ReleaseYear(metadata),
            RuntimeMinutes(metadata),
            ImdbRating(metadata),
            movie.Status,
            movie.IsInWatchlist,
            movie.IsWatched,
            movie.Review);
    }

    private static string? PosterUrl(MovieMetadata? metadata) => metadata?.PosterUrl;

    private static int? ReleaseYear(MovieMetadata? metadata) => metadata?.ReleaseYear?.Value;

    private static int? RuntimeMinutes(MovieMetadata? metadata) => metadata?.Runtime?.Minutes;

    private static decimal? ImdbRating(MovieMetadata? metadata) => metadata?.ImdbRating?.Value;

    private readonly record struct ScoredCandidate(
        Movie Movie,
        MovieFeatures Features,
        SimilarityResult Result);
}
