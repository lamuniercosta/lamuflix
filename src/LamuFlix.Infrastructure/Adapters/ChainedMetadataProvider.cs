using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.Adapters;

public sealed class ChainedMetadataProvider : IMetadataProvider
{
    private readonly IMetadataProvider[] providers;
    private readonly IMetadataProvider? signalSupplement;

    public ChainedMetadataProvider(IReadOnlyList<IMetadataProvider> providers)
        : this(providers, signalSupplement: null)
    {
    }

    public ChainedMetadataProvider(IReadOnlyList<IMetadataProvider> providers, IMetadataProvider? signalSupplement)
    {
        ArgumentNullException.ThrowIfNull(providers);
        if (providers.Count == 0)
        {
            throw new ArgumentException("At least one metadata provider is required.", nameof(providers));
        }

        this.providers = [.. providers];
        this.signalSupplement = signalSupplement;
    }

    public async Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
    {
        MetadataLookupResult? rememberedFailure = null;
        foreach (var provider in providers)
        {
            var result = await provider.FindAsync(lookup, ct);
            if (result is MetadataLookupResult.Found found)
            {
                return await SupplementAsync(provider, found, lookup, ct);
            }

            var immediate = ResolveImmediate(result, ref rememberedFailure);
            if (immediate is not null)
            {
                return immediate;
            }
        }

        return rememberedFailure ?? new MetadataLookupResult.NotFound();
    }

    private async Task<MetadataLookupResult> SupplementAsync(
        IMetadataProvider source,
        MetadataLookupResult.Found found,
        MetadataLookup lookup,
        CancellationToken ct)
    {
        if (signalSupplement is null || ReferenceEquals(source, signalSupplement))
        {
            return found;
        }

        var supplemental = await signalSupplement.FindAsync(SupplementLookup(lookup, found.Metadata), ct);
        return supplemental is MetadataLookupResult.Found supplemented
            ? new MetadataLookupResult.Found(Merge(found.Metadata, supplemented.Metadata))
            : new MetadataLookupResult.Found(found.Metadata.WithSignals(new MovieSignalSnapshot()));
    }

    private static MetadataLookup SupplementLookup(MetadataLookup lookup, MovieMetadata primary)
    {
        if (lookup.ManualImdbId is not null || primary.ImdbId is not { } imdbId)
        {
            return lookup;
        }

        return new MetadataLookup(lookup.Title, lookup.ReleaseYear, imdbId);
    }

    private static MovieMetadata Merge(MovieMetadata primary, MovieMetadata supplement) =>
        primary
            .WithSignals(supplement.Signals)
            .WithCollection(supplement.CollectionId, supplement.CollectionName);

    private static MetadataLookupResult? ResolveImmediate(MetadataLookupResult result, ref MetadataLookupResult? rememberedFailure)
    {
        switch (result)
        {
            case MetadataLookupResult.Failed failed when IsEligible(failed.Category):
                rememberedFailure ??= failed;
                return null;
            case MetadataLookupResult.Failed:
                return result;
            default:
                return null;
        }
    }

    private static bool IsEligible(EnrichmentFailureCategory category) =>
        category == EnrichmentFailureCategory.RateLimited
        || category == EnrichmentFailureCategory.QuotaExhausted
        || category == EnrichmentFailureCategory.ProviderUnavailable;
}
