using System;
using System.Collections.Generic;
using LamuFlix.Core.Features.Subtitles;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class SubtitlesOptionsValidator : IValidateOptions<SubtitlesOptions>
{
    public ValidateOptionsResult Validate(string? name, SubtitlesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        AddPreferenceFailures(options, failures);
        AddCandidateTokenTtlFailures(options, failures);
        AddCandidateStoreMaxEntriesFailures(options, failures);
        AddDownloadBytesFailures(options, failures);
        AddPublicationAttemptFailures(options, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddPreferenceFailures(SubtitlesOptions options, List<string> failures)
    {
        for (var index = 0; index < options.PreferredLanguages.Length; index++)
        {
            var preference = options.PreferredLanguages[index];
            var field = $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}[{index}]";
            if (string.IsNullOrWhiteSpace(preference))
            {
                failures.Add($"{field} must not be blank.");
                continue;
            }

            if (!SupportedSubtitleLanguages.TryResolve(preference, out _))
            {
                failures.Add($"{field} must be a supported subtitle language code.");
            }
        }
    }

    private static void AddCandidateTokenTtlFailures(SubtitlesOptions options, List<string> failures)
    {
        if (options.CandidateTokenTtl <= TimeSpan.Zero)
        {
            failures.Add(
                $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.CandidateTokenTtl)} must be greater than zero.");
        }
    }

    private static void AddCandidateStoreMaxEntriesFailures(SubtitlesOptions options, List<string> failures)
    {
        if (options.CandidateStoreMaxEntries < 1)
        {
            failures.Add(
                $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.CandidateStoreMaxEntries)} must be at least 1.");
        }
    }

    private static void AddDownloadBytesFailures(SubtitlesOptions options, List<string> failures)
    {
        if (options.MaxSubtitleDownloadBytes <= 0)
        {
            failures.Add(
                $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.MaxSubtitleDownloadBytes)} must be greater than zero.");
        }
    }

    private static void AddPublicationAttemptFailures(SubtitlesOptions options, List<string> failures)
    {
        if (options.MaxSubtitlePublicationAttempts <= 0)
        {
            failures.Add(
                $"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.MaxSubtitlePublicationAttempts)} must be greater than zero.");
        }
    }
}
