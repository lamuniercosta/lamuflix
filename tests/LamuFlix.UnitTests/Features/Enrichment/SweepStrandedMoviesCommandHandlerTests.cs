using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class SweepStrandedMoviesCommandHandlerTests
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly IMovieRepository movieRepository = Substitute.For<IMovieRepository>();
    private readonly ICommandHandler<RequeueStrandedMoviesCommand, int> requeueHandler =
        Substitute.For<ICommandHandler<RequeueStrandedMoviesCommand, int>>();

    [Fact]
    public async Task HandleAsync_EmptySelection_ReturnsZeroWithoutRequeue()
    {
        // arrange
        movieRepository.FindStrandedMovieIdsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MovieId>());
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SweepStrandedMoviesCommand(), CancellationToken.None);

        // assert
        Assert.Equal(0, result);
        await requeueHandler.DidNotReceive()
            .HandleAsync(Arg.Any<RequeueStrandedMoviesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UsesCapturedTimeForLeaseCutoff()
    {
        // arrange
        var cutoff = Now - ClaimLease;
        var timeProvider = new FixedTimeProvider(Now);
        movieRepository.FindStrandedMovieIdsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MovieId>());
        var handler = CreateHandler(timeProvider);

        // act
        await handler.HandleAsync(new SweepStrandedMoviesCommand(), CancellationToken.None);

        // assert
        await movieRepository.Received(1).FindStrandedMovieIdsAsync(cutoff, Arg.Any<CancellationToken>());
        Assert.Equal(1, timeProvider.CallCount);
    }

    [Fact]
    public async Task HandleAsync_SelectedIds_DispatchesExistingRequeueCommand()
    {
        // arrange
        MovieId[] ids = [new(1), new(2)];
        movieRepository.FindStrandedMovieIdsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ids);
        requeueHandler.HandleAsync(Arg.Any<RequeueStrandedMoviesCommand>(), Arg.Any<CancellationToken>())
            .Returns(2);
        var handler = CreateHandler();

        // act
        var result = await handler.HandleAsync(new SweepStrandedMoviesCommand(), CancellationToken.None);

        // assert
        Assert.Equal(2, result);
        await requeueHandler.Received(1).HandleAsync(
            Arg.Is<RequeueStrandedMoviesCommand>(command => ReferenceEquals(command.MovieIds, ids)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationTokenToSelectionAndRequeue()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        movieRepository.FindStrandedMovieIdsAsync(Arg.Any<DateTimeOffset>(), token)
            .Returns(new List<MovieId> { new(1) });
        var handler = CreateHandler();

        // act
        await handler.HandleAsync(new SweepStrandedMoviesCommand(), token);

        // assert
        await movieRepository.Received(1).FindStrandedMovieIdsAsync(Arg.Any<DateTimeOffset>(), token);
        await requeueHandler.Received(1).HandleAsync(Arg.Any<RequeueStrandedMoviesCommand>(), token);
    }

    [Fact]
    public async Task HandleAsync_RequeueFailure_Propagates()
    {
        // arrange
        var failure = new InvalidOperationException("queue failed");
        movieRepository.FindStrandedMovieIdsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<MovieId> { new(1) });
        requeueHandler.HandleAsync(Arg.Any<RequeueStrandedMoviesCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(failure);
        var handler = CreateHandler();

        // act / assert
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new SweepStrandedMoviesCommand(), CancellationToken.None));
        Assert.Same(failure, actual);
    }

    private SweepStrandedMoviesCommandHandler CreateHandler(FixedTimeProvider? timeProvider = null) => new(
        movieRepository,
        Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
        timeProvider ?? new FixedTimeProvider(Now),
        requeueHandler);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public int CallCount { get; private set; }

        public override DateTimeOffset GetUtcNow()
        {
            CallCount++;
            return now;
        }
    }
}