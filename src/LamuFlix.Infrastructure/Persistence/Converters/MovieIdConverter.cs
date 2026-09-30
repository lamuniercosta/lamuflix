using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class MovieIdConverter() : ValueConverter<MovieId?, int?>(
    id => id == null ? null : id.Value,
    value => FromInt(value))
{
    private static MovieId? FromInt(int? value)
    {
        if (value is null)
        {
            return null;
        }

        MovieId.TryCreate(value.Value, out var result);
        return DomainConversion.Require(result);
    }
}
