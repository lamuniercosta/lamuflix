using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Evaluation.RealLibrary;

public static class CompareRunner
{
    private const string PoolDisclosure =
        "Pool membership uses the ticket diagnostic formula (plotRank is not null && plotRank <= "
        + "CandidatePoolSize) OR same collection; it is not production candidate-pool parity.";

    private const string UnusableSelectedVectorDisclosure =
        "The selected movie has no usable persisted plot vector, so every raw plot cosine and plot "
        + "rank is null and pool membership falls back to the same-collection term only; production "
        + "returns no candidates in that state.";

    public static CompareReport Run(
        RealLibrarySnapshot snapshot,
        RecommendationOptions options,
        int selectedId,
        int? otherId,
        CancellationToken cancellationToken = default,
        TmdbOptions? tmdbOptions = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        var selectedMovieId = RequireSelected(selectedId);
        var otherMovieId = ValidateOther(otherId, selectedMovieId);
        var selected = FindSelected(snapshot, selectedMovieId);
        var selectedVectorUsable = TryUsableVector(snapshot, selectedMovieId, out _);

        var scored = ScoreEnriched(snapshot, options, selected, tmdbOptions, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var ordered = OrderRows(scored, PlotRanks(scored), options.CandidatePoolSize).ToList();
        var rows = KeepPair(ordered, otherMovieId);
        var matchedNames = otherMovieId is null
            ? ImmutableArray<RecommendationMatchedName>.Empty
            : MatchedNames(selected, rows[0].Movie);

        return new CompareReport(
            BuildEnvelope(selectedMovieId, otherMovieId, rows, matchedNames),
            selectedMovieId.Value,
            otherMovieId?.Value,
            selectedVectorUsable,
            otherMovieId is not null,
            Disclosures(selectedVectorUsable),
            matchedNames);
    }

    private static MovieId RequireSelected(int selectedId)
    {
        if (selectedId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectedId),
                selectedId,
                "The selected movie id must be a positive integer.");
        }

