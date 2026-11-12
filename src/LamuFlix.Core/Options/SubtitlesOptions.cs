namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global // Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record SubtitlesOptions
{
    public const string SectionName = "Subtitles";

    public const int DefaultCandidateStoreMaxEntries = 1000;

    public const long DefaultMaxSubtitleDownloadBytes = 2 * 1024 * 1024;

    public const int DefaultMaxSubtitlePublicationAttempts = 10_000;

    public static readonly System.TimeSpan DefaultCandidateTokenTtl = System.TimeSpan.FromMinutes(5);

    public bool SaveEnabled { get; init; }

#pragma warning disable CA1819 // Configuration-bound array; the options binder requires a settable array
    public string[] PreferredLanguages { get; init; } = [];
#pragma warning restore CA1819

    public System.TimeSpan CandidateTokenTtl { get; init; } = DefaultCandidateTokenTtl;

    public int CandidateStoreMaxEntries { get; init; } = DefaultCandidateStoreMaxEntries;

    public long MaxSubtitleDownloadBytes { get; init; } = DefaultMaxSubtitleDownloadBytes;

    public int MaxSubtitlePublicationAttempts { get; init; } = DefaultMaxSubtitlePublicationAttempts;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
