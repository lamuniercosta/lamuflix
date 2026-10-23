using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class ProfileSimilarityCalculatorTests
{
    private const double Tolerance = 1e-9;

    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 100,
                ["Comedy"] = 100,
                ["Thriller"] = 100,
            }));

    [Fact]
    public void Similarity_BothChannelsComparable_ReturnsArithmeticMean()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double> { ["Drama"] = 1.0 }, Vector((0, 1.0f)));
        var candidate = Vector((0, 1.0f));

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(["Drama"], candidate, profile, Table);

        // assert
        similarity.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Similarity_WeightedGenreOverlap_UsesCandidateWeightForPresentGenresOnly()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double> { ["Drama"] = 0.5 }, ImmutableArray<float>.Empty);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ["Drama", "Comedy"],
            ImmutableArray<float>.Empty,
            profile,
            Table);

        // assert
        similarity.ShouldBe(0.25, Tolerance);
    }

    [Fact]
    public void Similarity_GenreNames_AreTrimmedAndCaseInsensitive()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double> { ["DRAMA"] = 1.0 }, ImmutableArray<float>.Empty);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ["  drama  "],
            ImmutableArray<float>.Empty,
            profile,
            Table);

        // assert
        similarity.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Similarity_GenreChannelOnly_WhenProfileHasNoPlot()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double> { ["Drama"] = 1.0 }, ImmutableArray<float>.Empty);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ["Drama"],
            Vector((0, 1.0f)),
            profile,
            Table);

        // assert
        similarity.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Similarity_PlotChannelOnly_WhenCandidateHasNoGenres()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), Vector((0, 1.0f)));

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            Vector((0, 1.0f)),
            profile,
            Table);

        // assert
        similarity.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Similarity_NoComparableChannels_ReturnsZero()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), ImmutableArray<float>.Empty);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            profile,
            Table);

        // assert
        similarity.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_ProfileWithoutGenres_DoesNotCompareGenreChannel()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), ImmutableArray<float>.Empty);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ["Drama"],
            ImmutableArray<float>.Empty,
            profile,
            Table);

        // assert
        similarity.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_NegativeCosine_IsClampedToZero()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), Vector((0, 1.0f)));

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            Vector((0, -1.0f)),
            profile,
            Table);

        // assert
        similarity.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_OrthogonalVectors_ReturnZeroCosine()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), Vector((0, 1.0f)));

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            Vector((1, 1.0f)),
            profile,
            Table);

        // assert
        similarity.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_ScaledPlotExtremes_ProduceFiniteUnitCosine()
    {
        // arrange
        var profile = Profile(
            new Dictionary<string, double>(),
            Vector((0, float.Epsilon), (1, float.MaxValue)));
        var candidate = Vector((0, float.Epsilon), (1, float.MaxValue));

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            candidate,
            profile,
            Table);

        // assert
        similarity.ShouldBe(1.0, Tolerance);
        double.IsFinite(similarity).ShouldBeTrue();
        similarity.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Similarity_ZeroCandidateVector_IsNotComparable()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), Vector((0, 1.0f)));

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            ZeroVector(),
            profile,
            Table);

        // assert
        similarity.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_MalformedCandidateVector_ThrowsArgumentException()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), Vector((0, 1.0f)));

        // act / assert
        Should.Throw<ArgumentException>(() => ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            ImmutableArray.Create(1.0f),
            profile,
            Table));
    }

    [Fact]
    public void Similarity_NonFiniteCandidateVector_ThrowsArgumentException()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), Vector((0, 1.0f)));
        var malformed = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);

        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            malformed.Add(index == 0 ? float.NaN : 0.0f);
        }

        // act / assert
        Should.Throw<ArgumentException>(() => ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            malformed.MoveToImmutable(),
            profile,
            Table));
    }

    [Fact]
    public void Similarity_MalformedProfileVector_ThrowsArgumentException()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), ImmutableArray.Create(1.0f));

        // act / assert
        Should.Throw<ArgumentException>(() => ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            profile,
            Table));
    }

    [Fact]
    public void Similarity_NullProfile_ThrowsArgumentNullException()
    {
        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        Should.Throw<ArgumentNullException>(() => ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            null!,
            Table));
    }

    [Fact]
    public void Similarity_NullGenreWeights_ThrowsArgumentNullException()
    {
        // arrange
        var profile = Profile(new Dictionary<string, double>(), ImmutableArray<float>.Empty);

        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        Should.Throw<ArgumentNullException>(() => ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            profile,
            null!));
    }

    internal static TasteProfile Profile(
        Dictionary<string, double> preferences,
        ImmutableArray<float> embedding) =>
        new(
            preferences.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
            embedding);

    internal static ImmutableArray<float> Vector(params (int Axis, float Value)[] coordinates)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        var values = new float[EmbeddingDimensions.Value];

        foreach (var (axis, value) in coordinates)
        {
            values[axis] = value;
        }

        foreach (var value in values)
        {
            builder.Add(value);
        }

        return builder.MoveToImmutable();
    }

    internal static ImmutableArray<float> ZeroVector() => Vector();
}
