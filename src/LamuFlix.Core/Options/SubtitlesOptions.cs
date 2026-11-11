using System;
using System.Collections.Generic;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record SubtitlesOptions
{
    public const string SectionName = "Subtitles";

    public bool SaveEnabled { get; init; }

#pragma warning disable CA1819 // Configuration-bound array; the options binder requires a settable array
    public string[] PreferredLanguages { get; init; } = [];
#pragma warning restore CA1819
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
