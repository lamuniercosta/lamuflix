namespace LamuFlix.Core.Ports;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Compared through record equality; never read directly.
public sealed record SubtitleSidecarWriteResult
{
    public static readonly SubtitleSidecarWriteResult Created = new("created");

    public static readonly SubtitleSidecarWriteResult AlreadyPresent = new("alreadyPresent");

    private SubtitleSidecarWriteResult(string code) => Code = code;

    public string Code { get; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
