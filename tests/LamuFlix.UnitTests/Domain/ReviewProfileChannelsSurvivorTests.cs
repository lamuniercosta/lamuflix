using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class ReviewProfileChannelsSurvivorTests
{
    [Fact]
    public void IsEmpty_EmptyChannels_IsTrue()
    {
        // arrange
        var channels = ReviewProfileChannels.Empty;

        // act
        var empty = channels.IsEmpty;

        // assert
        empty.ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_OnlyPlotEmbeddingPopulated_IsFalse()
    {
        // arrange
        var channels = ReviewProfileChannels.Empty with
        {
            PlotEmbedding = ImmutableArray.Create(0.1f),
        };

        // act
        var empty = channels.IsEmpty;

        // assert
        empty.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty_OnlyGenresPopulated_IsFalse()
    {
        // arrange
        var channels = ReviewProfileChannels.Empty with
        {
            Genres = ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase).Add("Drama", 1.0),
        };

        // act
        var empty = channels.IsEmpty;

        // assert
        empty.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty_OnlyCastPopulated_IsFalse()
    {
        // arrange
        var channels = ReviewProfileChannels.Empty with
        {
            Cast = ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase).Add("Lead", 1.0),
        };

        // act
        var empty = channels.IsEmpty;

        // assert
        empty.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty_OnlyDirectorsPopulated_IsFalse()
    {
        // arrange
        var channels = ReviewProfileChannels.Empty with
        {
            Directors = ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase).Add("Auteur", 1.0),
        };

        // act
        var empty = channels.IsEmpty;

        // assert
        empty.ShouldBeFalse();
    }
}
