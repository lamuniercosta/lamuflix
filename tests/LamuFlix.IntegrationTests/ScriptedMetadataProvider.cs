using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.IntegrationTests;

public sealed class ScriptedMetadataProvider : IMetadataProvider
{
    private readonly Func<MetadataLookup, MetadataLookupResult> script;

    public ScriptedMetadataProvider(Func<MetadataLookup, MetadataLookupResult> script)
    {
        this.script = script;
    }

    public int Calls { get; private set; }

    public MetadataLookup? LastLookup { get; private set; }

    public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
    {
        Calls++;
        LastLookup = lookup;
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(script(lookup));
    }

    public static ScriptedMetadataProvider Returning(MetadataLookupResult result) =>
        new(_ => result);

    public static ScriptedMetadataProvider Failing(EnrichmentFailureCategory category) =>
        new(_ => new MetadataLookupResult.Failed(category));
}