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

    public SimilarityWeights Weights { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
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
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
