using System.Collections.Generic;
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

    /// <summary>
    /// Historical absolute library roots (Windows drive/UNC or POSIX) recognized only for backfill
    /// of stored paths and for legacy queued thumbnail payloads. They never authorize new imports.
    /// </summary>
#pragma warning disable CA1819 // Configuration-bound array; the options binder requires a settable array
    public string[] LegacyRootPaths { get; init; } = [];
#pragma warning restore CA1819
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
