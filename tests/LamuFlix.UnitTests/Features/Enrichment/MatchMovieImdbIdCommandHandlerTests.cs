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

public sealed class MatchMovieImdbIdCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    private const int PriorAttempts = 3;

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentOutbox outbox = Substitute.For<IEnrichmentOutbox>();
    private readonly MatchMovieImdbIdCommandHandler handler;
    private readonly Fixture fixture = new();

    public MatchMovieImdbIdCommandHandlerTests()
    {
        handler = new MatchMovieImdbIdCommandHandler(movies, outbox);
    }

    [Theory]
    [MemberData(nameof(Statuses))]
    public async Task HandleAsync_AnyStatus_SetsManualIdAndStagesTheRequestBeforeTheSharedSave(EnrichmentStatus status)
    {
        // arrange
        var movie = MovieIn(status);
        var imdbId = new ImdbId("tt0083658");
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var result = await handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, imdbId), ct);

        // assert
        result.ShouldBe(movie.Id);
        movie.ManualImdbId.ShouldBe(imdbId);
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.EnrichmentAttempts.ShouldBe(PriorAttempts);
        outbox.Received(1).Stage(
            Arg.Is<EnrichmentRequested>(message => message.MovieId == movie.Id && message.Attempt == 1));
        await movies.Received(1).SaveChangesAsync(ct);
        Received.InOrder(() =>
        {
            outbox.Stage(Arg.Any<EnrichmentRequested>());
            movies.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task HandleAsync_SaveThrows_PropagatesAfterStaging()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.NotFound);
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        movies.SaveChangesAsync(ct).ThrowsAsync(new InvalidOperationException("save"));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, new ImdbId("tt0083658")), ct));

        // assert
        outbox.Received(1).Stage(Arg.Any<EnrichmentRequested>());
    }

    [Fact]
    public async Task HandleAsync_SaveHitsTheImdbIdUniqueIndex_ThrowsConflictNamingTheRowThatWonTheRace()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.NotFound);
        var imdbId = new ImdbId("tt0083658");
        var ct = CancellationToken.None;
        var winner = NewId();
        movies.GetAsync(movie.Id, ct).Returns(movie);
        movies.SaveChangesAsync(ct).ThrowsAsync(new DuplicateImdbIdException("IX_movies_imdb_id"));
        movies.FindMovieIdByImdbIdAsync(imdbId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<MovieId?>(null), Task.FromResult<MovieId?>(winner));

        // act
        var thrown = await Should.ThrowAsync<ImdbIdConflictException>(
            () => handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, imdbId), ct));

        // assert
        thrown.ImdbId.ShouldBe(imdbId);
        thrown.HeldBy.ShouldBe(winner);
        outbox.Received(1).Stage(Arg.Any<EnrichmentRequested>());
    }

    [Fact]
    public async Task HandleAsync_SaveHitsTheImdbIdUniqueIndexButNoHolderIsVisible_RethrowsTheDuplicate()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.NotFound);
        var imdbId = new ImdbId("tt0083658");
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        movies.SaveChangesAsync(ct).ThrowsAsync(new DuplicateImdbIdException("IX_movies_imdb_id"));
        movies.FindMovieIdByImdbIdAsync(imdbId, Arg.Any<CancellationToken>()).Returns((MovieId?)null);

        // act
        var thrown = await Should.ThrowAsync<DuplicateImdbIdException>(
            () => handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, imdbId), ct));

        // assert
        thrown.Message.ShouldBe("IX_movies_imdb_id");
    }

    [Fact]
    public async Task HandleAsync_SaveHitsTheImdbIdUniqueIndexHeldByTheSameMovie_RethrowsTheDuplicate()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.NotFound);
        var imdbId = new ImdbId("tt0083658");
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        movies.SaveChangesAsync(ct).ThrowsAsync(new DuplicateImdbIdException("IX_movies_imdb_id"));
        movies.FindMovieIdByImdbIdAsync(imdbId, Arg.Any<CancellationToken>()).Returns(movie.Id);

        // act
        await Should.ThrowAsync<DuplicateImdbIdException>(
            () => handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, imdbId), ct));
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
            () => handler.HandleAsync(new MatchMovieImdbIdCommand(id, new ImdbId("tt0083658")), ct));

        // assert
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IdHeldByAnotherMovie_ThrowsConflictAndLeavesTheMovieUnchanged()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.NotFound);
        var imdbId = new ImdbId("tt0083658");
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        var holderId = movie.Id.Value == 99 ? new MovieId(100) : new MovieId(99);
        movies.FindMovieIdByImdbIdAsync(imdbId, ct).Returns(holderId);

        // act
        var thrown = await Should.ThrowAsync<ImdbIdConflictException>(
            () => handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, imdbId), ct));

        // assert
        thrown.ImdbId.ShouldBe(imdbId);
        thrown.HeldBy.ShouldBe(holderId);
        movie.ManualImdbId.ShouldBeNull();
        movie.Status.ShouldBe(EnrichmentStatus.NotFound);
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IdHeldByTheSameMovie_Succeeds()
    {
        // arrange
        var movie = MovieIn(EnrichmentStatus.Failed);
        var imdbId = new ImdbId("tt0083658");
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        movies.FindMovieIdByImdbIdAsync(imdbId, ct).Returns(movie.Id);

        // act
        var result = await handler.HandleAsync(new MatchMovieImdbIdCommand(movie.Id, imdbId), ct);

        // assert
        result.ShouldBe(movie.Id);
        movie.ManualImdbId.ShouldBe(imdbId);
        await movies.Received(1).SaveChangesAsync(ct);
        outbox.Received(1).Stage(
            Arg.Is<EnrichmentRequested>(message => message.MovieId == movie.Id && message.Attempt == 1));
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
