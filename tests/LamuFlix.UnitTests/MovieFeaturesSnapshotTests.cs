using System;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests;

public sealed class MovieFeaturesSnapshotTests
{
    private static readonly MovieId Movie = new(7);
    private static readonly DateTimeOffset ComputedAt = new(2026, 10, 22, 10, 30, 0, TimeSpan.Zero);

    private static readonly float[] NonFinite = [float.NaN, float.PositiveInfinity, float.NegativeInfinity];

    public static TheoryData<float> NonFiniteValues => new() { NonFinite[0], NonFinite[1], NonFinite[2] };

    [Fact]
    public void Constructor_DefaultVector_Throws()
    {
        Should.Throw<ArgumentException>(
            () => new MovieFeaturesSnapshot(Movie, default, ComputedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(EmbeddingDimensions.Value - 1)]
    [InlineData(EmbeddingDimensions.Value + 1)]
    public void Constructor_VectorWithWrongLength_Throws(int length)
    {
        var vector = Vector(length, 0f);

        Should.Throw<ArgumentException>(
            () => new MovieFeaturesSnapshot(Movie, vector, ComputedAt));
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void Constructor_NonFiniteElement_Throws(float value)
    {
        var vector = Vector(EmbeddingDimensions.Value, 0f).SetItem(17, value);

        Should.Throw<ArgumentException>(
            () => new MovieFeaturesSnapshot(Movie, vector, ComputedAt));
    }

    [Fact]
    public void Constructor_AllZeroFiniteVector_IsAccepted()
    {
        var vector = Vector(EmbeddingDimensions.Value, 0f);

        var snapshot = new MovieFeaturesSnapshot(Movie, vector, ComputedAt);

        snapshot.MovieId.ShouldBe(Movie);
        snapshot.Vector.ShouldBe(vector);
        snapshot.ComputedAt.ShouldBe(ComputedAt);
    }

    [Fact]
    public void Constructor_FiniteVector_IsAccepted()
    {
        var vector = Vector(EmbeddingDimensions.Value, 0.25f);

        var snapshot = new MovieFeaturesSnapshot(Movie, vector, ComputedAt);

        snapshot.Vector.ShouldBe(vector);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Constructor_AcceptsExactlyTheFinite384ElementVectors(float[] values)
    {
        var vector = Normalize(values);
        var allFinite = vector.All(float.IsFinite);

        if (!allFinite)
        {
            Should.Throw<ArgumentException>(
                () => new MovieFeaturesSnapshot(Movie, vector, ComputedAt));
            return true;
        }

        var snapshot = new MovieFeaturesSnapshot(Movie, vector, ComputedAt);

        return snapshot.Vector.SequenceEqual(vector);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Constructor_RejectsAnyNonFiniteElement(int seed, int position)
    {
        var index = Math.Abs(position % EmbeddingDimensions.Value);
        var nonFinite = NonFinite[(int)(Math.Abs((long)seed) % NonFinite.Length)];
        var vector = Vector(EmbeddingDimensions.Value, 0.5f).SetItem(index, nonFinite);

        Should.Throw<ArgumentException>(
            () => new MovieFeaturesSnapshot(Movie, vector, ComputedAt));

        return true;
    }

    private static ImmutableArray<float> Vector(int length, float value) =>
        ImmutableArray.CreateRange(Enumerable.Repeat(value, length));

    private static ImmutableArray<float> Normalize(float[] values) =>
        ImmutableArray.CreateRange(
            Enumerable.Range(0, EmbeddingDimensions.Value)
                .Select(index => values.Length == 0 ? 0f : values[index % values.Length]));
}
