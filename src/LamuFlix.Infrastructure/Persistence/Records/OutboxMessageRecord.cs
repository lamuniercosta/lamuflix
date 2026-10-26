using System;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class OutboxMessageRecord
{
    public long Id { get; set; }

#pragma warning disable CA1819 // Payload is the bytea column value
    public required byte[] Payload { get; set; }
#pragma warning restore CA1819

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
