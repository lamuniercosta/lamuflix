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
        movie.IsWatched.ShouldBeFalse();
        movie.IsNotInterested.ShouldBeFalse();
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

    public static TheoryData<EnrichmentStatus, EnrichmentAction> EnrichmentTransitions()
    {
        var data = new TheoryData<EnrichmentStatus, EnrichmentAction>();
        foreach (var status in EnrichmentStatus.List)
        {
            data.Add(status, EnrichmentAction.MarkEnriched);
            data.Add(status, EnrichmentAction.MarkNotFound);
            data.Add(status, EnrichmentAction.MarkFailed);
            data.Add(status, EnrichmentAction.RequestEnrichment);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EnrichmentTransitions))]
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

    public static TheoryData<bool, bool, bool, FeedbackAction> FeedbackTransitions()
    {
        var data = new TheoryData<bool, bool, bool, FeedbackAction>();
        foreach (var isWatched in new[] { false, true })
        {
            foreach (var isNotInterested in new[] { false, true })
            {
                foreach (var isInWatchlist in new[] { false, true })
                {
                    foreach (var action in Enum.GetValues<FeedbackAction>())
                    {
                        data.Add(isWatched, isNotInterested, isInWatchlist, action);
                    }
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(FeedbackTransitions))]
    public void Feedback_FromMarksAndWatchlist_MatchesTransitionTable(
        bool isWatched,
        bool isNotInterested,
        bool isInWatchlist,
        FeedbackAction action)
    {
        var movie = InFeedbackState(isWatched, isNotInterested, isInWatchlist);
        var before = Capture(movie);

        if (!IsLegal(isWatched, isNotInterested, action))
        {
            var exception = Should.Throw<InvalidTransitionException>(() => Act(movie, action));
            exception.Action.ShouldBe(FeedbackActionName(action));
            exception.State.ShouldBe(FeedbackStateToken(action));
            Capture(movie).ShouldBe(before);
            movie.IsInWatchlist.ShouldBe(isInWatchlist);
            return;
        }

        Act(movie, action);
        Capture(movie).ShouldBe(Expected(before, action));
        movie.IsInWatchlist.ShouldBe(isInWatchlist);
        AssertUntouchedFeedbackFlag(movie, before, action);
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
        var notFoundAttempts = notFound.EnrichmentAttempts;
        notFound.MarkNotFound(ActedAt);
        notFound.Status.ShouldBe(EnrichmentStatus.NotFound);
        notFound.EnrichmentAttempts.ShouldBe(notFoundAttempts);
        notFound.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        notFound.EnrichedAt.ShouldBeNull();
        notFound.Metadata.ShouldBeNull();
        notFound.LastAttemptAt.ShouldBe(ActedAt);

        var failed = InStatus(EnrichmentStatus.Enriched);
        failed.RequestEnrichment();
        var failedAttempts = failed.EnrichmentAttempts;
        failed.MarkFailed(EnrichmentFailureCategory.InvalidResponse, ActedAt);
        failed.Status.ShouldBe(EnrichmentStatus.Failed);
        failed.EnrichmentAttempts.ShouldBe(failedAttempts);
        failed.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        failed.EnrichedAt.ShouldBe(ArrivedAt);
        failed.Metadata.ShouldBe(SeedMetadata);
        failed.LastAttemptAt.ShouldBe(ActedAt);

        var enriched = InStatus(EnrichmentStatus.Failed);
        enriched.RequestEnrichment();
        var enrichedAttempts = enriched.EnrichmentAttempts;
        enriched.MarkEnriched(ActMetadata, ActedAt);
        enriched.Status.ShouldBe(EnrichmentStatus.Enriched);
        enriched.EnrichmentAttempts.ShouldBe(enrichedAttempts);
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

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void MatchManually_FromAnyStatus_SetsManualIdAndReturnsToPendingKeepingHistory(EnrichmentStatus status)
    {
        var movie = InStatus(status);
        var imdbId = new ImdbId("tt0083658");
        var before = Capture(movie);

        movie.MatchManually(imdbId);

        movie.ManualImdbId.ShouldBe(imdbId);
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.LastFailureCategory.ShouldBeNull();
        movie.EnrichmentAttempts.ShouldBe(before.EnrichmentAttempts);
        movie.LastAttemptAt.ShouldBe(before.LastAttemptAt);
    }

    [Fact]
    public void MatchManually_NullImdbId_ThrowsArgumentNullException()
    {
        var movie = InStatus(EnrichmentStatus.NotFound);

        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null exercises the guard clause.
        Should.Throw<ArgumentNullException>(() => movie.MatchManually(null!));
    }

    public static TheoryData<EnrichmentStatus> AllStatuses()
    {
        var data = new TheoryData<EnrichmentStatus>();
        foreach (var status in EnrichmentStatus.List)
        {
            data.Add(status);
        }

        return data;
    }

    private static bool IsLegal(EnrichmentStatus status, EnrichmentAction action) =>
        action == EnrichmentAction.RequestEnrichment
            ? status != EnrichmentStatus.Pending
            : status == EnrichmentStatus.Pending;

    private static Movie InStatus(EnrichmentStatus status) =>
        MovieFixture.CreateInStatus(status, 7, "Seven", ArrivedAt);

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

    private static Movie InFeedbackState(bool isWatched, bool isNotInterested, bool isInWatchlist)
    {
        var movie = InStatus(EnrichmentStatus.Enriched);
        if (isWatched)
        {
            movie.MarkWatched();
        }

        if (isNotInterested)
        {
            movie.MarkNotInterested();
        }

        if (isInWatchlist)
        {
            movie.AddToWatchlist();
        }

        return movie;
    }

    private static bool IsLegal(bool isWatched, bool isNotInterested, FeedbackAction action) =>
        action switch
        {
            FeedbackAction.MarkWatched => !isWatched,
            FeedbackAction.UnmarkWatched => isWatched,
            FeedbackAction.MarkNotInterested => !isNotInterested,
            FeedbackAction.UnmarkNotInterested => isNotInterested,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown feedback action."),
        };

    private static void Act(Movie movie, FeedbackAction action)
    {
        switch (action)
        {
            case FeedbackAction.MarkWatched:
                movie.MarkWatched();
                return;
            case FeedbackAction.UnmarkWatched:
                movie.UnmarkWatched();
                return;
            case FeedbackAction.MarkNotInterested:
                movie.MarkNotInterested();
                return;
            case FeedbackAction.UnmarkNotInterested:
                movie.UnmarkNotInterested();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown feedback action.");
        }
    }

    private static string FeedbackActionName(FeedbackAction action) =>
        action switch
        {
            FeedbackAction.MarkWatched => "MarkWatched",
            FeedbackAction.UnmarkWatched => "UnmarkWatched",
            FeedbackAction.MarkNotInterested => "MarkNotInterested",
            FeedbackAction.UnmarkNotInterested => "UnmarkNotInterested",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown feedback action."),
        };

    private static string FeedbackStateToken(FeedbackAction action) =>
        action switch
        {
            FeedbackAction.MarkWatched => "Watched",
            FeedbackAction.UnmarkWatched => "NotWatched",
            FeedbackAction.MarkNotInterested => "NotInterested",
            FeedbackAction.UnmarkNotInterested => "Interested",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown feedback action."),
        };

    private static void AssertUntouchedFeedbackFlag(Movie movie, Snapshot before, FeedbackAction action)
    {
        if (action is FeedbackAction.MarkWatched or FeedbackAction.UnmarkWatched)
        {
            movie.IsNotInterested.ShouldBe(before.IsNotInterested);
            return;
        }

        movie.IsWatched.ShouldBe(before.IsWatched);
    }

    private static Snapshot Expected(Snapshot before, FeedbackAction action) =>
        action switch
        {
            FeedbackAction.MarkWatched => before with { IsWatched = true },
            FeedbackAction.UnmarkWatched => before with { IsWatched = false },
            FeedbackAction.MarkNotInterested => before with { IsNotInterested = true },
            FeedbackAction.UnmarkNotInterested => before with { IsNotInterested = false },
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown feedback action."),
        };

    private static Snapshot Expected(Snapshot before, EnrichmentAction action) =>
        action switch
        {
            EnrichmentAction.MarkEnriched => before with
            {
                Status = EnrichmentStatus.Enriched,
                EnrichmentAttempts = before.EnrichmentAttempts,
                LastAttemptAt = ActedAt,
                EnrichedAt = ActedAt,
                Metadata = ActMetadata,
                LastFailureCategory = null,
            },
            EnrichmentAction.MarkNotFound => before with
            {
                Status = EnrichmentStatus.NotFound,
                EnrichmentAttempts = before.EnrichmentAttempts,
                LastAttemptAt = ActedAt,
            },
            EnrichmentAction.MarkFailed => before with
            {
                Status = EnrichmentStatus.Failed,
                EnrichmentAttempts = before.EnrichmentAttempts,
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
        movie.IsWatched,
        movie.IsNotInterested,
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
        bool IsWatched,
        bool IsNotInterested,
        MovieMetadata? Metadata,
        EnrichmentStatus Status,
        DateTimeOffset? EnrichedAt,
        int EnrichmentAttempts,
        EnrichmentFailureCategory? LastFailureCategory,
        DateTimeOffset? LastAttemptAt)
    {
        public bool Equals(Snapshot? other) => other is not null && State() == other.State();

        public override int GetHashCode() => State().GetHashCode();

        private (
            MovieId Id,
            string Title,
            LibraryPath Path,
            MediaFormat Format,
            bool IsInWatchlist,
            bool IsWatched,
            bool IsNotInterested,
            MovieMetadata? Metadata,
            EnrichmentStatus Status,
            DateTimeOffset? EnrichedAt,
            int EnrichmentAttempts,
            EnrichmentFailureCategory? LastFailureCategory,
            DateTimeOffset? LastAttemptAt) State() =>
            (Id, Title, Path, Format, IsInWatchlist, IsWatched, IsNotInterested, Metadata, Status, EnrichedAt, EnrichmentAttempts, LastFailureCategory, LastAttemptAt);
    }

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

    public enum FeedbackAction
    {
        MarkWatched,
        UnmarkWatched,
        MarkNotInterested,
        UnmarkNotInterested,
    }
}
