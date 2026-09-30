using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ApplyEnrichmentResultCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly ApplyEnrichmentResultCommandHandler handler;
    private readonly Fixture fixture = new();

    public ApplyEnrichmentResultCommandHandlerTests()
    {
        handler = new ApplyEnrichmentResultCommandHandler(movies, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task HandleAsync_Found_MarksEnrichedSavesAndReturnsStatus()
    {
        // arrange
        var movie = PendingMovie();
        var metadata = new MovieMetadata("Enriched Title");
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var status = await handler.HandleAsync(
            new ApplyEnrichmentResultCommand(movie.Id, new MetadataLookupResult.Found(metadata)),
            ct);

        // assert
        status.ShouldBe(EnrichmentStatus.Enriched);
        movie.LastAttemptAt.ShouldBe(Now);
        movie.Metadata.ShouldBe(metadata);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_NotFound_MarksNotFoundSavesAndReturnsStatus()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);

        // act
        var status = await handler.HandleAsync(
            new ApplyEnrichmentResultCommand(movie.Id, new MetadataLookupResult.NotFound()),
            ct);

        // assert
        status.ShouldBe(EnrichmentStatus.NotFound);
        movie.LastAttemptAt.ShouldBe(Now);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_Failed_ThrowsBeforeLoadOrSave()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;

        // act
        var thrown = await Should.ThrowAsync<ArgumentException>(
            () => handler.HandleAsync(
                new ApplyEnrichmentResultCommand(id, new MetadataLookupResult.Failed(EnrichmentFailureCategory.Unknown)),
                ct));

        // assert
        thrown.ParamName.ShouldBe("command");
        thrown.Message.ShouldContain("Failed lookup results cannot be applied.");
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
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
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(
                new ApplyEnrichmentResultCommand(id, new MetadataLookupResult.NotFound()),
                ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Movie PendingMovie()
    {
        var title = Faker.Lorem.GetFirstWord();
        return Movie.Create(
            NewId(),
            string.IsNullOrWhiteSpace(title) ? "Title" : title,
            new LibraryPath("C:/library/a"),
            new MediaFormat("mkv"));
    }

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);
}
