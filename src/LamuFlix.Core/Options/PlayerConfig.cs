namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CollectionNeverUpdated.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record PlayerConfig
{
    public string Name { get; init; } = string.Empty;

    public string ExecutablePath { get; init; } = string.Empty;

#pragma warning disable CA1819 // Ticket shape is a configuration-bound array
    public string[] Formats { get; init; } = [];
#pragma warning restore CA1819
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
