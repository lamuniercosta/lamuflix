using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.Evaluation.RealLibrary;

public static class QualityReportRunner
{
    public const string BarsVariable = "LAMUFLIX_EVALUATION_BARS";
    public const string StrongOnlyBarVariable = "LAMUFLIX_EVALUATION_STRONG_ONLY_BAR";
    public const string JudgedPairsPathVariable = "LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH";
    private const string DefaultJudgedPairsRelative = "artifacts/recommendations/judged-pairs.json";
    public const double DefaultStrongOnlyBar = 0.225;
    public const string ScoreOnlyRule = "score-only";
    public const string ScoreOrStrongRule = "score-or-strong";

    public static readonly ImmutableArray<double> DefaultBars =
        [0.15, 0.20, 0.225, 0.25, 0.30, 0.35, 0.40];

    public static readonly ImmutableArray<string> StrongCriteria = ["Collection", "Director"];

    private static readonly ImmutableArray<string> Disclosures =
    [
        "Strong matches use only supported existing criteria (Collection, Director); Themes is not "
        + "synthesized before DEV-518 exists.",
        "The identity-fallback diagnostic entry is never strong-eligible.",
        "Coverage buckets count qualifying results per enriched selected movie across all other "
        + "enriched movies; each line's four buckets sum to the enriched selected population.",
        "Bands are [lower,upper) over adjacent sorted unique bars; samples are ordered by SHA-256 of "
        + "the UTF-8 invariant decimal selectedId:otherId encoded with no whitespace or newline.",
        "Undersupplied bands report the available count instead of padding samples to twelve.",
        "Judged pairs are directed; unknown or unenriched ids are skipped and excluded from "
        + "denominators; zero valid pairs yield null shares.",
    ];

    public static QualityParameters ReadParameters(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        return new QualityParameters(
            ParseBars(readVariable(BarsVariable)),
            ParseStrongOnlyBar(readVariable(StrongOnlyBarVariable)),
            ResolveJudgedPairsPath(readVariable(JudgedPairsPathVariable)));
    }

    private static ImmutableArray<double> ParseBars(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DefaultBars;
        }

        var bars = ImmutableArray.CreateBuilder<double>();
        var previous = double.NegativeInfinity;

        foreach (var token in text.Split(',', StringSplitOptions.TrimEntries))
        {
            var value = ParseFinite(token, BarsVariable);

            if (value <= 0.0)
            {
                throw new InvalidOperationException(
                    $"Evaluation parameter '{BarsVariable}' values must be positive; got '{text}'.");
            }

            if (value <= previous)
            {
                throw new InvalidOperationException(
                    $"Evaluation parameter '{BarsVariable}' values must be unique and strictly ascending; got '{text}'.");
            }

            previous = value;
            bars.Add(value);
        }

