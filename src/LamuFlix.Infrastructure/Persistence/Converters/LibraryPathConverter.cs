using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class LibraryPathConverter() : ValueConverter<LibraryPath, string>(
    path => path.Value,
    value => FromString(value))
{
    private static LibraryPath FromString(string value)
    {
        LibraryPath.TryCreate(value, out var result);
        return DomainConversion.Require(result);
    }
}