        return new MovieId(selectedId);
    }

    private static MovieId? ValidateOther(int? otherId, MovieId selectedMovieId)
    {
        if (otherId is null)
        {
            return null;
        }

        if (otherId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(otherId),
                otherId,
                "The other movie id must be a positive integer when supplied.");
        }

        if (otherId == selectedMovieId.Value)
        {
            throw new ArgumentException(
                "The other movie id must differ from the selected movie id.",
                nameof(otherId));
        }

        return new MovieId(otherId.Value);
    }

    private static Movie FindSelected(RealLibrarySnapshot snapshot, MovieId selectedMovieId) =>
        snapshot.Movies.FirstOrDefault(movie => movie.Id == selectedMovieId)
        ?? throw new InvalidOperationException(
            $"The selected movie {selectedMovieId.Value} is not part of the loaded library snapshot.");

    private static ImmutableArray<CompareRow> KeepPair(List<CompareRow> ordered, MovieId? otherMovieId)
    {
        if (otherMovieId is null)
        {
            return [.. ordered];
        }

        var pair = ordered.Where(row => row.Movie.Id == otherMovieId).ToImmutableArray();

        if (pair.IsEmpty)
        {
            throw new InvalidOperationException(
                $"Movie {otherMovieId.Value} is not an enriched library movie other than the selected "
                + "movie, so there is no pair to inspect.");
        }

        return [.. pair.Select(row => row with { Rank = 1 })];
    }

    private static List<ScoredMovie> ScoreEnriched(
        RealLibrarySnapshot snapshot,
        RecommendationOptions options,
        Movie selected,
        TmdbOptions? tmdbOptions,
        CancellationToken cancellationToken)
    {
        var selectedFeatures = Features(snapshot, selected);
        var crowdScoring = CrowdScoring.Resolve(options, tmdbOptions ?? new TmdbOptions(), snapshot.CrowdLinks);
        var scored = new List<ScoredMovie>();

        foreach (var movie in snapshot.Movies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (movie.Id == selected.Id || movie.Status != EnrichmentStatus.Enriched)
            {
                continue;
            }

            scored.Add(new ScoredMovie(
                movie,
                SimilarityScorer.Score(
                    selectedFeatures,
                    Features(snapshot, movie),
                    options,
                    snapshot.GenreWeights,
                    snapshot.KeywordCounts,
                    crowdScoring,
                    selected.Id,
                    movie.Id),
                PlotCosine(snapshot, selected.Id, movie.Id),
                SharesCollection(selected, movie)));
        }

        return scored;
    }

    private static MovieFeatures Features(RealLibrarySnapshot snapshot, Movie movie) =>
        MovieFeaturesMapper.From(
            movie,
            includeSignals: true,
            plotEmbedding: snapshot.PlotEmbeddings.TryGetValue(movie.Id, out var vector) ? vector : null);

    private static ImmutableDictionary<MovieId, int> PlotRanks(List<ScoredMovie> scored)
    {
        var builder = ImmutableDictionary.CreateBuilder<MovieId, int>();

        var ranked = scored
            .Where(movie => movie.PlotCosineValue is not null)
            .OrderByDescending(movie => movie.PlotCosineValue.GetValueOrDefault())
            .ThenBy(movie => movie.Movie.Id.Value)
            .ToList();

        for (var index = 0; index < ranked.Count; index++)
        {
            builder[ranked[index].Movie.Id] = index + 1;
        }

        return builder.ToImmutable();
    }

    private static IEnumerable<CompareRow> OrderRows(
        List<ScoredMovie> scored,
        ImmutableDictionary<MovieId, int> plotRanks,
        int candidatePoolSize) =>
        scored
            .OrderByDescending(movie => movie.Result.Score)
            .ThenBy(movie => movie.Movie.Id.Value)
            .Select((movie, index) => ToRow(movie, index + 1, plotRanks, candidatePoolSize));

    private static CompareRow ToRow(
        ScoredMovie movie,
        int rank,
        ImmutableDictionary<MovieId, int> plotRanks,
        int candidatePoolSize)
    {
        var plotRank = plotRanks.TryGetValue(movie.Movie.Id, out var known) ? known : (int?)null;
        var inPool = (plotRank is not null && plotRank.Value <= candidatePoolSize) || movie.IsSameCollection;

        return new CompareRow(rank, movie.Movie, movie.Result, movie.PlotCosineValue, plotRank, inPool);
    }

    private static RecommendationArtifactEnvelope BuildEnvelope(
        MovieId selectedMovieId,
        MovieId? otherMovieId,
        ImmutableArray<CompareRow> rows,
        ImmutableArray<RecommendationMatchedName> matchedNames)
    {
        var targets = otherMovieId is null
            ? ImmutableArray.Create(selectedMovieId.Value)
            : ImmutableArray.Create(selectedMovieId.Value, otherMovieId.Value);

        var artifactRows = rows
            .Select(row => new RecommendationArtifactRow(
                row.Rank,
                row.Movie.Id.Value,
                row.Movie.Title,
                row.Result.Score,
                ArtifactReasons(row.Result.Reasons),
                new RecommendationPairDiagnostics(
                    ArtifactBreakdown(row.Result.Breakdown),
                    row.PlotCosineValue,
                    row.PlotRank,
                    row.InPool)))
            .ToImmutableArray();

        return new RecommendationArtifactEnvelope(
            RecommendationArtifactContract.CompareMode,
            targets,
            [
                new RecommendationArtifactList(
                    RecommendationArtifactContract.CompareListKind,
                    selectedMovieId.Value,
                    null,
                    artifactRows.IsDefaultOrEmpty
                        ? RecommendationArtifactContract.EmptyStatus
                        : RecommendationArtifactContract.OkStatus,
                    artifactRows)
                {
                    MatchedNames = matchedNames,
                },
            ]);
    }

    private static ImmutableArray<RecommendationArtifactReason> ArtifactReasons(ImmutableList<Reason> reasons) =>
        [
            .. reasons.Select(reason => new RecommendationArtifactReason(
                reason.Kind == ReasonKind.Personal
                    ? RecommendationArtifactContract.PersonalReasonKind
                    : RecommendationArtifactContract.StructuralReasonKind,
                reason.Text)),
        ];

    private static ImmutableArray<RecommendationCriterionContribution> ArtifactBreakdown(
        ImmutableList<CriterionContribution> breakdown) =>
        [
            .. breakdown.Select(criterion => new RecommendationCriterionContribution(
                criterion.Name,
                criterion.Value,
                criterion.Weight,
                criterion.Contribution)),
        ];

    private static ImmutableArray<string> Disclosures(bool selectedVectorUsable) =>
        selectedVectorUsable
            ? [PoolDisclosure]
            : [PoolDisclosure, UnusableSelectedVectorDisclosure];

    private static ImmutableArray<RecommendationMatchedName> MatchedNames(Movie selected, Movie other)
    {
        var builder = ImmutableArray.CreateBuilder<RecommendationMatchedName>();

        AddMatches(builder, "Director", selected.Metadata?.Directors, other.Metadata?.Directors);
        AddMatches(builder, "Themes", KeywordNames(selected), KeywordNames(other));
        AddMatches(builder, "Cast", selected.Metadata?.Actors, other.Metadata?.Actors);
        AddMatches(builder, "Genre", selected.Metadata?.Genres, other.Metadata?.Genres);
        AddCollection(builder, selected, other);

        return
        [
            .. builder
                .OrderBy(matched => matched.Criterion, StringComparer.Ordinal)
                .ThenBy(matched => matched.Name, StringComparer.Ordinal),
        ];
    }

    private static void AddMatches(
        ImmutableArray<RecommendationMatchedName>.Builder builder,
        string criterion,
        IEnumerable<string>? first,
        IEnumerable<string>? second)
    {
        if (first is null || second is null)
        {
            return;
        }

        var others = new HashSet<string>(second, StringComparer.OrdinalIgnoreCase);

        foreach (var name in first.Where(name => others.Contains(name)).Order(StringComparer.Ordinal))
        {
            builder.Add(new RecommendationMatchedName(criterion, name));
        }
    }

    private static void AddCollection(
        ImmutableArray<RecommendationMatchedName>.Builder builder,
        Movie selected,
        Movie other)
    {
        var collection = selected.Metadata?.CollectionName;

        if (collection is not null
            && string.Equals(collection, other.Metadata?.CollectionName, StringComparison.Ordinal))
        {
            builder.Add(new RecommendationMatchedName("Collection", collection));
        }
    }

    private static IEnumerable<string>? KeywordNames(Movie movie) =>
        movie.Metadata?.Signals?.Keywords.Select(keyword => keyword.Name);

    private static bool SharesCollection(Movie selected, Movie other) =>
        selected.Metadata?.CollectionId is { } collectionId
        && collectionId == other.Metadata?.CollectionId;

    private static bool TryUsableVector(
        RealLibrarySnapshot snapshot,
        MovieId movieId,
        out ImmutableArray<float> vector)
    {
        if (snapshot.PlotEmbeddings.TryGetValue(movieId, out var stored) && IsUsableVector(stored))
        {
            vector = stored;
            return true;
        }

        vector = ImmutableArray<float>.Empty;
        return false;
    }

    private static double? PlotCosine(RealLibrarySnapshot snapshot, MovieId selectedMovieId, MovieId movieId)
    {
        if (!TryUsableVector(snapshot, selectedMovieId, out var selectedVector)
            || !TryUsableVector(snapshot, movieId, out var candidateVector))
        {
            return null;
        }

        return Cosine(selectedVector, candidateVector);
    }

    private static double Cosine(ImmutableArray<float> first, ImmutableArray<float> second)
    {
        var dot = 0.0;
        var firstNorm = 0.0;
        var secondNorm = 0.0;

        for (var index = 0; index < first.Length; index++)
        {
            dot += (double)first[index] * second[index];
            firstNorm += (double)first[index] * first[index];
            secondNorm += (double)second[index] * second[index];
        }

        var denominator = Math.Sqrt(firstNorm) * Math.Sqrt(secondNorm);
        var value = dot / denominator;

        return double.IsFinite(value) ? Math.Clamp(value, -1.0, 1.0) : 0.0;
    }

    private static bool IsUsableVector(ImmutableArray<float> vector)
    {
        if (vector.IsDefaultOrEmpty || vector.Length != EmbeddingDimensions.Value)
        {
            return false;
        }

        var normSquared = 0.0;

        foreach (var coordinate in vector)
        {
            if (!float.IsFinite(coordinate))
            {
                return false;
            }

            normSquared += (double)coordinate * coordinate;
        }

        return normSquared > 0.0;
    }

    private sealed record ScoredMovie(
        Movie Movie,
        SimilarityResult Result,
        double? PlotCosineValue,
        bool IsSameCollection);

    private sealed record CompareRow(
        int Rank,
        Movie Movie,
        SimilarityResult Result,
        double? PlotCosineValue,
        int? PlotRank,
        bool InPool);
}

public sealed record CompareReport(
    RecommendationArtifactEnvelope Envelope,
    int SelectedId,
    int? OtherId,
    bool SelectedVectorUsable,
    bool IsPairInspect,
    ImmutableArray<string> Disclosures,
    ImmutableArray<RecommendationMatchedName> MatchedNames);
