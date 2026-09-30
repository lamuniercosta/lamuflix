using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class EnrichmentStatusConverter() : ValueConverter<EnrichmentStatus, int>(
    status => status.Value,
    value => FromInt(value))
{
    private static EnrichmentStatus FromInt(int value)
    {
        EnrichmentStatus.TryFromValue(value, out var result);
        return DomainConversion.Require(result);
    }
}
