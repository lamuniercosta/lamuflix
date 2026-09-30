using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class ImdbRatingConverter() : ValueConverter<ImdbRating, decimal>(
    rating => rating.Value,
    value => FromDecimal(value))
{
    private static ImdbRating FromDecimal(decimal value)
    {
        ImdbRating.TryCreate(value, out var result);
        return DomainConversion.Require(result);
    }
}
