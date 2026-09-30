using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class RequeueStrandedMoviesCommandHandlerTests
{
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();
    private readonly RequeueStrandedMoviesCommandHandler handler;
    private readonly Fixture fixture = new();

    public RequeueStrandedMoviesCommandHandlerTests()
    {
        handler = new RequeueStrandedMoviesCommandHandler(queue);
    }

    [Fact]
    public async Task HandleAsync_EnqueuesEachIdWithoutClaiming()
    {
        // arrange
        var first = NewId();
        var second = NewId();
        var ct = CancellationToken.None;

        // act
        var count = await handler.HandleAsync(new RequeueStrandedMoviesCommand([first, second]), ct);

        // assert
        count.ShouldBe(2);
        await queue.Received(1).EnqueueAsync(
            Arg.Is<EnrichmentRequested>(message => message.MovieId == first && message.Attempt == 1),
            ct);
        await queue.Received(1).EnqueueAsync(
            Arg.Is<EnrichmentRequested>(message => message.MovieId == second && message.Attempt == 1),
            ct);
    }

    [Fact]
    public async Task HandleAsync_Empty_ReturnsZero()
    {
        // arrange
        var ct = CancellationToken.None;

        // act
        var count = await handler.HandleAsync(new RequeueStrandedMoviesCommand([]), ct);

        // assert
        count.ShouldBe(0);
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QueueFailure_Propagates()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), ct)
            .ThrowsAsync(new InvalidOperationException("broker"));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new RequeueStrandedMoviesCommand([id]), ct));
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        // arrange
        var id = NewId();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // act
        await handler.HandleAsync(new RequeueStrandedMoviesCommand([id]), ct);

        // assert
        await queue.Received(1).EnqueueAsync(Arg.Any<EnrichmentRequested>(), ct);
    }

    private MovieId NewId() => new(System.Math.Abs(fixture.Create<int>()) + 1);
}
