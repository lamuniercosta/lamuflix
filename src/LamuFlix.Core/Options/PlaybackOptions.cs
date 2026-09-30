namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CollectionNeverUpdated.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record PlaybackOptions
{
    public const string SectionName = "Playback";

#pragma warning disable CA1819 // Ticket shape is a configuration-bound array
    public PlayerConfig[] Players { get; init; } = [];
#pragma warning restore CA1819
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
