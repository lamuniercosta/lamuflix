using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMatchManuallySurvivorTests
{
    [Fact]
    public void MatchManually_NullImdbId_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Movie.Create(new MovieId(1), "Arrival", new LibraryPath("C:/movie.mkv"), new MediaFormat("mkv"));

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => movie.MatchManually(null!));

        // assert
        failure.ParamName.ShouldBe("imdbId");
    }
}
