using System;

namespace LamuFlix.Core.Options;

public sealed record StreamConversionOptions
{
    public const string SectionName = "StreamConversion";

    public int MaxConcurrentConversions { get; init; } = 2;

    public TimeSpan IdleExpiry { get; init; } = TimeSpan.FromMinutes(60);

    public long ScratchSizeLimitBytes { get; init; } = 20L * 1024 * 1024 * 1024;

    public string ScratchDirectory { get; init; } = "cache/stream-conversion";
}
