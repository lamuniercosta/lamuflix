using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record SimilarityWeights
{
    [Range(0.0, double.MaxValue)]
    public double Director { get; init; } = 3.0;

    [Range(0.0, double.MaxValue)]
    public double Writers { get; init; } = 2.0;

    [Range(0.0, double.MaxValue)]
    public double Cast { get; init; } = 2.0;

    [Range(0.0, double.MaxValue)]
    public double Genres { get; init; } = 3.0;

    [Range(0.0, double.MaxValue)]
    public double YearProximity { get; init; } = 1.0;

    [Range(0.0, double.MaxValue)]
    public double RuntimeProximity { get; init; } = 0.5;

    [Range(0.0, double.MaxValue)]
    public double RatingBoost { get; init; } = 0.5;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
