namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitlePrepareOutcome
{
    private const string SavedCode = "saved";
    private const string AlreadyPresentCode = "alreadyPresent";
    private const string NoMatchCode = "noMatch";

    public static readonly SubtitlePrepareOutcome Saved = new(SavedCode);

    public static readonly SubtitlePrepareOutcome AlreadyPresent = new(AlreadyPresentCode);

    public static readonly SubtitlePrepareOutcome NoMatch = new(NoMatchCode);

    private SubtitlePrepareOutcome(string code) => Code = code;

    public string Code { get; }
}
