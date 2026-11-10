using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record LibraryOptions
{
    public const string SectionName = "Library";

    [Required]
    public string RootPath { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxConcurrency { get; init; } = 4;

    [Range(1, int.MaxValue)]
    public int MaxSubtitleInputBytes { get; init; } = 8388608;

    [Range(1, int.MaxValue)]
    public int MaxSubtitleCues { get; init; } = 100000;

    [Range(1, int.MaxValue)]
    public int MaxSubtitleLineCharacters { get; init; } = 16384;

    [Range(1, int.MaxValue)]
    public int MaxSubtitleOutputBytes { get; init; } = 33554432;

#pragma warning disable CA1819 // Configuration-bound array; the options binder requires a settable array
    public string[] LegacyRootPaths { get; init; } = [];
#pragma warning restore CA1819
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
