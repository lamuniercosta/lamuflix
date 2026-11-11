using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleAcquisitionSettings(IReadOnlyList<string> PreferredLanguages)
{
    public static SubtitleAcquisitionSettings Require(
        IOptions<SubtitlesOptions> subtitlesOptions,
        IOptions<SubtitleProviderOptions> providerOptions)
    {
        ArgumentNullException.ThrowIfNull(subtitlesOptions);
        ArgumentNullException.ThrowIfNull(providerOptions);

        var subtitles = subtitlesOptions.Value;
        if (!subtitles.SaveEnabled)
        {
            throw new SubtitleAcquisitionUnavailableException(
                "Subtitle saving is disabled for this library; enable Subtitles:SaveEnabled to acquire subtitles.");
        }

        if (subtitles.PreferredLanguages.Length == 0)
        {
            throw new SubtitleAcquisitionUnavailableException(
                "Subtitles:PreferredLanguages must list at least one language code in priority order.");
        }

        try
        {
            _ = providerOptions.Value;
        }
        catch (OptionsValidationException exception)
        {
            throw new SubtitleAcquisitionUnavailableException(
                "The subtitle provider acquisition settings are missing or invalid.", exception);
        }

        return new SubtitleAcquisitionSettings(
            [.. subtitles.PreferredLanguages
                .Where(language => !string.IsNullOrWhiteSpace(language))
                .Select(language => language.Trim())]);
    }
}
