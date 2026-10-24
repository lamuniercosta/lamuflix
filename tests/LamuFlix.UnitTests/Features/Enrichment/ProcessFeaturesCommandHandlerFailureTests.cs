using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ProcessFeaturesCommandHandlerFailureTests
{
    private const int MaxAttempts = 3;
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly MovieId Id = new(11);

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMovieFeaturesRepository features = Substitute.For<IMovieFeaturesRepository>();
    private readonly RecordingLogger<ProcessFeaturesCommandHandler> logger = new();

    [Fact]
    public async Task HandleAsync_TypedEmbeddingCategory_WinsOverTheExceptionTree()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => throw new EmbeddingGenerationException(
            EnrichmentFailureCategory.InvalidResponse,
            "typed failure",
            new HttpRequestException("transport failure", inner: null, HttpStatusCode.ServiceUnavailable)));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RateLimitedEmbeddingCategory_RetriesDelayedBelowMaxAttempts()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => throw new EmbeddingGenerationException(
            EnrichmentFailureCategory.RateLimited));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.RetryDelayed, 2),
            EnrichmentFailureCategory.RateLimited));
    }

    [Fact]
    public async Task HandleAsync_UnmatchedException_UsesClassifierUnknownCategory()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => throw new InvalidOperationException("unmatched"));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.Retry, 2),
            EnrichmentFailureCategory.Unknown));
    }

    [Fact]
    public async Task HandleAsync_PersistenceShapedException_UsesClassifierProviderUnavailable()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        features.UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), ct)
            .ThrowsAsync(new IOException("disk unavailable"));

        // act
        var outcome = await CreateHandler(Generator((values, _) => Generated(values)))
            .HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.Retry, 2),
            EnrichmentFailureCategory.ProviderUnavailable));
    }

    [Fact]
    public async Task HandleAsync_WrongDimensionVector_DeadLettersAsInvalidResponseWithoutUpsert()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => One([1f, 2f, 3f]));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        generator.CallCount.ShouldBe(1);
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonFiniteVector_DeadLettersAsInvalidResponseWithoutUpsert()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var vector = Enumerable.Repeat(float.NaN, EmbeddingDimensions.Value).ToArray();
        var generator = Generator((_, _) => One(vector));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GeneratorReturnsNoEmbeddings_DeadLettersAsInvalidResponse()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => new GeneratedEmbeddings<Embedding<float>>([]));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task HandleAsync_AttemptBelowOne_DeadLettersAsInvalidResponseWithoutLoadingPlot(int attempt)
    {
        // arrange
        var ct = CancellationToken.None;
        var generator = Generator((values, _) => Generated(values));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, attempt), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        await movies.DidNotReceive().GetPlotAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        generator.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_NullMovieId_DeadLettersAsInvalidResponseWithoutLoadingPlot()
    {
        // arrange
        var ct = CancellationToken.None;
        var generator = Generator((values, _) => Generated(values));

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - a malformed payload can carry no movie id.
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(null!, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.InvalidResponse));
        await movies.DidNotReceive().GetPlotAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        generator.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task HandleAsync_RetryableFailureBelowMaxAttempts_RetriesWithNextAttempt(
        int attempt,
        int nextAttempt)
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => throw new HttpRequestException("transient"));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, attempt), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.Retry, nextAttempt),
            EnrichmentFailureCategory.ProviderUnavailable));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(int.MaxValue)]
    public async Task HandleAsync_RetryableFailureAtOrAboveMaxAttempts_DeadLetters(int attempt)
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => throw new HttpRequestException("transient"));

        // act
        var outcome = await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, attempt), ct);

        // assert
        outcome.ShouldBe(new ProcessFeaturesOutcome.Failed(
            new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
            EnrichmentFailureCategory.ProviderUnavailable));
    }

    [Fact]
    public async Task HandleAsync_RequestedCancellation_RequeuesWithoutClassificationOrFailureLog()
    {
        // arrange
        using var source = new CancellationTokenSource();
        var token = source.Token;
        movies.GetPlotAsync(Id, token).Returns("A stored plot.");
        // ReSharper disable once AccessToDisposedClosure - the generator runs inside HandleAsync before the test disposes the source.
        var generator = Generator((_, _) =>
        {
            source.Cancel();
            throw new OperationCanceledException(token);
        });

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), token));

        // assert
        await features.DidNotReceive().UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), Arg.Any<CancellationToken>());
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_DuplicateImdbIdException_NeverProducesTheDuplicateCategory()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        features.UpsertAsync(Arg.Any<MovieFeaturesSnapshot>(), ct)
            .ThrowsAsync(new DuplicateImdbIdException());

        // act
        var outcome = await CreateHandler(Generator((values, _) => Generated(values)))
            .HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        var failed = outcome.ShouldBeOfType<ProcessFeaturesOutcome.Failed>();
        failed.Category.ShouldNotBe(EnrichmentFailureCategory.DuplicateImdbId);
        failed.Category.ShouldBe(EnrichmentFailureCategory.Unknown);
    }

    [Fact]
    public async Task HandleAsync_GeneratorFailure_LogsSafeCategoryWithoutRawExceptionText()
    {
        // arrange
        const string secret = "raw provider secret text";
        var ct = CancellationToken.None;
        movies.GetPlotAsync(Id, ct).Returns("A stored plot.");
        var generator = Generator((_, _) => throw new InvalidOperationException(secret));

        // act
        await CreateHandler(generator).HandleAsync(new ProcessFeaturesCommand(Id, 1), ct);

        // assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Exception.ShouldBeNull();
        StateValue(entry, "{OriginalFormat}").ShouldBe(
            "Feature processing for {MovieId} attempt {Attempt} failed category {Category}");
        StateValue(entry, "MovieId").ShouldBe(Id.Value);
        StateValue(entry, "Attempt").ShouldBe(1);
        StateValue(entry, "Category").ShouldBe(EnrichmentFailureCategory.Unknown.Code);
        foreach (var pair in entry.State)
        {
            (pair.Value?.ToString() ?? string.Empty).ShouldNotContain(secret);
        }
    }

    private ProcessFeaturesCommandHandler CreateHandler(
        IEmbeddingGenerator<string, Embedding<float>> generator) =>
        new(
            movies,
            features,
            generator,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = MaxAttempts }),
            new FixedTimeProvider(Now),
            logger);

    private static RecordingEmbeddingGenerator Generator(
        Func<IReadOnlyList<string>, CancellationToken, GeneratedEmbeddings<Embedding<float>>> generate) =>
        new((inputs, ct) => Task.FromResult(generate(inputs, ct)));

    private static GeneratedEmbeddings<Embedding<float>> Generated(IReadOnlyList<string> inputs) =>
        new(inputs.Select(input => new Embedding<float>(
            Enumerable.Repeat(input.Length + 1f, EmbeddingDimensions.Value).ToArray())));

    private static GeneratedEmbeddings<Embedding<float>> One(float[] vector) =>
        new([new Embedding<float>(vector)]);

    private static object? StateValue(
        RecordingLogger<ProcessFeaturesCommandHandler>.LogEntry entry,
        string name) =>
        entry.State.Single(pair => pair.Key == name).Value;
}
