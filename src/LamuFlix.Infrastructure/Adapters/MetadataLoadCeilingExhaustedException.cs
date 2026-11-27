using System;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class MetadataLoadCeilingExhaustedException : Exception
{
    public MetadataLoadCeilingExhaustedException()
        : base("The per-load outside-library discovery ceiling is exhausted.")
    {
    }
}
