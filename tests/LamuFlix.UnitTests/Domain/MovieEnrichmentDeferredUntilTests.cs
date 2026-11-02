using Xunit;
using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieEnrichmentDeferredUntilTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Midnight = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DeferEnrichmentUntil_FromPending_SetsDeadlineLeavesPendingNoFailure()
    {
        // arrange
        var movie = NewMovie();

        // act
        movie.DeferEnrichmentUntil(Midnight);

        // assert
        movie.EnrichmentDeferredUntil.ShouldBe(Midnight);
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.LastFailureCategory.ShouldBeNull();
        movie.EnrichmentAttempts.ShouldBe(0);
        movie.LastAttemptAt.ShouldBeNull();
    }

    public static TheoryData<EnrichmentStatus> NonPendingStatuses() =>
        new() { EnrichmentStatus.Enriched, EnrichmentStatus.NotFound, EnrichmentStatus.Failed };

    [Theory]
    [MemberData(nameof(NonPendingStatuses))]
    public void DeferEnrichmentUntil_FromNonPending_ThrowsInvalidTransitionLeavingStateUnchanged(
        EnrichmentStatus status)
    {
        // arrange
        var movie = Rehydrated(status);

        // act
        var thrown = Should.Throw<InvalidTransitionException>(() => movie.DeferEnrichmentUntil(Midnight));

        // assert
        thrown.Action.ShouldBe("DeferEnrichmentUntil");
        thrown.State.ShouldBe(status.ToString());
        movie.EnrichmentDeferredUntil.ShouldBeNull();
        movie.Status.ShouldBe(status);
    }

    [Fact]
    public void Rehydrate_PersistedDeadline_RestoresIt()
    {
        // arrange
        var expected = new MovieId(7);

        // act
        var movie = Movie.Rehydrate(
            expected,
            "Deferred",
            new LibraryPath("C:/library/deferred"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Pending,
            null,
            2,
            null,
            Now,
            enrichmentDeferredUntil: Midnight);

        // assert
        movie.EnrichmentDeferredUntil.ShouldBe(Midnight);
        movie.LastAttemptAt.ShouldBe(Now);
    }

    private static Movie NewMovie() =>
        Movie.Create(new MovieId(1), "Deferred", new LibraryPath("C:/library/deferred"), new MediaFormat("mkv"));

    private static Movie Rehydrated(EnrichmentStatus status) =>
        Movie.Rehydrate(
            new MovieId(2),
            "Deferred",
            new LibraryPath("C:/library/deferred"),
            new MediaFormat("mkv"),
            false,
            null,
            status,
            status == EnrichmentStatus.Enriched ? Now : null,
            1,
            status == EnrichmentStatus.Failed ? EnrichmentFailureCategory.Unknown : null,
            Now);
}
