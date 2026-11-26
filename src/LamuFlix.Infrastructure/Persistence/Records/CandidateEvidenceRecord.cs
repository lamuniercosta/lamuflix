using System;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class CandidateEvidenceRecord
{
    public required int TmdbId { get; set; }

    public string? ImdbId { get; set; }

    public int? CollectionId { get; set; }

    public decimal? Rating { get; set; }

    public int? Votes { get; set; }

    public required string Outcome { get; set; }

    public required DateTimeOffset ExpiresAtUtc { get; set; }
}
