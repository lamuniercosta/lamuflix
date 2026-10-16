using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class DeleteMovieCommandHandlerTests
{
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly DeleteMovieCommandHandler handler;
    private readonly Fixture fixture = new();

    public DeleteMovieCommandHandlerTests()
    {
        handler = new DeleteMovieCommandHandler(movies);
    }

    [Fact]
    public async Task HandleAsync_RemovedMovie_ReturnsUnit()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.TryDeleteAsync(id, ct).Returns(true);

        // act
        var result = await handler.HandleAsync(new DeleteMovieCommand(id), ct);

        // assert
        result.ShouldBe(Unit.Value);
    }

    [Fact]
    public async Task HandleAsync_AbsentMovie_ThrowsNotFound()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.TryDeleteAsync(id, ct).Returns(false);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new DeleteMovieCommand(id), ct));
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        // arrange
        var id = NewId();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        movies.TryDeleteAsync(id, ct).Returns(true);

        // act
        await handler.HandleAsync(new DeleteMovieCommand(id), ct);

        // assert
        await movies.Received(1).TryDeleteAsync(id, ct);
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
