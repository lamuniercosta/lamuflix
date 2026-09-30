namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record FeatureOptions
{
    public const string SectionName = "Features";

    public bool LocalPlay { get; init; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
