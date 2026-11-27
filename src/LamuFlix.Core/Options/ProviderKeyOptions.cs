namespace LamuFlix.Core.Options;

public sealed record ProviderKeyOptions
{
    public const string SectionName = "ProviderKeys";

    public const string TestKeySet = "test";

    public const string ProdKeySet = "prod";

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string KeySet { get; init; } = TestKeySet;
}
