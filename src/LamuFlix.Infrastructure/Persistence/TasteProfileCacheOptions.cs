using System;

namespace LamuFlix.Infrastructure.Persistence;

public sealed record TasteProfileCacheOptions
{
    public const string SectionName = "TasteProfileCache";

    public const double DefaultWarningFraction = 0.8;

    public TimeSpan Expiration { get; init; } = TimeSpan.FromMinutes(5);

    public double WarningFraction { get; init; } = DefaultWarningFraction;
}
