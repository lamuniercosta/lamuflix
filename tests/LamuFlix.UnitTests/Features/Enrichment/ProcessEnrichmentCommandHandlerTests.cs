using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ProcessEnrichmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private const int MaxAttempts = 3;

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMetadataProvider provider = Substitute.For<IMetadataProvider>();
    private readonly RecordingLogger<ProcessEnrichmentCommandHandler> logger = new();
    private readonly ProcessEnrichmentCommandHandler handler;
    private readonly Fixture fixture = new();

    public ProcessEnrichmentCommandHandlerTests()
    {
        handler = new ProcessEnrichmentCommandHandler(
            movies,
            provider,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = MaxAttempts }),
            new FixedTimeProvider(Now),
            logger);
    }

    [Fact]
    public async Task HandleAsync_RefusedClaim_ReturnsCompletedNotClaimedWithoutLookup()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.TryClaimForEnrichmentAsync(id, ct).Returns(false);

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(false));
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await provider.DidNotReceive().FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Found_MarksEnrichedSavesAndReportsClaimed()
    {
        // arrange
        var movie = PendingMovie();
        var metadata = new MovieMetadata("Enriched Title");
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns(new MetadataLookupResult.Found(metadata));
        var attempts = movie.EnrichmentAttempts;

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(true));
        movie.Status.ShouldBe(EnrichmentStatus.Enriched);
        movie.Metadata.ShouldBe(metadata);
        movie.EnrichmentAttempts.ShouldBe(attempts);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_NotFound_MarksNotFoundSavesAndReportsClaimed()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns(new MetadataLookupResult.NotFound());
        var attempts = movie.EnrichmentAttempts;

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(true));
        movie.Status.ShouldBe(EnrichmentStatus.NotFound);
        movie.EnrichmentAttempts.ShouldBe(attempts);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_UnenrichedMovie_LooksUpWithNullReleaseYear()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns(new MetadataLookupResult.NotFound());
        var attempts = movie.EnrichmentAttempts;

        // act
        await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        movie.EnrichmentAttempts.ShouldBe(attempts);
        await provider.Received(1).FindAsync(new MetadataLookup(movie.Title, null), ct);
    }

    [Fact]
    public async Task HandleAsync_EnrichedMovie_LooksUpWithMetadataReleaseYear()
    {
        // arrange
        var releaseYear = new ReleaseYear(1999, Now);
        var movie = EnrichedMovie(releaseYear);
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns(new MetadataLookupResult.NotFound());
        var attempts = movie.EnrichmentAttempts;

        // act
        await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        movie.EnrichmentAttempts.ShouldBe(attempts);
        await provider.Received(1).FindAsync(new MetadataLookup(movie.Title, releaseYear), ct);
    }

    [Fact]
    public async Task HandleAsync_MissingMovieAfterClaim_ThrowsNotFound()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.TryClaimForEnrichmentAsync(id, ct).Returns(true);
        movies.GetAsync(id, ct).Returns((Movie?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new ProcessEnrichmentCommand(id, 1), ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderFailedResult_RetryableCategory_ReturnsRetryAndWritesNothing()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable));

        // act
        var command = new ProcessEnrichmentCommand(movie.Id, 1);
        var outcome = await handler.HandleAsync(command, ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.Retry, 2),
            EnrichmentFailureCategory.ProviderUnavailable));
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        AssertFailureLogged(command, EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailedResult_RateLimitedCategory_UsesRetryDelayed()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.RateLimited));

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.RetryDelayed, 2),
            EnrichmentFailureCategory.RateLimited));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderFailedResult_NonRetryableCategory_MarksFailedAndDeadLetters()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse));
        var attempts = movie.EnrichmentAttempts;

        // act
        var command = new ProcessEnrichmentCommand(movie.Id, MaxAttempts);
        var outcome = await handler.HandleAsync(command, ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        movie.Status.ShouldBe(EnrichmentStatus.Failed);
        movie.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        movie.EnrichmentAttempts.ShouldBe(attempts);
        await movies.Received(1).SaveChangesAsync(ct);
        AssertFailureLogged(command, EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task HandleAsync_ProviderException_RetryableCategory_ReturnsRetry()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns<Task<MetadataLookupResult>>(
            _ => throw new HttpRequestException("boom"));

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.Retry, 2),
            EnrichmentFailureCategory.ProviderUnavailable));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderException_InvalidResponseCategory_MarksFailedAtTerminalPath()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns<Task<MetadataLookupResult>>(
            _ => throw new System.Text.Json.JsonException("bad"));
        var attempts = movie.EnrichmentAttempts;

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, MaxAttempts), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        movie.Status.ShouldBe(EnrichmentStatus.Failed);
        movie.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        movie.EnrichmentAttempts.ShouldBe(attempts);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_RetryableCategoryAtMaxAttempts_DeadLettersCarryingTheActualCategory()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable));

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, MaxAttempts), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.ProviderUnavailable));
        movie.Status.ShouldBe(EnrichmentStatus.Failed);
        movie.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_CancelledByCaller_IsNotClassifiedAndNotSaved()
    {
        // arrange
        var movie = PendingMovie();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        movies.TryClaimForEnrichmentAsync(movie.Id, token).Returns(true);
        movies.GetAsync(movie.Id, token).Returns(movie);
        provider.FindAsync(Arg.Any<MetadataLookup>(), token).Returns<MetadataLookupResult>(_ =>
            throw new OperationCanceledException(token));
        await cts.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), cts.Token));

        // assert
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderTimeout_IsClassifiedAsProviderUnavailable()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns<Task<MetadataLookupResult>>(
            _ => throw new TimeoutException("slow"));

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.Retry, 2),
            EnrichmentFailureCategory.ProviderUnavailable));
    }

    private void AssertFailureLogged(ProcessEnrichmentCommand command, EnrichmentFailureCategory category)
    {
        logger.Entries.Count.ShouldBe(1);
        var entry = logger.Entries[0];
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Exception.ShouldBeNull();
        StateValue(entry, "{OriginalFormat}").ShouldBe(
            "Enrichment failed for {MovieId} attempt {Attempt} category {Category}");
        StateValue(entry, "MovieId").ShouldBe(command.MovieId.Value);
        StateValue(entry, "Attempt").ShouldBe(command.Attempt);
        StateValue(entry, "Category").ShouldBe(category.Code);
    }

    private static object? StateValue(
        RecordingLogger<ProcessEnrichmentCommandHandler>.LogEntry entry,
        string name) =>
        entry.State.Single(pair => pair.Key == name).Value;

    private void Claimed(Movie movie, bool claimed)
    {
        var ct = CancellationToken.None;
        movies.TryClaimForEnrichmentAsync(movie.Id, ct).Returns(claimed);
        movies.GetAsync(movie.Id, ct).Returns(movie);
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

    private Movie EnrichedMovie(ReleaseYear releaseYear)
    {
        var pending = PendingMovie();
        return Movie.Rehydrate(
            pending.Id,
            pending.Title,
            pending.Path,
            pending.Format,
            false,
            new MovieMetadata("Previously Enriched", releaseYear: releaseYear),
            EnrichmentStatus.Pending,
            null,
            0,
            null,
            null);
    }

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);
}