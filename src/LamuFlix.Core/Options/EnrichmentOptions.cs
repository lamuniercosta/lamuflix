using System;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record EnrichmentOptions
{
    public const string SectionName = "Enrichment";

    public int MaxAttempts { get; init; } = 3;

    public TimeSpan SweepInterval { get; init; }

    public TimeSpan ClaimLease { get; init; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
