using System;
using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class EnrichmentFailureCategoryConverter() : ValueConverter<EnrichmentFailureCategory, string>(
    category => category.Code,
    value => FromCode(value))
{
    private static EnrichmentFailureCategory FromCode(string code) =>
        DomainConversion.Require(FindKnownCode(code));

    private static readonly EnrichmentFailureCategory[] Known =
    [
        EnrichmentFailureCategory.ProviderUnavailable,
        EnrichmentFailureCategory.RateLimited,
        EnrichmentFailureCategory.InvalidResponse,
        EnrichmentFailureCategory.Unknown,
    ];

    private static EnrichmentFailureCategory? FindKnownCode(string code)
    {
        foreach (var category in Known)
        {
            if (string.Equals(category.Code, code, StringComparison.Ordinal))
            {
                return category;
            }
        }

        return null;
    }
}
