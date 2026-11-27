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

        var provider = tmdb;
        if (provider is null || !budget.CanCallExternalIds)
        {
            return null;
        }

        budget.CountExternalId();
        return await ResolveDiscoveryAsync(tmdbId, cached, provider, budget, ct);
    }

    private async ValueTask<CandidateImdbEvidence?> ResolveDiscoveryAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        TmdbMetadataProvider provider,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct)
    {
        var externalId = await provider.GetDiscoveryExternalIdAsync(tmdbId, ct);
        if (externalId is { Status: DiscoveryExternalIdStatus.Found, ImdbId: { } found })
        {
            return await LookupAsync(tmdbId, cached, found, budget, ct);
        }

        if (externalId.Status is DiscoveryExternalIdStatus.ConfirmedAbsence)
        {
            var absence = await WithCollectionAsync(
                tmdbId,
                cached,
                new CandidateEvidencePayload(true, null, null, null),
                budget,
                ct);
            return await writer.RecordAsync(tmdbId, absence, ct);
        }

        return null;
    }

    private async ValueTask<CandidateImdbEvidence?> LookupAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        ImdbId imdbId,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct)
    {
        if (omdb is null || !budget.CanStartSend)
        {
            return null;
        }

        var result = await omdb.GetCandidateEvidenceAsync(imdbId.Value, ct, budget);
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

        payload = await WithCollectionAsync(tmdbId, cached, payload, budget, ct);
        return await writer.RecordAsync(tmdbId, payload, ct);
    }

    private async ValueTask<CandidateEvidencePayload> WithCollectionAsync(
        int tmdbId,
        CandidateImdbEvidence? cached,
        CandidateEvidencePayload payload,
        OutsideLibraryEvidenceBudget budget,
        CancellationToken ct)
    {
        if (cached?.CollectionId is { } collectionId)
        {
            return payload with { CollectionId = collectionId };
        }

        var provider = tmdb;
        if (provider is null || !budget.CanCallExternalIds)
        {
            return payload;
        }

        budget.CountExternalId();
        return await provider.GetCollectionIdAsync(tmdbId, ct) is { } live
            ? payload with { CollectionId = live }
            : payload;
    }
}
