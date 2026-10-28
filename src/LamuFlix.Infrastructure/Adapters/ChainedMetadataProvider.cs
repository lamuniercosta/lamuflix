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

    public ChainedMetadataProvider(IReadOnlyList<IMetadataProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        if (providers.Count == 0)
        {
            throw new ArgumentException("At least one metadata provider is required.", nameof(providers));
        }

        this.providers = [.. providers];
    }

    public async Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
    {
        MetadataLookupResult? rememberedFailure = null;
        foreach (var provider in providers)
        {
            var result = await provider.FindAsync(lookup, ct);
            switch (result)
            {
                case MetadataLookupResult.Found:
                    return result;
                case MetadataLookupResult.Failed failed when IsEligible(failed.Category):
                    rememberedFailure ??= failed;
                    break;
                case MetadataLookupResult.Failed:
                    return result;
                case MetadataLookupResult.NotFound:
                    break;
            }
        }

        return rememberedFailure ?? new MetadataLookupResult.NotFound();
    }

    private static bool IsEligible(EnrichmentFailureCategory category) =>
        category == EnrichmentFailureCategory.RateLimited
        || category == EnrichmentFailureCategory.ProviderUnavailable;
}
