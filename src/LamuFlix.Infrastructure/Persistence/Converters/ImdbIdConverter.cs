using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class ImdbIdConverter() : ValueConverter<ImdbId, string>(
    id => id.Value,
    value => FromString(value))
{
    private static ImdbId FromString(string value)
    {
        ImdbId.TryCreate(value, out var result);
        return DomainConversion.Require(result);
    }
}
