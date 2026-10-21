using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed class EmbeddingGenerationException : Exception
{
    public EmbeddingGenerationException(EnrichmentFailureCategory category)
        : this(category, DefaultMessage(category))
    {
    }

    public EmbeddingGenerationException(EnrichmentFailureCategory category, string message)
        : base(message)
    {
        Category = EnsureAllowed(category);
    }

    public EmbeddingGenerationException(EnrichmentFailureCategory category, string message, Exception innerException)
        : base(message, innerException)
    {
        Category = EnsureAllowed(category);
    }

    public EnrichmentFailureCategory Category { get; }

    private static string DefaultMessage(EnrichmentFailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return $"The embedding generator failed ({category.Code}).";
    }

    private static EnrichmentFailureCategory EnsureAllowed(EnrichmentFailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (!ReferenceEquals(category, EnrichmentFailureCategory.ProviderUnavailable)
            && !ReferenceEquals(category, EnrichmentFailureCategory.RateLimited)
            && !ReferenceEquals(category, EnrichmentFailureCategory.InvalidResponse))
        {
            throw new ArgumentException(
                "Only ProviderUnavailable, RateLimited and InvalidResponse categories are allowed.",
                nameof(category));
        }

        return category;
    }
}
