namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record ThumbnailOptions
{
    public const string SectionName = "Thumbnails";

    public bool Enabled { get; init; }

    public string FfmpegPath { get; init; } = "ffmpeg";

    public string CacheDirectory { get; init; } = "cache/thumbnails";

    public int MaxWidth { get; init; } = 320;

    public int ProcessTimeoutSeconds { get; init; } = 30;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
