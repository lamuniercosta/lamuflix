using System;

namespace LamuFlix.Infrastructure.Persistence;

public sealed record TasteProfileCacheOptions
{
    public const string SectionName = "TasteProfileCache";

    public TimeSpan Expiration { get; init; } = TimeSpan.FromMinutes(5);
}
