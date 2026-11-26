using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Infrastructure.Persistence;

namespace LamuFlix.Infrastructure.Adapters;

internal interface ICandidateEvidenceWriter
{
    Task<CandidateImdbEvidence> RecordAsync(
        int tmdbId,
        CandidateEvidencePayload payload,
        CancellationToken cancellationToken);
}

internal sealed class CandidateEvidenceWriter(
    ICandidateEvidenceStore store,
    TimeProvider timeProvider) : ICandidateEvidenceWriter
{
    public async Task<CandidateImdbEvidence> RecordAsync(
        int tmdbId,
        CandidateEvidencePayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var evidence = CandidateEvidenceParser.Parse(payload, timeProvider.GetUtcNow());
        if (evidence.Outcome is CandidateEvidenceOutcome.Unresolved)
        {
            return evidence;
        }

        await store.WriteAsync(tmdbId, evidence, cancellationToken);
        return evidence;
    }
}
