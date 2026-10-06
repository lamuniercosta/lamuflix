using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class RequestEnrichmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private const int PriorAttempts = 3;

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();
    private readonly RequestEnrichmentCommandHandler handler;
    private readonly Fixture fixture = new();

    public RequestEnrichmentCommandHandlerTests()
    {
        handler = new RequestEnrichmentCommandHandler(movies, queue);
    }

    [Theory]
    [MemberData(nameof(Statuses))]
    public async Task HandleAsync_Status_EnforcesGuard(EnrichmentStatus status)
    {
        // arrange
        var movie = MovieIn(status);
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        var allowed = status == EnrichmentStatus.NotFound || status == EnrichmentStatus.Failed;

        // act
        if (!allowed)
        {
            var thrown = await Should.ThrowAsync<InvalidTransitionException>(
                () => handler.HandleAsync(new RequestEnrichmentCommand(movie.Id), ct));

            // assert
            thrown.Action.ShouldBe(nameof(Movie.RequestEnrichment));
            thrown.State.ShouldBe(status.ToString());
            movie.Status.ShouldBe(status);
            movie.EnrichmentAttempts.ShouldBe(PriorAttempts);
            await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
            return;
        }

        var result = await handler.HandleAsync(new RequestEnrichmentCommand(movie.Id), ct);

        // assert
        result.ShouldBe(movie.Id);
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.EnrichmentAttempts.ShouldBe(PriorAttempts);
        await movies.Received(1).SaveChangesAsync(ct);
        await queue.Received(1).EnqueueAsync(
            Arg.Is<EnrichmentRequested>(message => message.MovieId == movie.Id && message.Attempt == 1),
            ct);
        Received.InOrder(() =>
        {
            movies.SaveChangesAsync(ct);
            queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), ct);
        });
    }

    [Fact]
    public async Task HandleAsync_QueueFailure_PropagatesAfterSave()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.NotFound);
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), ct)
            .ThrowsAsync(new InvalidOperationException("broker"));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new RequestEnrichmentCommand(movie.Id), ct));

        // assert
        movie.EnrichmentAttempts.ShouldBe(PriorAttempts);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFound()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.GetAsync(id, ct).Returns((Movie?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new RequestEnrichmentCommand(id), ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<EnrichmentStatus> Statuses() =>
    [
        EnrichmentStatus.NotFound,
        EnrichmentStatus.Failed,
        EnrichmentStatus.Pending,
        EnrichmentStatus.Enriched,
    ];

    private Movie MovieIn(EnrichmentStatus status) =>
        Movie.Rehydrate(
            NewId(),
            "Title",
            new LibraryPath("C:/library/a"),
            new MediaFormat("mkv"),
            false,
            status == EnrichmentStatus.Enriched ? new MovieMetadata("Enriched") : null,
            status,
            status == EnrichmentStatus.Enriched ? Now : null,
            PriorAttempts,
            status == EnrichmentStatus.Failed ? EnrichmentFailureCategory.Unknown : null,
            status == EnrichmentStatus.Pending ? null : Now);

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);
}
