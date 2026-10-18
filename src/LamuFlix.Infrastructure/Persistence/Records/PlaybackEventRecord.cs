using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class PlaybackEventRecord
{
    public int Id { get; set; }

    public required MovieId MovieId { get; set; }

    public required DateTimeOffset PlayedAt { get; set; }
}
