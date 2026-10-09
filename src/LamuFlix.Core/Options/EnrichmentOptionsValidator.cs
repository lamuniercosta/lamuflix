using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class EnrichmentOptionsValidator : IValidateOptions<EnrichmentOptions>
{
    public ValidateOptionsResult Validate(string? name, EnrichmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        if (options.ClaimLease <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(EnrichmentOptions.ClaimLease)} must be greater than zero.");
        }

        if (options.SweepInterval <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(EnrichmentOptions.SweepInterval)} must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}