namespace LamuFlix.Core.Ports;

// ReSharper disable UnusedAutoPropertyAccessor.Global // Compared through record equality; never read directly.
public sealed record SubtitleSidecarWriteResult
{
    private const string CreatedCode = "created";

    private const string AlreadyPresentCode = "alreadyPresent";

    public static readonly SubtitleSidecarWriteResult Created = new(CreatedCode);

    public static readonly SubtitleSidecarWriteResult AlreadyPresent = new(AlreadyPresentCode);

    private SubtitleSidecarWriteResult(string code) => Code = code;

    public string Code { get; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
