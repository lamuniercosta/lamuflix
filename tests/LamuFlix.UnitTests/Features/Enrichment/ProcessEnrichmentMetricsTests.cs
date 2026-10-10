using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.UnitTests.Pipeline;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Enrichment;

[Collection(nameof(MetricsCollection))]
public sealed class ProcessEnrichmentMetricsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private const int MaxAttempts = 3;
    private const string MeterName = "LamuFlix";
    private const string OutcomeInstrumentName = "lamuflix.enrichment.outcome";
    private const string OutcomeTagKey = "outcome";
    private const string FailureCategoryTagKey = "failure_category";
    private const string EnrichedOutcomeValue = "enriched";
    private const string NotFoundOutcomeValue = "not_found";
    private const string FailedOutcomeValue = "failed";
    private const string ProviderUnavailableCode = "provider_unavailable";
    private const string RateLimitedCode = "rate_limited";
    private const string InvalidResponseCode = "invalid_response";
    private const string UnknownCode = "unknown";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMetadataProvider provider = Substitute.For<IMetadataProvider>();
    private readonly RecordingLogger<ProcessEnrichmentCommandHandler> logger = new();
    private readonly ProcessEnrichmentCommandHandler handler;
    private readonly Fixture fixture = new();

    public ProcessEnrichmentMetricsTests()
    {
        handler = new ProcessEnrichmentCommandHandler(
            movies,
            provider,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = MaxAttempts }),
            new FixedTimeProvider(Now),
            logger);
    }

    public static TheoryData<EnrichmentFailureCategory, string> FailureCategories() =>
        new()
        {
            { EnrichmentFailureCategory.ProviderUnavailable, ProviderUnavailableCode },
            { EnrichmentFailureCategory.RateLimited, RateLimitedCode },
            { EnrichmentFailureCategory.InvalidResponse, InvalidResponseCode },
            { EnrichmentFailureCategory.Unknown, UnknownCode },
        };

    [Fact]
    public async Task HandleAsync_Found_RecordsOneEnrichedOutcomeWithoutFailureCategory()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Found(new MovieMetadata("Enriched Title")));
        using var capture = new OutcomeCapture();

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(true));
        AssertSingleOutcome(capture, EnrichedOutcomeValue, failureCategory: null);
    }

    [Fact]
    public async Task HandleAsync_NotFound_RecordsOneNotFoundOutcomeWithoutFailureCategory()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns(new MetadataLookupResult.NotFound());
        using var capture = new OutcomeCapture();

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(true));
        AssertSingleOutcome(capture, NotFoundOutcomeValue, failureCategory: null);
    }

    [Theory]
    [MemberData(nameof(FailureCategories))]
    public async Task HandleAsync_ProviderFailedResult_RecordsOneFailedOutcomeWithTheActualCategoryCode(
        EnrichmentFailureCategory category,
        string expectedCode)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(category));
        using var capture = new OutcomeCapture();

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBeOfType<ProcessEnrichmentOutcome.Failed>().Category.ShouldBe(category);
        AssertSingleOutcome(capture, FailedOutcomeValue, expectedCode);
    }

    [Theory]
    [MemberData(nameof(FailureCategories))]
    public async Task HandleAsync_AtMaxAttempts_StillRecordsTheTerminalOutcomeOnceWithTheActualCategoryCode(
        EnrichmentFailureCategory category,
        string expectedCode)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(category));
        using var capture = new OutcomeCapture();

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, MaxAttempts), ct);

        // assert
        outcome.ShouldBeOfType<ProcessEnrichmentOutcome.Failed>()
            .Decision.Action.ShouldBe(EnrichmentFailureAction.DeadLetter);
        AssertSingleOutcome(capture, FailedOutcomeValue, expectedCode);
    }

    [Fact]
    public async Task HandleAsync_LookupException_RecordsOneFailedOutcomeWithTheClassifiedCategory()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns<Task<MetadataLookupResult>>(
            _ => throw new HttpRequestException("boom"));
        using var capture = new OutcomeCapture();

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBeOfType<ProcessEnrichmentOutcome.Failed>()
            .Category.Code.ShouldBe(ProviderUnavailableCode);
        AssertSingleOutcome(capture, FailedOutcomeValue, ProviderUnavailableCode);
    }

    [Fact]
    public async Task HandleAsync_RepeatedRetryAttempts_RecordsOneOutcomePerAttempt()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable));
        using var capture = new OutcomeCapture();

        // act
        var first = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);
        var second = await handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 2), ct);

        // assert
        first.ShouldBeOfType<ProcessEnrichmentOutcome.Failed>();
        second.ShouldBeOfType<ProcessEnrichmentOutcome.Failed>();
        capture.Measurements.Count.ShouldBe(2);
        capture.Measurements.ShouldAllBe(measurement => measurement.Value == 1L);
        capture.Measurements.ShouldAllBe(
            measurement => measurement.Tags.Length == 2
                           && Equals(measurement.Tags[0].Key, OutcomeTagKey)
                           && Equals(measurement.Tags[0].Value, FailedOutcomeValue)
                           && Equals(measurement.Tags[1].Key, FailureCategoryTagKey)
                           && Equals(measurement.Tags[1].Value, ProviderUnavailableCode));
    }

    [Fact]
    public async Task HandleAsync_RefusedClaim_RecordsNoOutcomeMeasurement()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.TryClaimForEnrichmentAsync(id, ct).Returns(false);
        using var capture = new OutcomeCapture();

        // act
        var outcome = await handler.HandleAsync(new ProcessEnrichmentCommand(id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(false));
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_RequestedCancellation_RecordsNoOutcomeMeasurement()
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
        using var capture = new OutcomeCapture();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), cts.Token));

        // assert
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PersistenceFailureAfterClassification_RecordsTheClassifiedOutcomeOnce()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Found(new MovieMetadata("Enriched Title")));
        movies.SaveChangesAsync(ct).ThrowsAsync(new InvalidOperationException("save"));
        using var capture = new OutcomeCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct));

        // assert
        AssertSingleOutcome(capture, EnrichedOutcomeValue, failureCategory: null);
    }

    [Fact]
    public async Task HandleAsync_SettlementPersistenceFailure_RecordsNoAdditionalOutcomeMeasurement()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie, true);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse));
        movies.SaveChangesAsync(ct).ThrowsAsync(new InvalidOperationException("settlement"));
        using var capture = new OutcomeCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ProcessEnrichmentCommand(movie.Id, MaxAttempts), ct));

        // assert
        AssertSingleOutcome(capture, FailedOutcomeValue, InvalidResponseCode);
    }

    private static void AssertSingleOutcome(
        OutcomeCapture capture,
        string expectedOutcome,
        string? failureCategory)
    {
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(OutcomeInstrumentName);
        measured.Value.ShouldBe(1L);
        if (failureCategory is null)
        {
            measured.Tags.Length.ShouldBe(1);
            measured.Tags[0].Key.ShouldBe(OutcomeTagKey);
            measured.Tags[0].Value.ShouldBe(expectedOutcome);
            return;
        }

        measured.Tags.Length.ShouldBe(2);
        measured.Tags[0].Key.ShouldBe(OutcomeTagKey);
        measured.Tags[0].Value.ShouldBe(expectedOutcome);
        measured.Tags[1].Key.ShouldBe(FailureCategoryTagKey);
        measured.Tags[1].Value.ShouldBe(failureCategory);
    }

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

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);

    private sealed class OutcomeCapture : IDisposable
    {
        private readonly ConcurrentQueue<CapturedOutcome> measurements = [];
        private readonly MeterListener listener = new();

        public OutcomeCapture()
        {
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument.Name, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, publishedListener) =>
            {
                if (string.Equals(instrument.Meter.Name, MeterName, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, OutcomeInstrumentName, StringComparison.Ordinal))
                {
                    publishedListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();
        }

        public IReadOnlyList<CapturedOutcome> Measurements => [.. measurements];

        public void Dispose() => listener.Dispose();
    }

    private sealed record CapturedOutcome(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags);
}
