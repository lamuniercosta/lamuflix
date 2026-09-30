using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class MediaFormatConverter() : ValueConverter<MediaFormat, string>(
    format => format.Extension,
    value => FromString(value))
{
    private static MediaFormat FromString(string value)
    {
        MediaFormat.TryCreate(value, out var result);
        return DomainConversion.Require(result);
    }
}
