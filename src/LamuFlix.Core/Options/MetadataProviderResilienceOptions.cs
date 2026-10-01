using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record MetadataProviderResilienceOptions : IValidatableObject
{
    public const string SectionName = "Omdb:Resilience";

    [Range(1, int.MaxValue)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00")]
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);

    [Range(typeof(TimeSpan), "00:00:00.010", "1.00:00:00")]
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(10);

    [Range(typeof(TimeSpan), "00:00:00.010", "1.00:00:00")]
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromSeconds(30);

    [Range(0.0, 1.0)]
    public double FailureRatio { get; init; } = 0.1;

    [Range(typeof(TimeSpan), "00:00:00.500", "1.00:00:00")]
    public TimeSpan SamplingDuration { get; init; } = TimeSpan.FromSeconds(30);

    [Range(2, int.MaxValue)]
    public int MinimumThroughput { get; init; } = 100;

    [Range(typeof(TimeSpan), "00:00:00.500", "1.00:00:00")]
    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(5);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AttemptTimeout > TotalTimeout)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "AttemptTimeout must be less than or equal to TotalTimeout; got {0} and {1}.",
                    AttemptTimeout,
                    TotalTimeout),
                [nameof(AttemptTimeout), nameof(TotalTimeout)]);
        }

        if (SamplingDuration < TimeSpan.FromTicks(AttemptTimeout.Ticks * 2))
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "SamplingDuration must be at least twice AttemptTimeout; got {0} and {1}.",
                    SamplingDuration,
                    AttemptTimeout),
                [nameof(SamplingDuration), nameof(AttemptTimeout)]);
        }
    }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global