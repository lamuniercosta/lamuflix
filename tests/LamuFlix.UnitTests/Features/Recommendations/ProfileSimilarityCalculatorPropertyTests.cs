using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class ProfileSimilarityCalculatorPropertyTests
{
    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Drama"] = 20,
                ["Comedy"] = 60,
                ["Thriller"] = 90,
            }));

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Similarity_ValidInputs_IsFiniteWithinUnitInterval(
        string[]? genres,
        int[]? preferenceSeeds,
        double[]? embeddingSeeds)
    {
        // arrange
        var profile = Profile(
            Genres(genres),
            Values(preferenceSeeds),
            Vector(embeddingSeeds, 0));
        var candidate = Vector(embeddingSeeds, 1);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            [.. Genres(genres)],
            candidate,
            profile,
            Table);

        // assert
        return double.IsFinite(similarity) && similarity is >= 0.0 and <= 1.0;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Similarity_GenreOrder_DoesNotChangeTheResult(
        string[]? genres,
        int[]? preferenceSeeds)
    {
        // arrange
        var values = Genres(genres);
        var reversed = values.Reverse().ToArray();
        var profile = Profile(values, Values(preferenceSeeds), ImmutableArray<float>.Empty);

        // act
        var forward = ProfileSimilarityCalculator.Similarity([.. values], ImmutableArray<float>.Empty, profile, Table);
        var backward = ProfileSimilarityCalculator.Similarity([.. reversed], ImmutableArray<float>.Empty, profile, Table);

        // assert
        return Math.Abs(forward - backward) <= 1e-12;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Similarity_GenreCaseAndWhitespace_DoesNotChangeTheResult(
        int[]? preferenceSeeds)
    {
        // arrange
        var genres = new[] { "Drama", "Comedy" };
        var varied = new[] { "  drama ", "COMEDY\t" };
        var profile = Profile(genres, Values(preferenceSeeds), ImmutableArray<float>.Empty);

        // act
        var canonical = ProfileSimilarityCalculator.Similarity(
            [.. genres],
            ImmutableArray<float>.Empty,
            profile,
            Table);
        var noisy = ProfileSimilarityCalculator.Similarity(
            [.. varied],
            ImmutableArray<float>.Empty,
            profile,
            Table);

        // assert
        return Math.Abs(canonical - noisy) <= 1e-12;
    }

    [Property(MaxTest = 50)]
    [Trait("Category", "Property")]
    public bool Similarity_PlotDirection_IgnoresTheSineOfTheVector(
        double[]? embeddingSeeds)
    {
        // arrange
        var positive = Vector(embeddingSeeds, 0);
        var negative = ImmutableArray.CreateRange(positive.Select(value => -value));
        var profile = Profile(ImmutableArray<string>.Empty, new Dictionary<string, double>(), positive);

        // act
        var similarity = ProfileSimilarityCalculator.Similarity(
            ImmutableArray<string>.Empty,
            negative,
            profile,
            Table);

        // assert
        return double.IsFinite(similarity) && similarity is >= 0.0 and <= 1.0;
    }

    private static TasteProfile Profile(
        IEnumerable<string> genres,
        Dictionary<string, double> preferences,
        ImmutableArray<float> embedding)
    {
        var genrePreferences = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (genre, preference) in preferences)
        {
            genrePreferences[genre] = preference;
        }

        foreach (var genre in genres)
        {
            genrePreferences.TryAdd(genre.Trim(), 0.0);
        }

        var profile = ProfileSimilarityCalculatorTests.Profile(genrePreferences, embedding);
        return profile;
    }

    private static Dictionary<string, double> Values(int[]? seeds)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var names = new[] { "Drama", "Comedy", "Thriller", "Horror" };
        var source = seeds ?? [1, 2, 3, 4];

        for (var index = 0; index < names.Length; index++)
        {
            var seed = source.Length == 0 ? 1 : source[index % source.Length];
            var value = Math.Abs(seed % 1000.0) / 1000.0;
            values[names[index]] = double.IsFinite(value) ? value : 0.0;
        }

        return values;
    }

    private static string[] Genres(string[]? genres)
    {
        var source = genres ?? ["Drama"];
        var values = new List<string>(source.Length);

        foreach (var genre in source)
        {
            if (!string.IsNullOrWhiteSpace(genre))
            {
                values.Add(genre.Trim());
            }
        }

        return [.. values];
    }

    private static ImmutableArray<float> Vector(double[]? seeds, int offset)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        var source = seeds ?? [1, 2, 3, 4];

        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            var seed = source.Length == 0 ? 1 : source[(index + offset) % source.Length];
            var value = Math.Abs(seed % 100.0) / 100.0;
            builder.Add(double.IsFinite(value) ? (float)value : 0.0f);
        }

        return builder.MoveToImmutable();
    }
}
