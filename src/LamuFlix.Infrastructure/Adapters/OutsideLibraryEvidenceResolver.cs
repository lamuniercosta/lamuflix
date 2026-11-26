using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Adapters;

public interface IOutsideLibraryEvidenceResolver
{
    ValueTask<CandidateImdbEvidence?> ResolveAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct);
}

internal sealed partial class OutsideLibraryEvidenceResolver(
    ICandidateEvidenceWriter writer,
    CandidateEvidenceFlightRegistry flights,
    ILogger<OutsideLibraryEvidenceResolver> logger,
    TmdbMetadataProvider? tmdb = null,
    OmdbMetadataProvider? omdb = null) : IOutsideLibraryEvidenceResolver
{
    public ValueTask<CandidateImdbEvidence?> ResolveAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct) =>
        flights.RunAsync(
            tmdbId,
            flightToken => ResolveFlightAsync(tmdbId, cached, budget, flightToken),
            ct);

    private async ValueTask<CandidateImdbEvidence?> ResolveFlightAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (omdb is null)
        {
            return null;
        }

        if (cached?.ImdbId is { } inputId)
        {
            return ImdbId.TryCreate(inputId, out var knownId)
                ? await LookupAsync(tmdbId, cached, knownId, budget, ct)
                : null;
        }

        if (tmdb is null || !budget.CanCallExternalIds)
        {
            return null;
        }

        budget.CountExternalId();
        var externalId = await tmdb.GetExternalIdsAsync(tmdbId, ct);
        return externalId is { } found
            ? await LookupAsync(tmdbId, cached, found, budget, ct)
            : null;
    }

    private async ValueTask<CandidateImdbEvidence?> LookupAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        ImdbId imdbId,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct)
    {
        if (omdb is null || !budget.TryStartSend())
        {
            return null;
        }

        var result = await omdb.GetCandidateEvidenceAsync(imdbId.Value, ct);
        Log.Lookup(
            logger,
            tmdbId,
            budget.UsedSends,
            budget.Ceiling,
            budget.ExternalIdCalls,
            budget.UsageDate,
            result.Status);

        if (result.Status is CandidateLookupStatus.QuotaDenied)
        {
            budget.Deny();
            Log.Deferred(logger, tmdbId, budget.UsedSends, budget.Ceiling, budget.UsageDate);
            return null;
        }

        if (result.Evidence is not { } payload)
        {
            return null;
        }

        if (cached?.CollectionId is { } collectionId)
        {
            payload = payload with { CollectionId = collectionId };
        }

        return await writer.RecordAsync(tmdbId, payload, ct);
    }
}
