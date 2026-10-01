using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Enrichment;

public static class EnrichmentRetryPolicy
{
    public static EnrichmentFailureDecision? Decide(
        EnrichmentFailureCategory category,
        int attempt,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (!category.IsRetryable || attempt >= maxAttempts)
        {
            return null;
        }

        var action = ReferenceEquals(category, EnrichmentFailureCategory.RateLimited)
            ? EnrichmentFailureAction.RetryDelayed
            : EnrichmentFailureAction.Retry;
        return new EnrichmentFailureDecision(action, attempt + 1);
    }
}