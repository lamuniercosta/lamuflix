using System;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class DailyRequestUsageRecord
{
    public required DateOnly UsageDate { get; set; }

    public required int RequestCount { get; set; }
}
