namespace LamuFlix.Infrastructure.Adapters;

public enum CandidateLookupStatus
{
    Evidence,
    QuotaDenied,
    ConfirmedAbsence,
    Unresolved,
    Invalid,
}

public sealed record CandidateEvidenceLookupResult(CandidateLookupStatus Status, CandidateEvidencePayload? Evidence = null)
{
    public static CandidateEvidenceLookupResult For(CandidateEvidencePayload payload) =>
        new(CandidateLookupStatus.Evidence, payload);

    public static CandidateEvidenceLookupResult QuotaDenied { get; } =
        new(CandidateLookupStatus.QuotaDenied);

    public static CandidateEvidenceLookupResult ConfirmedAbsence { get; } =
        new(
            CandidateLookupStatus.ConfirmedAbsence,
            new CandidateEvidencePayload(true, null, null, null));

    public static CandidateEvidenceLookupResult Unresolved { get; } =
        new(CandidateLookupStatus.Unresolved);

    public static CandidateEvidenceLookupResult Invalid { get; } =
        new(CandidateLookupStatus.Invalid);
}
