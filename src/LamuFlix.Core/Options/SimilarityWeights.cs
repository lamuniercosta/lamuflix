using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record SimilarityWeights
{
    [Range(0.0, double.MaxValue)]
    public double Director { get; init; } = 3.0;

    [Range(0.0, double.MaxValue)]
    public double Themes { get; init; } = 3.0;

    [Range(0.0, double.MaxValue)]
    public double Crowd { get; init; } = 2.0;

    [Range(0.0, double.MaxValue)]
    public double Plot { get; init; } = 2.0;

    [Range(0.0, double.MaxValue)]
    public double Cast { get; init; } = 2.0;

    [Range(0.0, double.MaxValue)]
    public double Genres { get; init; } = 2.0;

    [Range(0.0, double.MaxValue)]
    public double Collection { get; init; } = 3.0;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
