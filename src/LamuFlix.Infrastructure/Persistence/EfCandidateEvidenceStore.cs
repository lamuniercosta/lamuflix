using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public interface ICandidateEvidenceStore
{
    Task<CandidateImdbEvidence?> ReadAsync(int tmdbId, CancellationToken cancellationToken);

    Task WriteAsync(int tmdbId, CandidateImdbEvidence evidence, CancellationToken cancellationToken);
}

internal sealed class EfCandidateEvidenceStore(
    LamuFlixDbContext db,
    TimeProvider timeProvider) : ICandidateEvidenceStore
{
    public async Task<CandidateImdbEvidence?> ReadAsync(int tmdbId, CancellationToken cancellationToken)
    {
        var record = await db.CandidateEvidences
            .AsNoTracking()
            .SingleOrDefaultAsync(evidence => evidence.TmdbId == tmdbId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM outside_library_candidate_evidence WHERE tmdb_id = {tmdbId}",
                cancellationToken);
            return null;
        }

        if (!Enum.TryParse(record.Outcome, out CandidateEvidenceOutcome outcome))
        {
            return null;
        }

        return new CandidateImdbEvidence(
            record.ImdbId,
            record.CollectionId,
            record.Rating,
            record.Votes,
            outcome,
            record.ExpiresAtUtc);
    }

    public async Task WriteAsync(
        int tmdbId,
        CandidateImdbEvidence evidence,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Outcome is CandidateEvidenceOutcome.Unresolved || evidence.ExpiresAtUtc is not { } expiresAtUtc)
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO outside_library_candidate_evidence AS evidence
                (tmdb_id, imdb_id, collection_id, rating, votes, outcome, expires_at_utc)
            VALUES
                ({tmdbId}, {evidence.ImdbId}, {evidence.CollectionId}, {evidence.Rating}, {evidence.Votes},
                 {evidence.Outcome.ToString()}, {expiresAtUtc})
            ON CONFLICT (tmdb_id) DO UPDATE
            SET imdb_id = excluded.imdb_id,
                collection_id = excluded.collection_id,
                rating = excluded.rating,
                votes = excluded.votes,
                outcome = excluded.outcome,
                expires_at_utc = excluded.expires_at_utc
            """,
            cancellationToken);
    }
}
