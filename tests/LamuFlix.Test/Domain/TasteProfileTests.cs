using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Domain;

public sealed class TasteProfileTests
{
    [Fact]
    public void IsEmpty_BothChannelsEmpty_IsTrue()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty);

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_PlotEmbeddingDefault_IsTrue()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            default(ImmutableArray<float>));

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_GenrePreferencesPopulated_IsFalse()
    {
        // arrange
        var preferences = ImmutableDictionary<string, double>.Empty.Add("Drama", 0.25);
        var profile = new TasteProfile(preferences, ImmutableArray<float>.Empty);

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty_PlotEmbeddingPopulated_IsFalse()
    {
        // arrange
        var profile = new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            [0.5f, 0.25f]);

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty_BothChannelsPopulated_IsFalse()
    {
        // arrange
        var preferences = ImmutableDictionary<string, double>.Empty.Add("Drama", 0.25);
        var profile = new TasteProfile(preferences, [0.5f, 0.25f]);

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Construct_EmptyChannels_ExposesProvidedValues()
    {
        // arrange
        var preferences = ImmutableDictionary<string, double>.Empty;

        // act
        var profile = new TasteProfile(preferences, ImmutableArray<float>.Empty);

        // assert
        profile.GenrePreferences.ShouldBeSameAs(preferences);
        profile.GenrePreferences.IsEmpty.ShouldBeTrue();
        profile.PlotEmbedding.IsDefaultOrEmpty.ShouldBeTrue();
    }
}
