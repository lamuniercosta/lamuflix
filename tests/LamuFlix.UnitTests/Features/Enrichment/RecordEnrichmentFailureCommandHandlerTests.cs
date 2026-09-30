using System;
using System.Collections.Generic;
using System.Linq;
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

public sealed class RecordEnrichmentFailureCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private const int MaxAttempts = 3;

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly RecordingLogger<RecordEnrichmentFailureCommandHandler> logger = new();
    private readonly RecordEnrichmentFailureCommandHandler handler;
    private readonly Fixture fixture = new();

    public RecordEnrichmentFailureCommandHandlerTests()
    {
        handler = new RecordEnrichmentFailureCommandHandler(
            movies,
            new FixedTimeProvider(Now),
            new EnrichmentOptions { MaxAttempts = MaxAttempts },
            logger);
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task HandleAsync_CategoryAndAttempt_ProducesExpectedDecision(
        EnrichmentFailureCategory category,
        int attempt,
        EnrichmentFailureAction expectedAction,
        int? expectedNext)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        movies.GetAsync(movie.Id, ct).Returns(movie);
        var command = new RecordEnrichmentFailureCommand(movie.Id, attempt, category);

        // act
        var decision = await handler.HandleAsync(command, ct);

        // assert
        decision.Action.ShouldBe(expectedAction);
        decision.NextAttempt.ShouldBe(expectedNext);
        AssertLog(command, expectedAction);
        if (expectedAction == EnrichmentFailureAction.DeadLetter)
        {
            movie.Status.ShouldBe(EnrichmentStatus.Failed);
            movie.LastFailureCategory.ShouldBe(category);
            await movies.Received(1).GetAsync(movie.Id, ct);
            await movies.Received(1).SaveChangesAsync(ct);
        }
        else
        {
            expectedNext.ShouldBe(attempt + 1);
            await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
            await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task HandleAsync_DeadLetterMissingMovie_ThrowsNotFound()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.GetAsync(id, ct).Returns((Movie?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(
                new RecordEnrichmentFailureCommand(id, MaxAttempts, EnrichmentFailureCategory.Unknown),
                ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    public static TheoryData<EnrichmentFailureCategory, int, EnrichmentFailureAction, int?> Matrix()
    {
        var data = new TheoryData<EnrichmentFailureCategory, int, EnrichmentFailureAction, int?>();
        foreach (var category in Categories())
        {
            AddRow(data, category, attempt: 1);
            AddRow(data, category, attempt: MaxAttempts);
        }

        return data;
    }

    private static void AddRow(
        TheoryData<EnrichmentFailureCategory, int, EnrichmentFailureAction, int?> data,
        EnrichmentFailureCategory category,
        int attempt)
    {
        var retry = category.IsRetryable && attempt < MaxAttempts;
        if (!retry)
        {
            data.Add(category, attempt, EnrichmentFailureAction.DeadLetter, null);
            return;
        }

        var action = category == EnrichmentFailureCategory.RateLimited
            ? EnrichmentFailureAction.RetryDelayed
            : EnrichmentFailureAction.Retry;
        data.Add(category, attempt, action, attempt + 1);
    }

    private static IEnumerable<EnrichmentFailureCategory> Categories() =>
    [
        EnrichmentFailureCategory.ProviderUnavailable,
        EnrichmentFailureCategory.RateLimited,
        EnrichmentFailureCategory.InvalidResponse,
        EnrichmentFailureCategory.Unknown,
    ];

    private void AssertLog(RecordEnrichmentFailureCommand command, EnrichmentFailureAction action)
    {
        logger.Entries.Count.ShouldBe(1);
        var entry = logger.Entries[0];
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Exception.ShouldBeNull();
        StateValue(entry, "MovieId").ShouldBe(command.Id.Value);
        StateValue(entry, "Attempt").ShouldBe(command.Attempt);
        StateValue(entry, "Category").ShouldBe(command.Category.Code);
        StateValue(entry, "Action").ShouldBe(action.Name);
    }

    private static object? StateValue(RecordingLogger<RecordEnrichmentFailureCommandHandler>.LogEntry entry, string name) =>
        entry.State.Single(pair => pair.Key == name).Value;

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