        if (bars.Count == 0)
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{BarsVariable}' must name at least one bar.");
        }

        return bars.ToImmutable();
    }

    private static double ParseStrongOnlyBar(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DefaultStrongOnlyBar;
        }

        var value = ParseFinite(text, StrongOnlyBarVariable);

        if (value <= 0.0)
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{StrongOnlyBarVariable}' must be a finite positive decimal; got '{text}'.");
        }

        return value;
    }

    private static string ResolveJudgedPairsPath(string? text) =>
        EvaluationPaths.Resolve(text, DefaultJudgedPairsRelative);

    private static double ParseFinite(string text, string name)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || !double.IsFinite(value))
        {
            throw new InvalidOperationException(
                $"Evaluation parameter '{name}' must be an invariant finite decimal; got '{text}'.");
        }

        return value;
    }

    public static QualityReport Run(
        RealLibrarySnapshot snapshot,
        RecommendationOptions options,
        QualityParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(parameters);

        cancellationToken.ThrowIfCancellationRequested();

        var judged = JudgedPairReader.ReadOrDefault(parameters.JudgedPairsPath);
        var ranking = Ranking.Build(snapshot, options, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return new QualityReport(
            parameters.Bars,
            parameters.StrongOnlyBar,
            ranking.SelectedCount,
            Coverage.Build(ranking, parameters.Bars),
            Bands.Build(ranking, parameters.Bars, parameters.StrongOnlyBar),
            JudgedPairs.Evaluate(ranking, judged, parameters.Bars),
            Disclosures);
    }

    // ReSharper disable once CompareOfFloatsByEqualityOperator // a strong match is exactly 1 (collection identity), not approximately 1
    private static bool IsStrong(SimilarityResult result) =>
        result.Breakdown.Any(criterion =>
            StrongCriteria.Contains(criterion.Name) && criterion.Value == 1.0);

    private sealed class Ranking
    {
        private Ranking(int selectedCount, ImmutableArray<SelectedRanking> selected)
        {
            SelectedCount = selectedCount;
            Selected = selected;
        }

        public int SelectedCount { get; }

        public ImmutableArray<SelectedRanking> Selected { get; }

        public static Ranking Build(
            RealLibrarySnapshot snapshot,
            RecommendationOptions options,
            CancellationToken cancellationToken)
        {
            var enriched = snapshot.Movies
                .Where(movie => movie.Status == EnrichmentStatus.Enriched)
                .ToImmutableArray();

            var selected = ImmutableArray.CreateBuilder<SelectedRanking>(enriched.Length);

            foreach (var movie in enriched)
            {
                cancellationToken.ThrowIfCancellationRequested();
                selected.Add(new SelectedRanking(movie.Id, RankAgainst(snapshot, options, movie, cancellationToken)));
            }

            return new Ranking(enriched.Length, selected.ToImmutable());
        }

        private static ImmutableArray<RankedResult> RankAgainst(
            RealLibrarySnapshot snapshot,
            RecommendationOptions options,
            Movie selected,
            CancellationToken cancellationToken)
        {
            var selectedFeatures = Features(snapshot, selected);
            var scored = ImmutableArray.CreateBuilder<RankedResult>();

            foreach (var movie in snapshot.Movies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (movie.Id == selected.Id || movie.Status != EnrichmentStatus.Enriched)
                {
                    continue;
                }

                scored.Add(new RankedResult(
                    movie.Id,
                    SimilarityScorer.Score(
                        selectedFeatures,
                        Features(snapshot, movie),
                        options,
                        snapshot.GenreWeights,
                        snapshot.KeywordCounts)));
            }

            var ordered = scored
                .OrderByDescending(result => result.Result.Score)
                .ThenBy(result => result.MovieId.Value)
                .ToImmutableArray();

            return [.. ordered.Select((result, index) => result with { Rank = index + 1 })];
        }

        private static MovieFeatures Features(RealLibrarySnapshot snapshot, Movie movie) =>
            MovieFeaturesMapper.From(
                movie,
                includeSignals: true,
                plotEmbedding: snapshot.PlotEmbeddings.TryGetValue(movie.Id, out var vector) ? vector : null);
    }

    private static class Coverage
    {
        public static ImmutableArray<CoverageLine> Build(Ranking ranking, ImmutableArray<double> bars)
        {
            var lines = ImmutableArray.CreateBuilder<CoverageLine>(bars.Length * 2);

            foreach (var bar in bars)
            {
                lines.Add(Line(ranking, bar, ScoreOnlyRule, static (result, barValue) => result.Score >= barValue));
                lines.Add(Line(
                    ranking,
                    bar,
                    ScoreOrStrongRule,
                    (result, barValue) => result.Score >= barValue || IsStrong(result.Result)));
            }

            return lines.ToImmutable();
        }

        private static CoverageLine Line(
            Ranking ranking,
            double bar,
            string rule,
            Func<RankedResult, double, bool> qualifies)
        {
            var buckets = new int[4];
            var firsts = ImmutableArray.CreateBuilder<double>();
            var tenths = ImmutableArray.CreateBuilder<double>();

            foreach (var selected in ranking.Selected)
            {
                var qualifying = selected.Results.Where(result => qualifies(result, bar)).ToImmutableArray();

                buckets[BucketIndex(qualifying.Length)]++;

                if (qualifying.Length > 0)
                {
                    firsts.Add(qualifying[0].Score);
                }

                if (qualifying.Length >= 10)
                {
                    tenths.Add(qualifying[9].Score);
                }
            }

            return new CoverageLine(
                bar,
                rule,
                new CoverageBuckets(buckets[0], buckets[1], buckets[2], buckets[3]),
                Median(firsts),
                firsts.Count,
                Median(tenths),
                tenths.Count);
        }

        private static int BucketIndex(int qualifyingCount) => qualifyingCount switch
        {
            0 => 0,
            <= 3 => 1,
            <= 10 => 2,
            _ => 3,
        };
    }

    private static class Bands
    {
        public static ImmutableArray<BandSample> Build(
            Ranking ranking,
            ImmutableArray<double> bars,
            double strongOnlyBar)
        {
            var samples = ImmutableArray.CreateBuilder<BandSample>();

            for (var index = 0; index < bars.Length - 1; index++)
            {
                var lower = bars[index];
                var upper = bars[index + 1];
                samples.Add(BandSampleSelector.Select(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[{0},{1})",
                        lower,
                        upper),
                    lower,
                    upper,
                    Candidates(ranking, result => result.Score >= lower && result.Score < upper)));
            }

            samples.Add(BandSampleSelector.Select(
                BandSampleSelector.StrongOnlyBandLabel,
                lowerInclusive: null,
                upperExclusive: strongOnlyBar,
                Candidates(ranking, result => result.Score < strongOnlyBar && IsStrong(result.Result))));

            return samples.ToImmutable();
        }

        private static IEnumerable<DirectedPair> Candidates(
            Ranking ranking,
            Func<RankedResult, bool> qualifies) =>
            ranking.Selected.SelectMany(
                selected => selected.Results
                    .Where(qualifies)
                    .Select(result => new DirectedPair(selected.SelectedId.Value, result.MovieId.Value)));
    }

    private static class JudgedPairs
    {
        public static JudgedPairReport Evaluate(
            Ranking ranking,
            ImmutableArray<JudgedPair> judgedPairs,
            ImmutableArray<double> bars)
        {
            var bySelected = ranking.Selected.ToDictionary(selected => selected.SelectedId.Value);

            var outcomes = ImmutableArray.CreateBuilder<JudgedPairOutcome>(judgedPairs.Length);
            var skipped = ImmutableArray.CreateBuilder<SkippedJudgedPair>();

            foreach (var pair in judgedPairs)
            {
                if (!bySelected.TryGetValue(pair.MovieId, out var selected))
                {
                    skipped.Add(new SkippedJudgedPair(pair, "selected movie is not in the enriched population"));
                    continue;
                }

                var result = selected.Results.FirstOrDefault(candidate => candidate.MovieId.Value == pair.OtherId);

                if (result is null)
                {
                    skipped.Add(new SkippedJudgedPair(pair, "other movie is not in the enriched population"));
                    continue;
                }

                outcomes.Add(new JudgedPairOutcome(
                    pair,
                    result.Rank,
                    result.Score,
                    result.Rank == 1,
                    result.Rank <= 3,
                    IsStrong(result.Result),
                    [.. bars.Where(bar => result.Score >= bar)]));
            }

            var valid = outcomes.ToImmutable();

            return new JudgedPairReport(
                valid,
                skipped.ToImmutable(),
                JudgedSharesCalculator.Compute(valid, bars, JudgedVerdict.Good, includeRankOne: true),
                JudgedSharesCalculator.Compute(valid, bars, JudgedVerdict.Bad, includeRankOne: false));
        }
    }

    private static double? Median(ImmutableArray<double>.Builder values)
    {
        var sorted = values
            .Where(double.IsFinite)
            .OrderBy(value => value)
            .ToImmutableArray();

        if (sorted.IsDefaultOrEmpty)
        {
            return null;
        }

        var middle = sorted.Length / 2;

        return sorted.Length % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2.0;
    }
}

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record QualityParameters(
    ImmutableArray<double> Bars,
    double StrongOnlyBar,
    string JudgedPairsPath);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record QualityReport(
    ImmutableArray<double> Bars,
    double StrongOnlyBar,
    int EnrichedSelectedCount,
    ImmutableArray<CoverageLine> Coverage,
    ImmutableArray<BandSample> Bands,
    JudgedPairReport JudgedPairs,
    ImmutableArray<string> Disclosures);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record CoverageLine(
    double Bar,
    string Rule,
    CoverageBuckets Buckets,
    double? MedianFirstScore,
    int FirstScoreCount,
    double? MedianTenthScore,
    int TenthScoreCount);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record CoverageBuckets(int Zero, int OneToThree, int FourToTen, int AboveTen)
{
    public int Total => Zero + OneToThree + FourToTen + AboveTen;
}

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record JudgedPairOutcome(
    JudgedPair Pair,
    int Rank,
    double Score,
    bool IsRankOne,
    bool IsTopThree,
    bool IsStrong,
    ImmutableArray<double> BarsMet);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record SkippedJudgedPair(JudgedPair Pair, string Reason);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record JudgedPairReport(
    ImmutableArray<JudgedPairOutcome> Valid,
    ImmutableArray<SkippedJudgedPair> Skipped,
    JudgedShares Good,
    JudgedShares Bad);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record JudgedShares(
    int ValidCount,
    double? RankOneShare,
    double? TopThreeShare,
    double? StrongShare,
    ImmutableArray<BarShare> BarShares);

// ReSharper disable once NotAccessedPositionalProperty.Global // positional report contract consumed by serializers and tests
public sealed record BarShare(double Bar, double? Share);

internal sealed record RankedResult(MovieId MovieId, SimilarityResult Result, int Rank = 0)
{
    public double Score => Result.Score;
}

internal sealed record SelectedRanking(MovieId SelectedId, ImmutableArray<RankedResult> Results);
