using System;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class MetadataQuotaExhaustedException : Exception
{
    public MetadataQuotaExhaustedException()
        : base("The OMDb daily request quota is exhausted.")
    {
    }
}
