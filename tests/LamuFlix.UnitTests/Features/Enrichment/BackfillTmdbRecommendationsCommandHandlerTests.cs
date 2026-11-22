using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Ports;
using LamuFlix.UnitTests.Features;
using NSubstitute;
using Xunit;

namespace LamuFlix.UnitTests.Features.Enrichment;

/// <summary>
/// Pins the operator backfill contract: eligibility and skip rules, one identity lookup per eligible movie,
/// atomic persistence only on a complete match, truthful five counts, and stop behaviour on breaker
/// rejection or cancellation.
/// </summary>
public sealed class BackfillTmdbRecommendationsCommandHandlerTests
{
    private const string ImdbIdValue = "tt0069293";

    private static int nextId;

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMetadataProvider provider = Substitute.For<IMetadataProvider>();
    private readonly RecordingLogger<BackfillTmdbRecommendationsCommandHandler> logger = new();
    private readonly BackfillTmdbRecommendationsCommandHandler handler;

    public BackfillTmdbRecommendationsCommandHandlerTests()
    {
        handler = new BackfillTmdbRecommendationsCommandHandler(movies, provider, logger);
    }

    [Fact]
    public async Task HandleAsync_MovieWithoutImdbId_SkipsWithoutLookupOrSave()
    {
        // arrange
        var movie = Movie.Create(NewId(), "No Identity", new LibraryPath("C:/library/a"), new MediaFormat("mkv"));
        Snapshot(movie);

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(
            ImdbEligible: 0,
            Matched: 0,
            Unmatched: 0,
            Skipped: 1,
            Failures: 0));
        result.Stopped.ShouldBeFalse();
        await provider.DidNotReceive().FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StoredTmdbId_SkipsWithoutLookup()
    {
        // arrange
        var movie = EnrichedMovie(tmdbId: 603, recommended: [604]);
        Snapshot(movie);

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(1, 0, 0, 1, 0));
        await provider.DidNotReceive().FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>());
        movie.TmdbId.ShouldBe(603);
    }

    [Fact]
    public async Task HandleAsync_Matched_CommitsIdentityAndPairsAndCarriesTheStoredImdbId()
    {
        // arrange
        var ct = CancellationToken.None;
        var movie = EnrichedMovie(tmdbId: null);
        Snapshot(movie);
        MetadataLookup? lookup = null;
        provider.FindAsync(Arg.Any<MetadataLookup>(), ct).Returns(call =>
        {
            lookup = call.Arg<MetadataLookup>();
            return new MetadataLookupResult.Found(MetadataWithRecommendations(603, [604, 605]));
        });

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), ct);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(1, 1, 0, 0, 0));
        lookup.ShouldNotBeNull();
        lookup.ManualImdbId.ShouldBe(new ImdbId(ImdbIdValue));
        movie.TmdbId.ShouldBe(603);
        movie.RecommendedTmdbIds.ShouldBe([604, 605]);
        movie.Status.ShouldBe(EnrichmentStatus.Enriched);
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulEmptyFirstPage_CommitsTheIdentityWithNoPairs()
    {
        // arrange
        var movie = EnrichedMovie(tmdbId: null);
        Snapshot(movie);
        Found(MetadataWithRecommendations(603, []));

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(1, 1, 0, 0, 0));
        movie.TmdbId.ShouldBe(603);
        movie.RecommendedTmdbIds.ShouldBeEmpty();
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FailedFirstPage_LeavesTheIdentityNullAndCountsAFailure()
    {
        // arrange
        var movie = EnrichedMovie(tmdbId: null);
        Snapshot(movie);
        Found(new MovieMetadata("Solaris", imdbId: new ImdbId(ImdbIdValue), tmdbId: 603));

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(1, 0, 0, 0, 1));
        result.Stopped.ShouldBeFalse();
        movie.TmdbId.ShouldBeNull();
        movie.RecommendedTmdbIds.ShouldBeNull();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Unmatched_LeavesTheIdentityNullAndIsNotAFailure()
    {
        // arrange
        var movie = EnrichedMovie(tmdbId: null);
        Snapshot(movie);
        provider.FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>())
            .Returns(new MetadataLookupResult.NotFound());

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(1, 0, 1, 0, 0));
        movie.TmdbId.ShouldBeNull();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecoverableFailure_CountsItAndKeepsProcessing()
    {
        // arrange
        var failing = EnrichedMovie(tmdbId: null);
        var matching = EnrichedMovie(tmdbId: null);
        Snapshot(failing, matching);
        provider.FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse),
                _ => new MetadataLookupResult.Found(MetadataWithRecommendations(803, [804])));

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(2, 1, 0, 0, 1));
        result.Stopped.ShouldBeFalse();
        matching.TmdbId.ShouldBe(803);
        failing.TmdbId.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ProviderUnavailable_StopsWithPartialCountsAndKeepsCompletedCommits()
    {
        // arrange
        var matching = EnrichedMovie(tmdbId: null);
        var stopped = EnrichedMovie(tmdbId: null);
        var untouched = EnrichedMovie(tmdbId: null);
        Snapshot(matching, stopped, untouched);
        provider.FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => new MetadataLookupResult.Found(MetadataWithRecommendations(603, [604])),
                _ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable));

        // act
        var result = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(2, 1, 0, 0, 1, BackfillStopReason.ProviderUnavailable));
        result.Stopped.ShouldBeTrue();
        matching.TmdbId.ShouldBe(603);
        stopped.TmdbId.ShouldBeNull();
        untouched.TmdbId.ShouldBeNull();
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await provider.Received(2).FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyCancelled_StopsBeforeReadingTheLibrary()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // act
        var result = await handler.HandleAsync(
            new BackfillTmdbRecommendationsCommand(),
            cancellation.Token);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(0, 0, 0, 0, 0, BackfillStopReason.Cancelled));
        await movies.DidNotReceive().SnapshotMovieIdsAsync(Arg.Any<CancellationToken>());
        await provider.DidNotReceive().FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledDuringALookup_StopsWithTheCountsReachedSoFar()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var matching = EnrichedMovie(tmdbId: null);
        var pending = EnrichedMovie(tmdbId: null);
        Snapshot(matching, pending);
        provider.FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => new MetadataLookupResult.Found(MetadataWithRecommendations(603, [604])),
                _ =>
                {
                    cancellation.Cancel();
                    throw new OperationCanceledException(cancellation.Token);
                });

        // act
        var result = await handler.HandleAsync(
            new BackfillTmdbRecommendationsCommand(),
            cancellation.Token);

        // assert
        result.ShouldBe(new BackfillTmdbRecommendationsResult(2, 1, 0, 0, 0, BackfillStopReason.Cancelled));
        matching.TmdbId.ShouldBe(603);
        pending.TmdbId.ShouldBeNull();
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Rerun_SkipsTheRowCommittedByTheFirstRun()
    {
        // arrange
        var movie = EnrichedMovie(tmdbId: null);
        Snapshot(movie);
        Found(MetadataWithRecommendations(603, [604]));

        // act
        var first = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);
        var second = await handler.HandleAsync(new BackfillTmdbRecommendationsCommand(), CancellationToken.None);

        // assert
        first.ShouldBe(new BackfillTmdbRecommendationsResult(1, 1, 0, 0, 0));
        second.ShouldBe(new BackfillTmdbRecommendationsResult(1, 0, 0, 1, 0));
        await provider.Received(1).FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>());
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void Snapshot(params Movie[] seeded)
    {
        var byId = seeded.ToDictionary(movie => movie.Id);
        movies.SnapshotMovieIdsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => [.. seeded.Select(movie => movie.Id)]);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(call => byId.GetValueOrDefault(call.Arg<MovieId>()));
    }

    private void Found(MovieMetadata metadata) =>
        provider.FindAsync(Arg.Any<MetadataLookup>(), Arg.Any<CancellationToken>())
            .Returns(new MetadataLookupResult.Found(metadata));

    private static MovieMetadata MetadataWithRecommendations(int tmdbId, IReadOnlyList<int> recommended) =>
        new(
            "Solaris",
            imdbId: new ImdbId(ImdbIdValue),
            tmdbId: tmdbId,
            recommendedTmdbIds: recommended);

    private static Movie EnrichedMovie(int? tmdbId, IReadOnlyList<int>? recommended = null) =>
        Movie.Rehydrate(
            NewId(),
            "Solaris",
            new LibraryPath($"C:/library/{Guid.NewGuid():N}"),
            new MediaFormat("mkv"),
            false,
            new MovieMetadata("Solaris", imdbId: new ImdbId(ImdbIdValue)),
            EnrichmentStatus.Enriched,
            null,
            0,
            null,
            null,
            tmdbId: tmdbId,
            recommendedTmdbIds: recommended);

    private static MovieId NewId() => new(Interlocked.Increment(ref nextId));
}
