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

    [Range(1, int.MaxValue)]
    public int Take { get; init; } = 10;

    [Range(1, int.MaxValue)]
    public int CandidatePoolSize { get; init; } = 50;

    [Range(0.0, 1.0)]
    public double MmrLambda { get; init; } = 0.7;

    public double RecencyDecayLambda { get; init; } = Math.Log(2.0) / 30.0;

    public double SimilarStoryThreshold { get; init; } = 0.5;

    public double BlendSelectedWeight { get; init; } = 1.0;

    public double BlendProfileWeight { get; init; } = 1.0;

    public double AlreadyWatchedPenalty { get; init; } = 0.25;

    public double NotInterestedPenalty { get; init; } = 1.0;

    public SimilarityWeights Weights { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ValidateDecay())
        {
            yield return result;
        }

        foreach (var result in ValidateWeights())
        {
            yield return result;
        }

        foreach (var result in ValidatePool())
        {
            yield return result;
        }

        foreach (var result in ValidateThreshold())
        {
            yield return result;
        }

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

    private IEnumerable<ValidationResult> ValidateThreshold()
    {
        if (!double.IsFinite(SimilarStoryThreshold) || SimilarStoryThreshold is < -1.0 or > 1.0)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} must be a finite value between -1 and 1; got {1}.",
                    nameof(SimilarStoryThreshold),
                    SimilarStoryThreshold),
                [nameof(SimilarStoryThreshold)]);
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
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
