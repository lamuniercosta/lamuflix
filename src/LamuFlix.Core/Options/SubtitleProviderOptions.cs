using System;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global // Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record SubtitleProviderOptions
{
    public const string SectionName = "Subtitles:OpenSubtitles";

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public string UserAgent { get; init; } = string.Empty;

#pragma warning disable CA1819 // Configuration-bound array; the options binder requires a settable array
    public string[] DownloadAllowedHosts { get; init; } = [];
#pragma warning restore CA1819

    public long MaxDownloadBytes { get; init; }

    public TimeSpan RequestTimeout { get; init; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
