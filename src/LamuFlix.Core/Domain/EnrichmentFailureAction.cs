using Ardalis.SmartEnum;

namespace LamuFlix.Core.Domain;

public sealed class EnrichmentFailureAction : SmartEnum<EnrichmentFailureAction, int>
{
    public static readonly EnrichmentFailureAction Retry = new(nameof(Retry), 0);
    public static readonly EnrichmentFailureAction RetryDelayed = new(nameof(RetryDelayed), 1);
    public static readonly EnrichmentFailureAction DeadLetter = new(nameof(DeadLetter), 2);

    private EnrichmentFailureAction(string name, int value)
        : base(name, value)
    {
    }
}
