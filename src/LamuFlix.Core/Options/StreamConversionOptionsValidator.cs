using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class StreamConversionOptionsValidator : IValidateOptions<StreamConversionOptions>
{
    public ValidateOptionsResult Validate(string? name, StreamConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (options.MaxConcurrentConversions <= 0)
        {
            failures.Add($"{StreamConversionOptions.SectionName}:{nameof(StreamConversionOptions.MaxConcurrentConversions)} must be greater than zero.");
        }

        if (options.IdleExpiry <= TimeSpan.Zero)
        {
            failures.Add($"{StreamConversionOptions.SectionName}:{nameof(StreamConversionOptions.IdleExpiry)} must be greater than zero.");
        }

        if (options.ScratchSizeLimitBytes <= 0)
        {
            failures.Add($"{StreamConversionOptions.SectionName}:{nameof(StreamConversionOptions.ScratchSizeLimitBytes)} must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.ScratchDirectory))
        {
            failures.Add($"{StreamConversionOptions.SectionName}:{nameof(StreamConversionOptions.ScratchDirectory)} must not be blank.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
