using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class TasteProfileMovieSurvivorTests
{
    [Fact]
    public void Title_Default_IsEmpty()
    {
        // arrange
        var movie = new TasteProfileMovie(
            new MovieId(1),
            ImmutableArray<string>.Empty,
            ImmutableArray<float>.Empty,
            ImmutableArray<DateTimeOffset>.Empty,
            IsWatched: false);

        // act
        var title = movie.Title;

        // assert
        title.ShouldBe(string.Empty);
    }
}
