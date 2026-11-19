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

    [Range(1, int.MaxValue)]
    public int Take { get; init; } = 10;

    [Range(1, int.MaxValue)]
    public int CandidatePoolSize { get; init; } = 50;

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

    public double BlendProfileWeight { get; init; } = 1.0;

    public double AlreadyWatchedPenalty { get; init; } = 0.25;

    public double NotInterestedPenalty { get; init; } = 1.0;

    public double LikedReviewWeight { get; init; } = 1.0;

    public double DislikedReviewWeight { get; init; } = 1.0;

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

        foreach (var result in ValidateReview())
        {
            yield return result;
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
        if (Weights is not null)
        {
            var total = Weights.Collection + Weights.Director + Weights.Themes + Weights.Plot + Weights.Cast + Weights.Genres;
            if (!double.IsFinite(total) || total <= 0.0)
            {
                yield return new ValidationResult(
                    "The sum of the six similarity weights must be finite and greater than 0.0.",
                    [nameof(Weights)]);
            }
        }

        if (!double.IsFinite(ThemesCeiling) || ThemesCeiling <= 0.0)
        {
            yield return new ValidationResult("ThemesCeiling must be finite and greater than 0.0.", [nameof(ThemesCeiling)]);
        }

        if (!double.IsFinite(PlotFloor) || PlotFloor is < -1.0 or > 1.0)
        {
            yield return new ValidationResult("PlotFloor must be finite and between -1.0 and 1.0.", [nameof(PlotFloor)]);
        }

        if (!double.IsFinite(PlotCeiling) || PlotCeiling is < -1.0 or > 1.0 || PlotFloor >= PlotCeiling)
        {
            yield return new ValidationResult("PlotCeiling must be finite, between -1.0 and 1.0, and greater than PlotFloor.", [nameof(PlotCeiling)]);
        }

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
                    "{0} must be greater than or equal to {1}; got {2} and {3}.",
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

        foreach (var result in ValidateBlendCoefficient(BlendProfileWeight, nameof(BlendProfileWeight)))
        {
            yield return result;
        }

        foreach (var result in ValidateBlendCoefficient(AlreadyWatchedPenalty, nameof(AlreadyWatchedPenalty)))
        {
            yield return result;
        }

        foreach (var result in ValidateBlendCoefficient(NotInterestedPenalty, nameof(NotInterestedPenalty)))
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
        if (BlendSelectedWeight == 0.0 && BlendProfileWeight == 0.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} and {1} cannot both be 0.0.",
                    nameof(BlendSelectedWeight),
                    nameof(BlendProfileWeight)),
                [nameof(BlendSelectedWeight), nameof(BlendProfileWeight)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateReview()
    {
        foreach (var result in ValidateReviewCoefficient(LikedReviewWeight, nameof(LikedReviewWeight)))
        {
            yield return result;
        }

        foreach (var result in ValidateReviewCoefficient(DislikedReviewWeight, nameof(DislikedReviewWeight)))
        {
            yield return result;
        }

        foreach (var result in ValidateReviewChannels())
        {
            yield return result;
        }
    }

    private static IEnumerable<ValidationResult> ValidateReviewCoefficient(double value, string memberName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be finite and greater than 0.0; got {1}.",
                    memberName,
                    value),
                [memberName]);
        }
    }

    private IEnumerable<ValidationResult> ValidateReviewChannels()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - configuration binder can set Weights to null via reflection.
        if (Weights is null)
        {
            yield break;
        }

        if (Weights.HasEnabledReviewChannel)
        {
            yield break;
        }

        yield return new ValidationResult(
            string.Format(
                CultureInfo.InvariantCulture,
                "At least one review channel must be enabled; {0}.{1} must be greater than 0.0.",
                nameof(Weights),
                nameof(SimilarityWeights.ReviewPlotWeight)),
            [nameof(Weights)]);
    }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
