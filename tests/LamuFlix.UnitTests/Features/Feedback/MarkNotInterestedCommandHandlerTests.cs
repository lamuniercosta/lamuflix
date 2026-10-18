using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Feedback;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Feedback;

public sealed class MarkNotInterestedCommandHandlerTests
{
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly MarkNotInterestedCommandHandler handler;
    private readonly Fixture fixture = new();

    public MarkNotInterestedCommandHandlerTests()
    {
        handler = new MarkNotInterestedCommandHandler(movies);
    }

    [Fact]
    public async Task HandleAsync_StoredMovieInterested_SavesOnceAndReturnsUnit()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var result = await handler.HandleAsync(new MarkNotInterestedCommand(movie.Id), ct);

        // assert
        result.ShouldBe(Unit.Value);
        movie.IsNotInterested.ShouldBeTrue();
        movie.IsWatched.ShouldBeFalse();
        movie.IsInWatchlist.ShouldBeFalse();
        await movies.Received(1).SaveChangesAsync(ct);
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
            () => handler.HandleAsync(new MarkNotInterestedCommand(id), ct));

        // assert
        thrown.ShouldNotBeNull();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyNotInterested_PropagatesWithoutSave()
    {
        // arrange
        var movie = PendingMovie();
        movie.MarkNotInterested();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var thrown = await Should.ThrowAsync<InvalidTransitionException>(
            () => handler.HandleAsync(new MarkNotInterestedCommand(movie.Id), ct));

        // assert
        thrown.Action.ShouldBe(nameof(Movie.MarkNotInterested));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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
        await handler.HandleAsync(new MarkNotInterestedCommand(movie.Id), ct);

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
