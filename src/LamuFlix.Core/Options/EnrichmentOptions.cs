using System;
using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record EnrichmentOptions
{
    public const string SectionName = "Enrichment";

    [Range(1, int.MaxValue)]
    public int MaxAttempts { get; init; } = 3;

    public TimeSpan SweepInterval { get; init; }

    public TimeSpan ClaimLease { get; init; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
