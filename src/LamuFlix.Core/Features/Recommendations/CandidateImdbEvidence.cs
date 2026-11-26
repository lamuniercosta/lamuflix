using System;

namespace LamuFlix.Core.Features.Recommendations;

public enum CandidateEvidenceOutcome
{
    Complete,
    ConfirmedAbsence,
    Unresolved,
}

public sealed record CandidateImdbEvidence(
    string? ImdbId,
    int? CollectionId,
    decimal? Rating,
    int? Votes,
    CandidateEvidenceOutcome Outcome,
    DateTimeOffset? ExpiresAtUtc = null)
{
    public const decimal MinimumRating = 6.5m;

    public const int MinimumVotes = 1000;

    public bool HasKnownFloorFailure =>
        Rating is < MinimumRating || Votes is < MinimumVotes;
}
