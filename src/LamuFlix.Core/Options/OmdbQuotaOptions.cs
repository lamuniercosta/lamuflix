using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

public sealed record OmdbQuotaOptions
{
    public const string SectionName = "Omdb:Quota";

    [Range(1, int.MaxValue)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global - bound from configuration
    public int DailyLimit { get; init; } = 1000;
}
