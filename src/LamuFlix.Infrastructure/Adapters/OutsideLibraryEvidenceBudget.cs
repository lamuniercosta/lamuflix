using System;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.Infrastructure.Adapters;

public sealed class OutsideLibraryEvidenceBudget(int ceiling, DateOnly usageDate)
{
    public int Ceiling { get; } = Math.Clamp(
        ceiling,
        0,
        OutsideLibraryCollectionSelection.MaxAdmitted);

    public DateOnly UsageDate { get; } = usageDate;

    public int UsedSends { get; private set; }

    public int ExternalIdCalls { get; private set; }

    public bool CacheOnly { get; private set; }

    public bool CanCallExternalIds =>
        !CacheOnly && ExternalIdCalls < Ceiling && UsedSends < Ceiling;

    public bool TryStartSend()
    {
        if (CacheOnly || UsedSends >= Ceiling)
        {
            return false;
        }

        UsedSends++;
        return true;
    }

    public void CountExternalId() => ExternalIdCalls++;

    public void Deny() => CacheOnly = true;
}
