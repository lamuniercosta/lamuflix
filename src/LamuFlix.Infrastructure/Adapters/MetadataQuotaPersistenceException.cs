using System;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class MetadataQuotaPersistenceException : Exception
{
    public MetadataQuotaPersistenceException(Exception innerException)
        : base("The OMDb request quota could not be recorded.", innerException)
    {
    }
}
