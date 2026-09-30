using System;
using System.Diagnostics.CodeAnalysis;
using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class EnrichmentFailureCategoryConverter() : ValueConverter<EnrichmentFailureCategory, string>(
    category => category.Code,
    value => FromCode(value))
{
    private static EnrichmentFailureCategory FromCode(string code) =>
        DomainConversion.Require(TryFromKnownCode(code, out var result) ? result : null);

    private static readonly EnrichmentFailureCategory[] Known =
    [
        EnrichmentFailureCategory.ProviderUnavailable,
        EnrichmentFailureCategory.RateLimited,
        EnrichmentFailureCategory.InvalidResponse,
        EnrichmentFailureCategory.Unknown,
    ];

    private static bool TryFromKnownCode(string code, [NotNullWhen(true)] out EnrichmentFailureCategory? result)
    {
        foreach (var category in Known)
        {
            if (string.Equals(category.Code, code, StringComparison.Ordinal))
            {
                result = category;
                return true;
            }
        }

        result = null;
        return false;
    }
}
