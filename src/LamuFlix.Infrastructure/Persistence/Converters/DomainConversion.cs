using System;

namespace LamuFlix.Infrastructure.Persistence.Converters;

internal static class DomainConversion
{
    public static TModel Require<TModel>(TModel? result)
        where TModel : class =>
        result ?? throw new InvalidOperationException($"Invalid {typeof(TModel).Name} provider value.");
}
