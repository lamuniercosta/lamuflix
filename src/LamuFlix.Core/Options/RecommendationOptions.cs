using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record RecommendationOptions : IValidatableObject
{
    public const string SectionName = "Recommendations";

    private const int DefaultMaxPerBestSeed = 3;

    private const int DefaultMaxPerCollection = 1;

    private const int DefaultMaxExcludeCount = 50;

    public const double DefaultSeedWeightLiked = 1.0;

    public const double DefaultSeedWeightWatched = 0.6;

    public const double DefaultSeedWeightWatchlist = 0.4;

    public const double DefaultDislikedSeedWeight = 1.0;

    public const double DefaultRecencyFloor = 0.25;

    private const double DefaultFirstSeedRankMultiplier = 1.0;

    private const double DefaultSecondSeedRankMultiplier = 0.5;

    private const double DefaultThirdSeedRankMultiplier = 0.25;

    private static readonly string[] DefaultStrongReasonCriteria = ["Collection", "Director", "Themes"];

    private static readonly string[] SupportedStrongReasonCriteria = ["Collection", "Director", "Themes"];

    [Range(1, int.MaxValue)]
    public int Take { get; init; } = 10;

    [Range(1, int.MaxValue)]
    public int CandidatePoolSize { get; init; } = 50;

    private const double DefaultMinimumSimilarity = 0.225;

    public double MinimumSimilarity { get; init; } = DefaultMinimumSimilarity;

    public IReadOnlyList<string>? StrongReasonCriteria { get; init; } = DefaultStrongReasonCriteria;

    [Range(1, int.MaxValue)]
    public int MaxPerBestSeed { get; init; } = DefaultMaxPerBestSeed;

    [Range(1, int.MaxValue)]
    public int MaxPerCollection { get; init; } = DefaultMaxPerCollection;

    [Range(0, int.MaxValue)]
    public int MaxExcludeCount { get; init; } = DefaultMaxExcludeCount;

    [Range(0.0, 1.0)]
    public double MmrLambda { get; init; } = 0.7;

    public double RecencyDecayLambda { get; init; } = Math.Log(2.0) / 30.0;

    public IReadOnlyList<string>? ThemeStoplist { get; init; } =
        ["aftercreditsstinger", "duringcreditsstinger", "sequel", "3d animation"];

    public int MinimumKeywordMovies { get; init; } = 2;

    public double ThemesCeiling { get; init; } = 0.35;

    public double PlotFloor { get; init; } = 0.16;

    public double PlotCeiling { get; init; } = 0.45;

    public double ThemesDisplayThreshold { get; init; } = 0.3;

    public double PlotDisplayThreshold { get; init; } = 0.5;

    public double GenreDisplayThreshold { get; init; } = 0.5;

    public double BlendSelectedWeight { get; init; } = 1.0;

    public double AlreadyWatchedPenalty { get; init; } = 0.25;

    [Range(0.0, double.MaxValue)]
    public double SeedWeightLiked { get; init; } = DefaultSeedWeightLiked;

    [Range(0.0, double.MaxValue)]
    public double SeedWeightWatched { get; init; } = DefaultSeedWeightWatched;

    [Range(0.0, double.MaxValue)]
    public double SeedWeightWatchlist { get; init; } = DefaultSeedWeightWatchlist;

    [Range(0.0, double.MaxValue)]
    public double DislikedSeedWeight { get; init; } = DefaultDislikedSeedWeight;

    [Range(0.0, 1.0)]
    public double RecencyFloor { get; init; } = DefaultRecencyFloor;

    public IReadOnlyList<double>? SeedRankMultipliers { get; init; } =
        [DefaultFirstSeedRankMultiplier, DefaultSecondSeedRankMultiplier, DefaultThirdSeedRankMultiplier];

    public SimilarityWeights Weights { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ValidateCore())
        {
            yield return result;
        }

        foreach (var result in ValidateBlend())
        {
            yield return result;
        }

        foreach (var result in ValidateNearestSeed())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateNearestSeed()
    {
        foreach (var result in ValidateSeedWeights())
        {
            yield return result;
        }

        foreach (var result in ValidateRankMultipliers())
        {
            yield return result;
        }

        foreach (var result in ValidateRecencyFloor())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateSeedWeights()
    {
        foreach (var result in ValidateSeedWeight(SeedWeightLiked, nameof(SeedWeightLiked)))
        {
            yield return result;
        }

        foreach (var result in ValidateSeedWeight(SeedWeightWatched, nameof(SeedWeightWatched)))
        {
            yield return result;
        }

        foreach (var result in ValidateSeedWeight(SeedWeightWatchlist, nameof(SeedWeightWatchlist)))
        {
            yield return result;
        }

        foreach (var result in ValidateSeedWeight(DislikedSeedWeight, nameof(DislikedSeedWeight)))
        {
            yield return result;
        }
    }

    private static IEnumerable<ValidationResult> ValidateSeedWeight(double value, string memberName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be finite and greater than or equal to 0.0; got {1}.",
                    memberName,
                    value),
                [memberName]);
        }
    }

    private IEnumerable<ValidationResult> ValidateRankMultipliers()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - configuration binder can set the collection to null via reflection.
        if (SeedRankMultipliers is null)
        {
            yield return new ValidationResult(
                $"The {nameof(SeedRankMultipliers)} field is required.",
                [nameof(SeedRankMultipliers)]);
            yield break;
        }

        if (SeedRankMultipliers.Count == 0)
        {
            yield return new ValidationResult(
                $"{nameof(SeedRankMultipliers)} must contain at least one multiplier.",
                [nameof(SeedRankMultipliers)]);
            yield break;
        }

        var previous = double.PositiveInfinity;
        for (var index = 0; index < SeedRankMultipliers.Count; index++)
        {
            if (RankMultiplierError(SeedRankMultipliers[index], index, previous) is { } failure)
            {
                yield return failure;
                yield break;
            }

            previous = SeedRankMultipliers[index];
        }
    }

    private static ValidationResult? RankMultiplierError(double multiplier, int index, double previous)
    {
        if (!double.IsFinite(multiplier) || multiplier < 0.0)
        {
            return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}[{1}] must be finite and greater than or equal to 0.0; got {2}.",
                    nameof(SeedRankMultipliers),
                    index,
                    multiplier),
                [nameof(SeedRankMultipliers)]);
        }

        return multiplier > previous
            ? new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be non-increasing; entry {1} ({2}) exceeds the previous entry ({3}).",
                    nameof(SeedRankMultipliers),
                    index,
                    multiplier,
                    previous),
                [nameof(SeedRankMultipliers)])
            : null;
    }

    private IEnumerable<ValidationResult> ValidateRecencyFloor()
    {
        if (!double.IsFinite(RecencyFloor) || RecencyFloor <= 0.0 || RecencyFloor > 1.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be finite and greater than 0.0 and at most 1.0; got {1}.",
                    nameof(RecencyFloor),
                    RecencyFloor),
                [nameof(RecencyFloor)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateCore()
    {
        foreach (var result in ValidateDecay())
        {
            yield return result;
        }

        foreach (var result in ValidateWeights())
        {
            yield return result;
        }

        foreach (var result in ValidateScoring())
        {
            yield return result;
        }

        foreach (var result in ValidatePool())
        {
            yield return result;
        }

        foreach (var result in ValidateStrongMatch())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateStrongMatch()
    {
        foreach (var result in ValidateMinimumSimilarity())
        {
            yield return result;
        }

        foreach (var result in ValidateStrongReasonCriteria())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateMinimumSimilarity()
    {
        if (!double.IsFinite(MinimumSimilarity) || MinimumSimilarity is < 0.0 or > 1.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be finite and between 0.0 and 1.0; got {1}.",
                    nameof(MinimumSimilarity),
                    MinimumSimilarity),
                [nameof(MinimumSimilarity)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateStrongReasonCriteria()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - configuration binder can set the collection to null via reflection.
        if (StrongReasonCriteria is null)
        {
            yield return new ValidationResult(
                $"The {nameof(StrongReasonCriteria)} field is required.",
                [nameof(StrongReasonCriteria)]);
            yield break;
        }

        var supported = new HashSet<string>(SupportedStrongReasonCriteria, StringComparer.OrdinalIgnoreCase);
        foreach (var criterion in StrongReasonCriteria)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - entries can be null when constructed directly, so validate defensively.
            if (criterion is null)
            {
                yield return new ValidationResult(
                    $"{nameof(StrongReasonCriteria)} cannot contain null entries.",
                    [nameof(StrongReasonCriteria)]);
                yield break;
            }

            if (!supported.Contains(criterion))
            {
                yield return new ValidationResult(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} entry '{1}' is not supported; allowed values are {2}.",
                        nameof(StrongReasonCriteria),
                        criterion,
                        string.Join(", ", SupportedStrongReasonCriteria)),
                    [nameof(StrongReasonCriteria)]);
                yield break;
            }
        }
    }

    private IEnumerable<ValidationResult> ValidateBlend()
    {
        foreach (var result in ValidateBlendCoefficients())
        {
            yield return result;
        }

        foreach (var result in ValidateBlendSignal())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateDecay()
    {
        if (!double.IsFinite(RecencyDecayLambda) || RecencyDecayLambda <= 0.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be finite and greater than 0.0; got {1}.",
                    nameof(RecencyDecayLambda),
                    RecencyDecayLambda),
                [nameof(RecencyDecayLambda)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateWeights()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - configuration binder can set Weights to null via reflection.
        if (Weights is null)
        {
            yield return new ValidationResult(
                $"The {nameof(Weights)} field is required.",
                [nameof(Weights)]);
            yield break;
        }

        var nested = new List<ValidationResult>();
        Validator.TryValidateObject(Weights, new ValidationContext(Weights), nested, validateAllProperties: true);
        foreach (var failure in nested)
        {
            yield return failure;
        }
    }

    private IEnumerable<ValidationResult> ValidateScoring()
    {
        foreach (var result in ValidateWeightTotal())
        {
            yield return result;
        }

        foreach (var result in ValidateThemesCeiling())
        {
            yield return result;
        }

        foreach (var result in ValidatePlotBounds())
        {
            yield return result;
        }

        foreach (var result in ValidateDisplayThresholds())
        {
            yield return result;
        }

        foreach (var result in ValidateMinimumKeywordMovies())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateWeightTotal()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - configuration binder can set Weights to null via reflection.
        if (Weights is null)
        {
            yield break;
        }

        var total = Weights.Collection + Weights.Director + Weights.Themes + Weights.Plot + Weights.Cast + Weights.Genres;
        if (!double.IsFinite(total) || total <= 0.0)
        {
            yield return new ValidationResult(
                "The sum of the six similarity weights must be finite and greater than 0.0.",
                [nameof(Weights)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateThemesCeiling()
    {
        if (!double.IsFinite(ThemesCeiling) || ThemesCeiling <= 0.0)
        {
            yield return new ValidationResult("ThemesCeiling must be finite and greater than 0.0.", [nameof(ThemesCeiling)]);
        }
    }

    private IEnumerable<ValidationResult> ValidatePlotBounds()
    {
        if (!double.IsFinite(PlotFloor) || PlotFloor is < -1.0 or > 1.0)
        {
            yield return new ValidationResult("PlotFloor must be finite and between -1.0 and 1.0.", [nameof(PlotFloor)]);
        }

        if (!double.IsFinite(PlotCeiling) || PlotCeiling is < -1.0 or > 1.0 || PlotFloor >= PlotCeiling)
        {
            yield return new ValidationResult("PlotCeiling must be finite, between -1.0 and 1.0, and greater than PlotFloor.", [nameof(PlotCeiling)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateDisplayThresholds()
    {
        foreach (var (value, name) in new[]
                 {
                     (ThemesDisplayThreshold, nameof(ThemesDisplayThreshold)),
                     (PlotDisplayThreshold, nameof(PlotDisplayThreshold)),
                     (GenreDisplayThreshold, nameof(GenreDisplayThreshold)),
                 })
        {
            if (!double.IsFinite(value) || value is < 0.0 or > 1.0)
            {
                yield return new ValidationResult($"{name} must be finite and between 0.0 and 1.0.", [name]);
            }
        }
    }

    private IEnumerable<ValidationResult> ValidateMinimumKeywordMovies()
    {
        if (MinimumKeywordMovies < 1)
        {
            yield return new ValidationResult("MinimumKeywordMovies must be at least 1.", [nameof(MinimumKeywordMovies)]);
        }
    }

    private IEnumerable<ValidationResult> ValidatePool()
    {
        if (CandidatePoolSize < Take)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} caps the requested {1} and must be greater than or equal to it; got {2} and {3}.",
                    nameof(CandidatePoolSize),
                    nameof(Take),
                    CandidatePoolSize,
                    Take),
                [nameof(CandidatePoolSize), nameof(Take)]);
        }
    }


    private IEnumerable<ValidationResult> ValidateBlendCoefficients()
    {
        foreach (var result in ValidateBlendCoefficient(BlendSelectedWeight, nameof(BlendSelectedWeight)))
        {
            yield return result;
        }

        foreach (var result in ValidateBlendCoefficient(AlreadyWatchedPenalty, nameof(AlreadyWatchedPenalty)))
        {
            yield return result;
        }
    }

    private static IEnumerable<ValidationResult> ValidateBlendCoefficient(double value, string memberName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be finite and greater than or equal to 0.0; got {1}.",
                    memberName,
                    value),
                [memberName]);
        }
    }

    private IEnumerable<ValidationResult> ValidateBlendSignal()
    {
        if (BlendSelectedWeight == 0.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} cannot be 0.0.",
                    nameof(BlendSelectedWeight)),
                [nameof(BlendSelectedWeight)]);
        }
    }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
