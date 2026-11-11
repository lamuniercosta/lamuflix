using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class SubtitlesOptionsValidator : IValidateOptions<SubtitlesOptions>
{
    public ValidateOptionsResult Validate(string? name, SubtitlesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.SaveEnabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (options.PreferredLanguages.Length == 0)
        {
            failures.Add(
                $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)} must list at least one "
                + "language code in priority order when saving subtitles is enabled.");
        }

        for (var index = 0; index < options.PreferredLanguages.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(options.PreferredLanguages[index]))
            {
                failures.Add(
                    $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}[{index}] must not be blank.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
