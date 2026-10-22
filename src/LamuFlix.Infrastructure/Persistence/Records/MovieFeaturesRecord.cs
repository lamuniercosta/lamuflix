using System;
using LamuFlix.Core.Domain;
using Pgvector;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class MovieFeaturesRecord
{
    public required MovieId MovieId { get; set; }

    public required Vector Vector { get; set; }

    public required DateTimeOffset ComputedAt { get; set; }
}
