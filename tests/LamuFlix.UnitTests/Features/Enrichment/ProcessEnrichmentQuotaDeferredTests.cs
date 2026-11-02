using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Enrichment;

public sealed class ProcessEnrichmentQuotaDeferredTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NextUtcMidnight = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private const int MaxAttempts = 3;

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMetadataProvider provider = Substitute.For<IMetadataProvider>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();

    [Fact]
    public async Task HandleAsync_QuotaExhausted_LeavesPendingSetsDeferredUntilSameAttemptNoFailure()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted));

        // act
        var outcome = await Handler().HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.QuotaDeferred(NextUtcMidnight));
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.EnrichmentDeferredUntil.ShouldBe(NextUtcMidnight);
        movie.LastFailureCategory.ShouldBeNull();
        movie.EnrichmentAttempts.ShouldBe(0);
        await movies.Received(1).SaveChangesAsync(ct);
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<FeaturesRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChainFinalQuotaAtOrdinaryRetryLimit_LeavesPendingSameAttemptNoFailureWrite()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted));

        // act
        var outcome = await Handler().HandleAsync(new ProcessEnrichmentCommand(movie.Id, MaxAttempts), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.QuotaDeferred(NextUtcMidnight));
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.LastFailureCategory.ShouldBeNull();
        movie.EnrichmentDeferredUntil.ShouldBe(NextUtcMidnight);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    public static TheoryData<int, ProcessEnrichmentOutcome> RateLimitedOutcomes() =>
        new()
        {
            {
                1,
                new ProcessEnrichmentOutcome.Failed(
                    new EnrichmentFailureDecision(EnrichmentFailureAction.RetryDelayed, 2),
                    EnrichmentFailureCategory.RateLimited)
            },
            {
                MaxAttempts,
                new ProcessEnrichmentOutcome.Failed(
                    new EnrichmentFailureDecision(EnrichmentFailureAction.DeadLetter, null),
                    EnrichmentFailureCategory.RateLimited)
            },
        };

    [Theory]
    [MemberData(nameof(RateLimitedOutcomes))]
    public async Task HandleAsync_Http429_RetainsRateLimitedBoundedRetryAndTerminalFailed(
        int attempt,
        ProcessEnrichmentOutcome expected)
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie);
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct)
            .Returns(new MetadataLookupResult.Failed(EnrichmentFailureCategory.RateLimited));

        // act
        var outcome = await Handler().HandleAsync(new ProcessEnrichmentCommand(movie.Id, attempt), ct);

        // assert
        outcome.ShouldBe(expected);
        movie.EnrichmentDeferredUntil.ShouldBeNull();
        if (attempt == MaxAttempts)
        {
            movie.Status.ShouldBe(EnrichmentStatus.Failed);
            movie.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        }
        else
        {
            movie.Status.ShouldBe(EnrichmentStatus.Pending);
            movie.LastFailureCategory.ShouldBeNull();
        }
    }

    [Fact]
    public void Decide_QuotaDeferred_ReturnsQuotaDeferredRoutingKeyNullNextAttemptAndDeadline()
    {
        // arrange
        var outcome = new ProcessEnrichmentOutcome.QuotaDeferred(NextUtcMidnight);

        // act
        var disposition = EnrichmentRouting.Decide(outcome);

        // assert
        disposition.Action.ShouldBe(EnrichmentRouting.QuotaDeferred);
        disposition.RoutingKey.ShouldBe(RabbitMqTopology.QuotaDeferredRoutingKey);
        disposition.NextAttempt.ShouldBeNull();
        disposition.DeferredUntil.ShouldBe(NextUtcMidnight);
    }

    [Fact]
    public async Task FetchAsync_NonFinalQuotaThenNextProviderFound_CompletesEnrichmentWithZeroDeferral()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie);
        var chain = new ChainedMetadataProvider(
        [
            StubProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted)),
            StubProvider.Returning(new MetadataLookupResult.Found(new MovieMetadata("Found Later"))),
        ]);

        // act
        var outcome = await Handler(chain).HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(Claimed: true));
        movie.Status.ShouldBe(EnrichmentStatus.Enriched);
        movie.EnrichmentDeferredUntil.ShouldBeNull();
        movie.LastFailureCategory.ShouldBeNull();
    }

    [Fact]
    public async Task FetchAsync_ChainFinalQuota_DefersWithQuotaDeferred()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie);
        var chain = new ChainedMetadataProvider(
        [
            StubProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted)),
            StubProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted)),
        ]);

        // act
        var outcome = await Handler(chain).HandleAsync(new ProcessEnrichmentCommand(movie.Id, 1), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.QuotaDeferred(NextUtcMidnight));
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.EnrichmentDeferredUntil.ShouldBe(NextUtcMidnight);
        movie.LastFailureCategory.ShouldBeNull();
    }

    [Fact]
    public async Task FetchAsync_ChainFinalQuotaAtOrdinaryRetryLimit_DefersSameAttemptWithoutFailureWrite()
    {
        // arrange
        var movie = PendingMovie();
        var ct = CancellationToken.None;
        Claimed(movie);
        var chain = new ChainedMetadataProvider(
        [
            StubProvider.Returning(new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted)),
        ]);

        // act
        var outcome = await Handler(chain).HandleAsync(new ProcessEnrichmentCommand(movie.Id, MaxAttempts), ct);

        // assert
        outcome.ShouldBe(new ProcessEnrichmentOutcome.QuotaDeferred(NextUtcMidnight));
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.LastFailureCategory.ShouldBeNull();
        await movies.Received(1).SaveChangesAsync(ct);
    }

    private ProcessEnrichmentCommandHandler Handler(IMetadataProvider? metadata = null) =>
        new(
            movies,
            metadata ?? provider,
            queue,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = MaxAttempts }),
            new Features.FixedTimeProvider(Now),
            NullLogger<ProcessEnrichmentCommandHandler>.Instance);

    private void Claimed(Movie movie)
    {
        var ct = CancellationToken.None;
        movies.TryClaimForEnrichmentAsync(movie.Id, ct).Returns(true);
        movies.GetAsync(movie.Id, ct).Returns(movie);
    }

    private static Movie PendingMovie() =>
        Movie.Create(new MovieId(1), "Deferred", new LibraryPath("C:/library/deferred"), new MediaFormat("mkv"));

    private sealed class StubProvider(MetadataLookupResult result) : IMetadataProvider
    {
        public static StubProvider Returning(MetadataLookupResult result) => new(result);

        public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct) =>
            Task.FromResult(result);
    }
}
