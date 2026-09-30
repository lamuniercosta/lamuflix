using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class MovieIdConverter() : ValueConverter<MovieId, int>(
    id => id.Value,
    value => FromInt(value))
{
    private static MovieId FromInt(int value)
    {
        MovieId.TryCreate(value, out var result);
        return DomainConversion.Require(result);
    }
}
