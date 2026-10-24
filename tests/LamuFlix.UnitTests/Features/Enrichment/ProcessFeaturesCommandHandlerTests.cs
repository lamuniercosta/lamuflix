using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ProcessFeaturesCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
    private static readonly MovieId Id = new(7);

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMovieFeaturesRepository features = Substitute.For<IMovieFeaturesRepository>();
    private readonly RecordingLogger<ProcessFeaturesCommandHandler> logger = new();

    [Fact]
    public async Task HandleAsync_NonblankPlot_GeneratesOneVectorAndUpsertsSnapshotFromClock()
    {
        // arrange
        const string plot = "  A mixed\tplot\nwith \u00A0 text  ";
        var ct = CancellationToken.None;
        var fake = new FakeEmbeddingGenerator();
        var generator = FromFake(fake);
        movies.GetPlotAsync(Id, ct).Returns(plot);
        MovieFeaturesSnapshot? upserted = null;
        features.UpsertAsync(Arg.Do<MovieFeaturesSnapshot>(snapshot => upserted = snapshot), ct)
            .Returns(Task.CompletedTask);

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Completed(Generated: true));
        generator.CallCount.ShouldBe(1);
        generator.LastInputs.ShouldBe([plot]);
        upserted.ShouldNotBeNull();
        upserted.MovieId.ShouldBe(Id);
        upserted.ComputedAt.ShouldBe(Now);
        var expected = (await fake.GenerateAsync([plot], cancellationToken: ct))[0].Vector.ToArray();
        upserted.Vector.ToArray().ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_NonblankPlot_LeavesEnrichmentStateUntouched()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");

        // act
        await CreateHandler(FromFake()).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        await movies.DidNotReceive().TryClaimForEnrichmentAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_CompletesAsNoInputWithoutGeneratingOrUpserting()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns((string?)null);
        var generator = FromFake();

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Completed(Generated: false));
        generator.CallCount.ShouldBe(0);
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    [InlineData("\u2003\u00A0\f\v")]
    [InlineData("\u3000")]
    public async Task HandleAsync_WhitespaceOnlyPlot_CompletesAsNoInputWithoutGeneratingOrUpserting(string plot)
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns(plot);
        var generator = FromFake();

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Completed(Generated: false));
        generator.CallCount.ShouldBe(0);
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExistingFeaturesRow_StillGeneratesAndUpserts()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        features.GetByMovieIdAsync(Id, ct).Returns(ExistingSnapshot());
        var generator = FromFake();

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Completed(Generated: true));
        generator.CallCount.ShouldBe(1);
        await features.Received(1).UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), ct);
    }

    [Fact]
    public async Task HandleAsync_NonblankPlot_LogsMovieIdAttemptAndCompletionWithoutPlotOrVector()
    {
        // arrange
        const string plot = "A very secret plot that must never be logged.";
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns(plot);

        // act
        await CreateHandler(FromFake()).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Exception.ShouldBeNull();
        StateValue(entry, "{OriginalFormat}").ShouldBe(
            "Feature processing for {MovieId} attempt {Attempt} completed generated");
        StateValue(entry, "MovieId").ShouldBe(Id.Value);
        StateValue(entry, "Attempt").ShouldBe(1);
        AssertSafeLog(entry, plot);
    }

    [Fact]
    public async Task HandleAsync_WhitespacePlot_LogsNoInputOutcomeWithoutPlot()
    {
        // arrange
        const string plot = "\t \u2003 ";
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns(plot);

        // act
        await CreateHandler(FromFake()).HandleAsync(new ProcessFeaturesCommand(Id, 2), ct);

        // assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Exception.ShouldBeNull();
        StateValue(entry, "{OriginalFormat}").ShouldBe(
            "Feature processing for {MovieId} attempt {Attempt} completed with no input");
        StateValue(entry, "MovieId").ShouldBe(Id.Value);
        StateValue(entry, "Attempt").ShouldBe(2);
        AssertSafeLog(entry, plot);
    }

    private ProcessFeaturesCommandHandler CreateHandler(
        IEmbeddingGenerator<string, Embedding<float>> generator) =>
        new(
            movies,
            features,
            generator,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = 3 }),
            new FixedTimeProvider(Now),
            logger);

    private static RecordingEmbeddingGenerator FromFake(FakeEmbeddingGenerator? fake = null)
    {
        var inner = fake ?? new FakeEmbeddingGenerator();
        return new(async (inputs, ct) =>
            await inner.GenerateAsync(inputs, cancellationToken: ct));
    }

    private static MovieFeaturesSnapshot ExistingSnapshot() =>
        new(
            Id,
            ImmutableArray.CreateRange(Enumerable.Repeat(0.5f, EmbeddingDimensions.Value)),
            Now.AddDays(-1));

    private static object? StateValue(
        RecordingLogger<ProcessFeaturesCommandHandler>.LogEntry entry,
        string name) =>
        entry.State.Single(pair => pair.Key == name).Value;

    private static void AssertSafeLog(
        RecordingLogger<ProcessFeaturesCommandHandler>.LogEntry entry,
        string plot)
    {
        foreach (var pair in entry.State)
        {
            var rendered = pair.Value?.ToString() ?? string.Empty;
            rendered.ShouldNotContain(plot);
            pair.Value.ShouldNotBeAssignableTo<IEnumerable<float>>();
        }
    }
}
