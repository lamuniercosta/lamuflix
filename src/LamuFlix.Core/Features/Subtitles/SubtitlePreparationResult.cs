using System;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitlePreparationResult
{
    private SubtitlePreparationResult(SubtitlePrepareOutcome outcome, string? selectedLanguage)
    {
        Outcome = outcome;
        SelectedLanguage = selectedLanguage;
    }

    public SubtitlePrepareOutcome Outcome { get; }

    public string? SelectedLanguage { get; }

    public static SubtitlePreparationResult Saved(string selectedLanguage)
    {
        if (string.IsNullOrWhiteSpace(selectedLanguage))
        {
            throw new ArgumentException(
                "A saved subtitle must carry the matched preferred language code.",
                nameof(selectedLanguage));
        }

        return new SubtitlePreparationResult(SubtitlePrepareOutcome.Saved, selectedLanguage);
    }

    public static SubtitlePreparationResult AlreadyPresent() =>
        new(SubtitlePrepareOutcome.AlreadyPresent, null);

    public static SubtitlePreparationResult NoMatch() => new(SubtitlePrepareOutcome.NoMatch, null);
}
