namespace LamuFlix.Core.Options;

public sealed record OmdbQuotaOptions
{
    public const string SectionName = "Omdb:Quota";

    // ReSharper disable once UnusedAutoPropertyAccessor.Global - bound from the raw configuration key.
    public int DailyLimit { get; set; }

    public bool IsEnabled => DailyLimit > 0;
}
