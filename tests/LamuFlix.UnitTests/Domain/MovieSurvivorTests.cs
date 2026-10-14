using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieSurvivorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_BlankTitle_ThrowsArgumentExceptionWithParamNameAndMessage(string title)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => Movie.Create(
            new MovieId(1), title, new LibraryPath("C:/movie.mkv"), new MediaFormat("mkv")));

        // assert
        thrown.ParamName.ShouldBe("title");
        thrown.Message.ShouldContain("Title is required");
    }

    [Fact]
    public void MarkFailed_PendingMovie_RecordsCategoryAndAttemptTime()
    {
        // arrange
        var movie = NewMovie();

        // act
        movie.MarkFailed(EnrichmentFailureCategory.RateLimited, Now);

        // assert
        movie.Status.ShouldBe(EnrichmentStatus.Failed);
        movie.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        movie.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public void MarkFailed_AlreadyFailedMovie_ThrowsInvalidTransitionWithActionAndState()
    {
        // arrange
        var movie = NewMovie();
        movie.MarkFailed(EnrichmentFailureCategory.RateLimited, Now);

        // act
        var thrown = Should.Throw<InvalidTransitionException>(
            () => movie.MarkFailed(EnrichmentFailureCategory.ProviderUnavailable, Later));

        // assert
        thrown.Action.ShouldBe("MarkFailed");
        thrown.State.ShouldBe("Failed");
        movie.LastAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public void RequestEnrichment_PendingMovie_ThrowsInvalidTransitionWithActionAndState()
    {
        // arrange
        var movie = NewMovie();

        // act
        var thrown = Should.Throw<InvalidTransitionException>(() => movie.RequestEnrichment());

        // assert
        thrown.Action.ShouldBe("RequestEnrichment");
        thrown.State.ShouldBe("Pending");
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public void RequestEnrichment_FailedMovie_ReturnsToPending()
    {
        // arrange
        var movie = NewMovie();
        movie.MarkFailed(EnrichmentFailureCategory.RateLimited, Now);

        // act
        movie.RequestEnrichment();

        // assert
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
    }

    private static Movie NewMovie() =>
        Movie.Create(new MovieId(1), "Arrival", new LibraryPath("C:/movie.mkv"), new MediaFormat("mkv"));
}
