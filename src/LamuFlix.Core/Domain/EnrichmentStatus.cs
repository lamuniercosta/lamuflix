using Ardalis.SmartEnum;

namespace LamuFlix.Core.Domain;

public sealed class EnrichmentStatus : SmartEnum<EnrichmentStatus, int>
{
    public static readonly EnrichmentStatus Pending = new(nameof(Pending), 0);
    public static readonly EnrichmentStatus Enriched = new(nameof(Enriched), 1);
    public static readonly EnrichmentStatus NotFound = new(nameof(NotFound), 2);
    public static readonly EnrichmentStatus Failed = new(nameof(Failed), 3);

    private EnrichmentStatus(string name, int value)
        : base(name, value)
    {
    }
}
