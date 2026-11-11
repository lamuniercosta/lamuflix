using System;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleSidecarResult
{
    public SubtitleSidecarResult(byte[] content, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        Content = content;
        FileName = fileName;
    }

    public byte[] Content { get; }

    public string FileName { get; }
}
