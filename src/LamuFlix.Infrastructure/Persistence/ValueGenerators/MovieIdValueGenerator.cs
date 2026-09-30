using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace LamuFlix.Infrastructure.Persistence.ValueGenerators;

public sealed class MovieIdValueGenerator : ValueGenerator<MovieId?>
{
    private static readonly FieldInfo ValueField =
        typeof(MovieId).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("MovieId backing field was not found.");

    private static int _temporaryValue;

    public override bool GeneratesTemporaryValues => true;

    public override MovieId Next(EntityEntry entry)
    {
        var value = Interlocked.Decrement(ref _temporaryValue);
        var movieId = (MovieId)RuntimeHelpers.GetUninitializedObject(typeof(MovieId));
        ValueField.SetValue(movieId, value);
        return movieId;
    }
}
