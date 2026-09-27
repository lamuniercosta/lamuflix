using System;
using LamuFlix.Core.Domain;
using Shouldly;
using Xunit;

namespace LamuFlix.Test.Domain;

public sealed class MovieTests
{
    private static readonly DateTimeOffset ArrivedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ActedAt = new(2020, 2, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly MovieMetadata SeedMetadata = new("Seed");
    private static readonly MovieMetadata ActMetadata = new("Act");

    [Fact]
    public void Create_InitializesPendingWithNoHistory()
    {
        var id = new MovieId(7);
        var path = new LibraryPath(@"C:\lib\a.mkv");
        var format = new MediaFormat(".MKV");

        var movie = Movie.Create(id, "Seven", path, format);

        movie.Id.ShouldBe(id);
        movie.Title.ShouldBe("Seven");
        movie.Path.ShouldBe(path);
        movie.Format.Extension.ShouldBe("mkv");
        movie.IsInWatchlist.ShouldBeFalse();
        movie.Metadata.ShouldBeNull();
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.EnrichedAt.ShouldBeNull();
        movie.EnrichmentAttempts.ShouldBe(0);
        movie.LastFailureCategory.ShouldBeNull();
        movie.LastAttemptAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Create_BlankTitle_Throws(string title)
    {
        Should.Throw<ArgumentException>(() => Movie.Create(
            new MovieId(1),
            title,
            new LibraryPath(@"C:\lib\a.mkv"),
            new MediaFormat("mkv")));
    }

    [Theory]
    [InlineData(EnrichmentStatus.Pending, EnrichmentAction.MarkEnriched)]
    [InlineData(EnrichmentStatus.Pending, EnrichmentAction.MarkNotFound)]
    [InlineData(EnrichmentStatus.Pending, EnrichmentAction.MarkFailed)]
    [InlineData(EnrichmentStatus.Pending, EnrichmentAction.RequestEnrichment)]
    [InlineData(EnrichmentStatus.Enriched, EnrichmentAction.MarkEnriched)]
    [InlineData(EnrichmentStatus.Enriched, EnrichmentAction.MarkNotFound)]
    [InlineData(EnrichmentStatus.Enriched, EnrichmentAction.MarkFailed)]
    [InlineData(EnrichmentStatus.Enriched, EnrichmentAction.RequestEnrichment)]
    [InlineData(EnrichmentStatus.NotFound, EnrichmentAction.MarkEnriched)]
    [InlineData(EnrichmentStatus.NotFound, EnrichmentAction.MarkNotFound)]
    [InlineData(EnrichmentStatus.NotFound, EnrichmentAction.MarkFailed)]
    [InlineData(EnrichmentStatus.NotFound, EnrichmentAction.RequestEnrichment)]
    [InlineData(EnrichmentStatus.Failed, EnrichmentAction.MarkEnriched)]
    [InlineData(EnrichmentStatus.Failed, EnrichmentAction.MarkNotFound)]
    [InlineData(EnrichmentStatus.Failed, EnrichmentAction.MarkFailed)]
    [InlineData(EnrichmentStatus.Failed, EnrichmentAction.RequestEnrichment)]
    public void Enrichment_FromState_MatchesTransitionTable(EnrichmentStatus status, EnrichmentAction action)
    {
        var movie = InStatus(status);
        movie.AddToWatchlist();
        var before = Capture(movie);

        if (!IsLegal(status, action))
        {
            var exception = Should.Throw<InvalidTransitionException>(() => Act(movie, action));
            exception.Action.ShouldBe(action.ToString());
            exception.State.ShouldBe(status.ToString());
            Capture(movie).ShouldBe(before);
            return;
        }

        Act(movie, action);
        Capture(movie).ShouldBe(Expected(before, action));
    }

    [Theory]
    [InlineData(false, WatchlistAction.Add)]
    [InlineData(false, WatchlistAction.Remove)]
    [InlineData(true, WatchlistAction.Add)]
    [InlineData(true, WatchlistAction.Remove)]
    public void Watchlist_FromMembership_MatchesTransitionTable(bool inWatchlist, WatchlistAction action)
    {
        var movie = InStatus(EnrichmentStatus.Enriched);
        if (inWatchlist)
        {
            movie.AddToWatchlist();
        }

        var before = Capture(movie);
        var legal = inWatchlist ? action == WatchlistAction.Remove : action == WatchlistAction.Add;
        if (!legal)
        {
            var exception = Should.Throw<InvalidTransitionException>(() => Act(movie, action));
            exception.Action.ShouldBe(action == WatchlistAction.Add ? "AddToWatchlist" : "RemoveFromWatchlist");
            exception.State.ShouldBe(inWatchlist ? "InWatchlist" : "NotInWatchlist");
            Capture(movie).ShouldBe(before);
            return;
        }

        Act(movie, action);
        var after = Capture(movie);
        after.ShouldBe(before with { IsInWatchlist = !inWatchlist });
    }

    [Fact]
    public void RequestEnrichment_KeepsAttemptHistory()
    {
        AssertRequestKeepsHistory(InStatus(EnrichmentStatus.Failed));
        AssertRequestKeepsHistory(InStatus(EnrichmentStatus.Enriched));
    }

    [Fact]
    public void LaterOutcome_PreservesFieldsTheTableDoesNotChange()
    {
        var notFound = InStatus(EnrichmentStatus.Failed);
        notFound.RequestEnrichment();
        notFound.MarkNotFound(ActedAt);
        notFound.Status.ShouldBe(EnrichmentStatus.NotFound);
        notFound.EnrichmentAttempts.ShouldBe(2);
        notFound.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        notFound.EnrichedAt.ShouldBeNull();
        notFound.Metadata.ShouldBeNull();
        notFound.LastAttemptAt.ShouldBe(ActedAt);

        var failed = InStatus(EnrichmentStatus.Enriched);
        failed.RequestEnrichment();
        failed.MarkFailed(EnrichmentFailureCategory.InvalidResponse, ActedAt);
        failed.Status.ShouldBe(EnrichmentStatus.Failed);
        failed.EnrichmentAttempts.ShouldBe(2);
        failed.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        failed.EnrichedAt.ShouldBe(ArrivedAt);
        failed.Metadata.ShouldBe(SeedMetadata);
        failed.LastAttemptAt.ShouldBe(ActedAt);

        var enriched = InStatus(EnrichmentStatus.Failed);
        enriched.RequestEnrichment();
        enriched.MarkEnriched(ActMetadata, ActedAt);
        enriched.Status.ShouldBe(EnrichmentStatus.Enriched);
        enriched.EnrichmentAttempts.ShouldBe(2);
        enriched.LastFailureCategory.ShouldBeNull();
        enriched.EnrichedAt.ShouldBe(ActedAt);
        enriched.Metadata.ShouldBe(ActMetadata);
        enriched.LastAttemptAt.ShouldBe(ActedAt);
    }

    private static void AssertRequestKeepsHistory(Movie movie)
    {
        var before = Capture(movie);
        movie.RequestEnrichment();
        var after = Capture(movie);
        after.Status.ShouldBe(EnrichmentStatus.Pending);
        after.EnrichmentAttempts.ShouldBe(before.EnrichmentAttempts);
        after.EnrichedAt.ShouldBe(before.EnrichedAt);
        after.Metadata.ShouldBe(before.Metadata);
        after.LastFailureCategory.ShouldBe(before.LastFailureCategory);
        after.LastAttemptAt.ShouldBe(before.LastAttemptAt);
    }

    private static bool IsLegal(EnrichmentStatus status, EnrichmentAction action) =>
        action == EnrichmentAction.RequestEnrichment
            ? status != EnrichmentStatus.Pending
            : status == EnrichmentStatus.Pending;

    private static Movie InStatus(EnrichmentStatus status)
    {
        var movie = Movie.Create(new MovieId(7), "Seven", new LibraryPath(@"C:\lib\a.mkv"), new MediaFormat("mkv"));
        switch (status)
        {
            case EnrichmentStatus.Pending:
                return movie;
            case EnrichmentStatus.Enriched:
                movie.MarkEnriched(SeedMetadata, ArrivedAt);
                return movie;
            case EnrichmentStatus.NotFound:
                movie.MarkNotFound(ArrivedAt);
                return movie;
            case EnrichmentStatus.Failed:
                movie.MarkFailed(EnrichmentFailureCategory.RateLimited, ArrivedAt);
                return movie;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown enrichment status.");
        }
    }

    private static void Act(Movie movie, EnrichmentAction action)
    {
        switch (action)
        {
            case EnrichmentAction.MarkEnriched:
                movie.MarkEnriched(ActMetadata, ActedAt);
                return;
            case EnrichmentAction.MarkNotFound:
                movie.MarkNotFound(ActedAt);
                return;
            case EnrichmentAction.MarkFailed:
                movie.MarkFailed(EnrichmentFailureCategory.InvalidResponse, ActedAt);
                return;
            case EnrichmentAction.RequestEnrichment:
                movie.RequestEnrichment();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown enrichment action.");
        }
    }

    private static void Act(Movie movie, WatchlistAction action)
    {
        if (action == WatchlistAction.Add)
        {
            movie.AddToWatchlist();
            return;
        }

        movie.RemoveFromWatchlist();
    }

    private static Snapshot Expected(Snapshot before, EnrichmentAction action) =>
        action switch
        {
            EnrichmentAction.MarkEnriched => before with
            {
                Status = EnrichmentStatus.Enriched,
                EnrichmentAttempts = before.EnrichmentAttempts + 1,
                LastAttemptAt = ActedAt,
                EnrichedAt = ActedAt,
                Metadata = ActMetadata,
                LastFailureCategory = null,
            },
            EnrichmentAction.MarkNotFound => before with
            {
                Status = EnrichmentStatus.NotFound,
                EnrichmentAttempts = before.EnrichmentAttempts + 1,
                LastAttemptAt = ActedAt,
            },
            EnrichmentAction.MarkFailed => before with
            {
                Status = EnrichmentStatus.Failed,
                EnrichmentAttempts = before.EnrichmentAttempts + 1,
                LastAttemptAt = ActedAt,
                LastFailureCategory = EnrichmentFailureCategory.InvalidResponse,
            },
            EnrichmentAction.RequestEnrichment => before with { Status = EnrichmentStatus.Pending },
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown enrichment action."),
        };

    private static Snapshot Capture(Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.Path,
        movie.Format,
        movie.IsInWatchlist,
        movie.Metadata,
        movie.Status,
        movie.EnrichedAt,
        movie.EnrichmentAttempts,
        movie.LastFailureCategory,
        movie.LastAttemptAt);

    private sealed record Snapshot(
        MovieId Id,
        string Title,
        LibraryPath Path,
        MediaFormat Format,
        bool IsInWatchlist,
        MovieMetadata? Metadata,
        EnrichmentStatus Status,
        DateTimeOffset? EnrichedAt,
        int EnrichmentAttempts,
        EnrichmentFailureCategory? LastFailureCategory,
        DateTimeOffset? LastAttemptAt);

    public enum EnrichmentAction
    {
        MarkEnriched,
        MarkNotFound,
        MarkFailed,
        RequestEnrichment,
    }

    public enum WatchlistAction
    {
        Add,
        Remove,
    }
}
