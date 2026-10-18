using System;
using System.Linq;
using LamuFlix.Core.Domain;
using Shouldly;

namespace LamuFlix.UnitTests.Domain;

public sealed class PlaybackEventTests
{
    private static readonly DateTimeOffset PlayedAt = new(2026, 10, 18, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_GivenMovieIdAndPlayedAt_ExposesBothValues()
    {
        // arrange
        var movieId = new MovieId(7);

        // act
        var playbackEvent = new PlaybackEvent(movieId, PlayedAt);

        // assert
        playbackEvent.MovieId.ShouldBe(movieId);
        playbackEvent.PlayedAt.ShouldBe(PlayedAt);
    }

    [Fact]
    public void Properties_ContainNoPersistenceIdentityMember()
    {
        // act
        var propertyNames = typeof(PlaybackEvent)
            .GetProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // assert
        propertyNames.ShouldBe(new[] { "MovieId", "PlayedAt" });
    }
}
