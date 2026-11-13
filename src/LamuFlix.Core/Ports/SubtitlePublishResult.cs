namespace LamuFlix.Core.Ports;

public enum SubtitlePublishOutcome
{
    Published = 0,
    AttemptsExhausted = 1,
}

public sealed record SubtitlePublishResult(SubtitlePublishOutcome Outcome, string FileName)
{
    public static SubtitlePublishResult Published(string fileName) =>
        new(SubtitlePublishOutcome.Published, fileName);

    public static SubtitlePublishResult AttemptsExhausted() =>
        new(SubtitlePublishOutcome.AttemptsExhausted, string.Empty);
}
