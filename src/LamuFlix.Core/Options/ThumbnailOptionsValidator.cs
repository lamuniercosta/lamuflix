using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class ThumbnailOptionsValidator : IValidateOptions<ThumbnailOptions>
{
    public ValidateOptionsResult Validate(string? name, ThumbnailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        AddPathFailures(options, failures);
        AddRangeFailures(options, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddPathFailures(ThumbnailOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.FfmpegPath))
        {
            failures.Add($"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.FfmpegPath)} must not be blank.");
        }

        if (string.IsNullOrWhiteSpace(options.CacheDirectory))
        {
            failures.Add($"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.CacheDirectory)} must not be blank.");
        }
    }

    private static void AddRangeFailures(ThumbnailOptions options, List<string> failures)
    {
        if (options.MaxWidth <= 0)
        {
            failures.Add($"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.MaxWidth)} must be greater than zero.");
        }

        if (options.ProcessTimeoutSeconds <= 0)
        {
            failures.Add($"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.ProcessTimeoutSeconds)} must be greater than zero.");
        }
    }
}
