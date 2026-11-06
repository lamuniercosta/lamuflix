using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Feedback;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Feedback;

public sealed class SetMovieReviewCommandHandlerTests
{
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly SetMovieReviewCommandHandler handler;
    private readonly Fixture fixture = new();

    public SetMovieReviewCommandHandlerTests()
    {
        handler = new SetMovieReviewCommandHandler(movies);
    }

    public static TheoryData<string, int> ReviewNames => new()
    {
        { "none", 0 },
        { "liked", 1 },
        { "disliked", 2 },
    };

    public static TheoryData<string?> InvalidReviewNames => new()
    {
        { (string?)null },
        { string.Empty },
        { "   " },
        { "Lik" },
        { "Liked" },
        { "LIKED" },
        { "love" },
        { "2" },
    };

    [Theory]
    [MemberData(nameof(ReviewNames))]
    public async Task HandleAsync_KnownReviewName_AssignsExactSmartEnumAndSavesOnce(string name, int expectedValue)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var result = await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, name), ct);

        // assert
        result.ShouldBe(Unit.Value);
        movie.Review.Value.ShouldBe(expectedValue);
        movie.Review.Name.ShouldBe(name);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Theory]
    [MemberData(nameof(ReviewNames))]
    public async Task HandleAsync_RepeatedAssignment_RemainsIdempotentWithoutConflict(string name, int expectedValue)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, name), ct);
        var result = await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, name), ct);

        // assert
        result.ShouldBe(Unit.Value);
        movie.Review.Value.ShouldBe(expectedValue);
        await movies.Received(2).SaveChangesAsync(ct);
    }

    [Theory]
    [InlineData("liked", "disliked", 2)]
    [InlineData("disliked", "liked", 1)]
    [InlineData("liked", "none", 0)]
    [InlineData("disliked", "none", 0)]
    public async Task HandleAsync_OppositeOrClearingAssignment_SwitchesDirectlyWithoutIntermediateState(
        string before,
        string next,
        int expectedValue)
    {
        // arrange
        var movie = PendingMovie();
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, before), CancellationToken.None);

        // act
        await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, next), CancellationToken.None);

        // assert
        movie.Review.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [MemberData(nameof(InvalidReviewNames))]
    public async Task HandleAsync_InvalidReviewName_ThrowsReviewValidationWithoutLoadOrSave(string? name)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var thrown = await Should.ThrowAsync<ValidationException>(
            () => handler.HandleAsync(new SetMovieReviewCommand(movie.Id, name), ct));

        // assert
        thrown.Errors.Keys.ShouldContain("review");
        movie.Review.ShouldBe(Review.None);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFoundWithoutSave()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.GetAsync(id, ct).Returns((Movie?)null);

        // act
        var thrown = await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new SetMovieReviewCommand(id, "liked"), ct));

        // assert
        thrown.ShouldNotBeNull();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LikedMovie_KeepsWatchedAndWatchlistIndependent()
    {
        // arrange
        var movie = PendingMovie();
        movie.MarkWatched();
        movie.AddToWatchlist();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, "liked"), ct);

        // assert
        movie.Review.ShouldBe(Review.Liked);
        movie.IsWatched.ShouldBeTrue();
        movie.IsInWatchlist.ShouldBeTrue();
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        // arrange
        var movie = PendingMovie();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        await handler.HandleAsync(new SetMovieReviewCommand(movie.Id, "disliked"), ct);

        // assert
        await movies.Received(1).GetAsync(movie.Id, ct);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    private Movie PendingMovie()
    {
        var title = Faker.Lorem.GetFirstWord();
        return Movie.Create(NewId(), string.IsNullOrWhiteSpace(title) ? "Title" : title, new LibraryPath("C:/library/a"), new MediaFormat("mkv"));
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
