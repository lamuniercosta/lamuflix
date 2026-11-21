using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class TasteProfileTests
{
    [Fact]
    public void IsEmpty_NoGenrePreferencesAndNoPlayback_IsTrue()
    {
        // arrange
        var profile = new TasteProfile(ImmutableDictionary<string, double>.Empty);

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
        var profile = new TasteProfile(preferences);

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty_PlaybackPopulated_IsFalse()
    {
        // arrange
        var profile = new TasteProfile(ImmutableDictionary<string, double>.Empty)
        {
            PlaybackByMovieId = ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty
                .Add(new MovieId(7), [DateTimeOffset.UnixEpoch]),
        };

        // act
        var isEmpty = profile.IsEmpty;

        // assert
        isEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Construct_EmptyPreferences_ExposesProvidedValues()
    {
        // arrange
        var preferences = ImmutableDictionary<string, double>.Empty;

        // act
        var profile = new TasteProfile(preferences);

        // assert
        profile.GenrePreferences.ShouldBeSameAs(preferences);
        profile.GenrePreferences.IsEmpty.ShouldBeTrue();
        profile.PlaybackByMovieId.IsEmpty.ShouldBeTrue();
    }
}
