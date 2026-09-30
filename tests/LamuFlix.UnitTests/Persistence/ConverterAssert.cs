using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.UnitTests.Persistence;

internal static class ConverterAssert
{
    public static void InvalidNonNullProviderThrows(ValueConverter converter, object providerValue) =>
        Should.Throw<InvalidOperationException>(() => converter.ConvertFromProvider(providerValue));

    public static void NullIsPreserved(ValueConverter converter)
    {
        converter.ConvertFromProvider(null).ShouldBeNull();
        converter.ConvertToProvider(null).ShouldBeNull();
    }
}
