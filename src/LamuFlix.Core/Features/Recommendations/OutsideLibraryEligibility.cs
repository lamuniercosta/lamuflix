namespace LamuFlix.Core.Features.Recommendations;

public enum OutsideLibraryVerdict
{
    Admit,
    Reject,
    Defer,
}

public static class OutsideLibraryEligibility
{
    public static OutsideLibraryVerdict Evaluate(CandidateImdbEvidence? evidence)
    {
        if (evidence is { HasKnownFloorFailure: true })
        {
            return OutsideLibraryVerdict.Reject;
        }

        return evidence?.Outcome switch
        {
            CandidateEvidenceOutcome.Complete or CandidateEvidenceOutcome.ConfirmedAbsence
                => OutsideLibraryVerdict.Admit,
            _ => OutsideLibraryVerdict.Defer,
        };
    }
}
