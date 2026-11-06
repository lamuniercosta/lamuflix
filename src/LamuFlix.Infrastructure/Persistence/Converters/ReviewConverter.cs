using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class ReviewConverter() : ValueConverter<Review, int>(
    review => review.Value,
    value => FromInt(value))
{
    private static Review FromInt(int value)
    {
        Review.TryFromValue(value, out var result);
        return DomainConversion.Require(result);
    }
}
