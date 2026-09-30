using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class RuntimeConverter() : ValueConverter<Runtime, int>(
    runtime => runtime.Minutes,
    value => FromInt(value))
{
    private static Runtime FromInt(int value)
    {
        Runtime.TryCreate(value, out var result);
        return DomainConversion.Require(result);
    }
}
