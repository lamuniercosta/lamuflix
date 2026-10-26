using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Enrichment;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration under the "OutboxRelay" section by AddLamuFlixOutboxRelay.
public sealed record OutboxRelayOptions : IValidatableObject
{
    public const string SectionName = "OutboxRelay";

    public int BatchSize { get; init; } = 100;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan PublishTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan FailureBackoffMin { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan FailureBackoffMax { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan ProcessedRetention { get; init; } = TimeSpan.FromDays(7);

    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(1);

    public int CleanupBatchSize { get; init; } = 1000;

    public TimeSpan BacklogAgeWarning { get; init; } = TimeSpan.FromMinutes(5);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (BatchSize is < 1 or > 1000)
        {
            yield return Range(nameof(BatchSize), BatchSize, 1, 1000);
        }

        if (CleanupBatchSize is < 1 or > 10000)
        {
            yield return Range(nameof(CleanupBatchSize), CleanupBatchSize, 1, 10000);
        }

        if (PollInterval <= TimeSpan.Zero)
        {
            yield return Positive(nameof(PollInterval), PollInterval);
        }

        if (PublishTimeout <= TimeSpan.Zero)
        {
            yield return Positive(nameof(PublishTimeout), PublishTimeout);
        }

        if (FailureBackoffMin <= TimeSpan.Zero)
        {
            yield return Positive(nameof(FailureBackoffMin), FailureBackoffMin);
        }

        if (ProcessedRetention <= TimeSpan.Zero)
        {
            yield return Positive(nameof(ProcessedRetention), ProcessedRetention);
        }

        if (CleanupInterval <= TimeSpan.Zero)
        {
            yield return Positive(nameof(CleanupInterval), CleanupInterval);
        }

        if (BacklogAgeWarning <= TimeSpan.Zero)
        {
            yield return Positive(nameof(BacklogAgeWarning), BacklogAgeWarning);
        }

        if (FailureBackoffMax < FailureBackoffMin)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1}) must be at least {2} ({3}).",
                    nameof(FailureBackoffMax),
                    FailureBackoffMax,
                    nameof(FailureBackoffMin),
                    FailureBackoffMin),
                [nameof(FailureBackoffMax), nameof(FailureBackoffMin)]);
        }
    }

    private static ValidationResult Range(string memberName, int value, int minimum, int maximum) =>
        new(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} must be between {1} and {2}; got {3}.",
                memberName,
                minimum,
                maximum,
                value),
            [memberName]);

    private static ValidationResult Positive(string memberName, TimeSpan value) =>
        new(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} must be greater than zero; got {1}.",
                memberName,
                value),
            [memberName]);
}
// ReSharper restore UnusedAutoPropertyAccessor.Global

public sealed class OutboxRelayOptionsValidator : IValidateOptions<OutboxRelayOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxRelayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new string[results.Count];
        for (var index = 0; index < results.Count; index++)
        {
            failures[index] = string.Format(
                CultureInfo.InvariantCulture,
                "DataAnnotation validation failed for '{0}' members: '{1}' with the error: '{2}'.",
                nameof(OutboxRelayOptions),
                string.Join(",", results[index].MemberNames),
                results[index].ErrorMessage);
        }

        return ValidateOptionsResult.Fail(failures);
    }
}
