using System;
using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Converters;

public sealed class ReleaseYearConverter : ValueConverter<ReleaseYear, int>
{
    public ReleaseYearConverter(TimeProvider time)
        : base(year => year.Value, value => FromInt(value, time))
    {
        ArgumentNullException.ThrowIfNull(time);
    }

    private static ReleaseYear FromInt(int value, TimeProvider time)
    {
        ReleaseYear.TryCreate(value, time.GetUtcNow(), out var result);
        return DomainConversion.Require(result);
    }
}
