using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Fluent;
using LamuFlix.Core.Domain;
using Xunit;

namespace LamuFlix.Test.Domain;

public sealed class PropertyTests
{
    [Fact]
    [Trait("Category", "Property")]
    public void MovieId_TryCreate_AgreesWithPositiveRule()
    {
        Prop.ForAll<int>(value => TryAgrees(
            MovieId.TryCreate(value, out var created),
            created,
            value > 0,
            () => new MovieId(value))).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ImdbId_TryCreate_AgreesWithPattern()
    {
        Prop.ForAll<string>(value =>
        {
            var matches = value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, @"^tt\d{7,8}$");
            return TryAgrees(ImdbId.TryCreate(value, out var created), created, matches, () => new ImdbId(value));
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ImdbRating_TryCreate_AgreesWithOneDecimalRule()
    {
        Prop.ForAll<decimal>(value =>
        {
            var valid = value >= 0.0m && value <= 10.0m && value == decimal.Round(value, 1);
            return TryAgrees(ImdbRating.TryCreate(value, out var created), created, valid, () => new ImdbRating(value));
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Runtime_TryCreate_AgreesWithPositiveRule()
    {
        Prop.ForAll<int>(minutes => TryAgrees(
            Runtime.TryCreate(minutes, out var created),
            created,
            minutes > 0,
            () => new Runtime(minutes))).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void ReleaseYear_TryCreate_AgreesWithBounds()
    {
        Prop.ForAll((int value, DateTimeOffset now) =>
        {
            var valid = value >= 1888 && value <= now.Year + 5;
            return TryAgrees(
                ReleaseYear.TryCreate(value, now, out var created),
                created,
                valid,
                () => new ReleaseYear(value, now));
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void LibraryPath_TryCreate_RejectsBlankAndParentSegments()
    {
        Prop.ForAll<string>(value =>
        {
            var valid = !string.IsNullOrWhiteSpace(value) && !value.Contains("..", StringComparison.Ordinal);
            return TryAgrees(LibraryPath.TryCreate(value, out var created), created, valid, () => new LibraryPath(value));
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    [Trait("Category", "Property")]
    public void MediaFormat_TryCreate_AgreesWithNormalization()
    {
        Prop.ForAll<string>(value =>
        {
            var valid = TryNormalize(value, out var expected);
            var createdOk = MediaFormat.TryCreate(value, out var created);
            if (createdOk != valid)
            {
                return false;
            }

            if (!valid)
            {
                return created is null && ConstructorThrows(value);
            }

            return created is not null
                && created.Extension == expected
                && new MediaFormat(value).Extension == expected;
        }).QuickCheckThrowOnFailure();
    }

    private static bool TryAgrees<T>(bool createdOk, T? created, bool valid, Func<T> construct)
        where T : class
    {
        if (createdOk != valid)
        {
            return false;
        }

        if (!valid)
        {
            return created is null && ConstructorThrows(construct);
        }

        return created is not null && EqualityComparer<T>.Default.Equals(created, construct());
    }

    private static bool ConstructorThrows<T>(Func<T> construct)
    {
        try
        {
            _ = construct();
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static bool ConstructorThrows(string? value)
    {
        try
        {
            _ = new MediaFormat(value);
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Movie_TransitionTable_AgreesForEveryStateAndAction()
    {
        Prop.ForAll((DateTimeOffset actedAt, string title) =>
        {
            var safeTitle = string.IsNullOrWhiteSpace(title) ? "Title" : title;
            var when = actedAt == ArrivedAt ? actedAt.AddTicks(1) : actedAt;
            foreach (EnrichmentStatus status in Enum.GetValues<EnrichmentStatus>())
            {
                foreach (MovieTransition action in Enum.GetValues<MovieTransition>())
                {
                    if (!TransitionAgrees(status, action, when, safeTitle))
                    {
                        return false;
                    }
                }
            }

            return WatchlistAgrees(safeTitle);
        }).QuickCheckThrowOnFailure();
    }

    private static readonly DateTimeOffset ArrivedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static bool TransitionAgrees(EnrichmentStatus status, MovieTransition action, DateTimeOffset actedAt, string title)
    {
        var movie = MovieIn(status, title);
        var before = Take(movie);
        var legal = IsLegal(status, action);
        if (!TryApply(movie, action, actedAt, title, out var rejected))
        {
            return false;
        }

        if (rejected)
        {
            return !legal && SameHistory(movie, before);
        }

        if (!legal)
        {
            return false;
        }

        return OutcomeMatches(movie, before, action, actedAt, title);
    }

    private static bool IsLegal(EnrichmentStatus status, MovieTransition action)
    {
        if (action == MovieTransition.RequestEnrichment)
        {
            return status != EnrichmentStatus.Pending;
        }

        return status == EnrichmentStatus.Pending;
    }

    private static bool TryApply(Movie movie, MovieTransition action, DateTimeOffset actedAt, string title, out bool rejected)
    {
        try
        {
            Apply(movie, action, actedAt, title);
            rejected = false;
            return true;
        }
        catch (InvalidTransitionException)
        {
            rejected = true;
            return true;
        }
        catch (Exception)
        {
            rejected = false;
            return false;
        }
    }

    private static bool OutcomeMatches(Movie movie, Taken before, MovieTransition action, DateTimeOffset actedAt, string title)
    {
        if (movie.IsInWatchlist != before.InWatchlist)
        {
            return false;
        }

        if (movie.Title != title)
        {
            return false;
        }

        switch (action)
        {
            case MovieTransition.MarkEnriched:
                return MarkEnrichedMatches(movie, before, actedAt, title);
            case MovieTransition.MarkNotFound:
                return AttemptMatches(movie, before, actedAt) && KeptHistory(movie, before);
            case MovieTransition.MarkFailed:
                return FailedMatches(movie, before, actedAt);
            case MovieTransition.RequestEnrichment:
                return RequestMatches(movie, before);
            default:
                return false;
        }
    }

    private static bool MarkEnrichedMatches(Movie movie, Taken before, DateTimeOffset actedAt, string title)
    {
        if (movie.Status != EnrichmentStatus.Enriched)
        {
            return false;
        }

        if (movie.EnrichmentAttempts != before.Attempts + 1)
        {
            return false;
        }

        if (movie.EnrichedAt != actedAt || movie.LastAttemptAt != actedAt)
        {
            return false;
        }

        if (movie.Metadata is null || movie.Metadata.Title != title)
        {
            return false;
        }

        return movie.LastFailureCategory is null;
    }

    private static bool FailedMatches(Movie movie, Taken before, DateTimeOffset actedAt)
    {
        if (!AttemptMatches(movie, before, actedAt))
        {
            return false;
        }

        if (movie.Status != EnrichmentStatus.Failed)
        {
            return false;
        }

        if (movie.LastFailureCategory != EnrichmentFailureCategory.Unknown)
        {
            return false;
        }

        if (movie.EnrichedAt != before.EnrichedAt)
        {
            return false;
        }

        return movie.Metadata == before.Metadata;
    }

    private static bool AttemptMatches(Movie movie, Taken before, DateTimeOffset actedAt)
    {
        if (movie.EnrichmentAttempts != before.Attempts + 1)
        {
            return false;
        }

        return movie.LastAttemptAt == actedAt;
    }

    private static bool KeptHistory(Movie movie, Taken before)
    {
        if (movie.EnrichedAt != before.EnrichedAt)
        {
            return false;
        }

        return movie.Metadata == before.Metadata && movie.LastFailureCategory == before.Category;
    }

    private static bool RequestMatches(Movie movie, Taken before)
    {
        if (movie.Status != EnrichmentStatus.Pending)
        {
            return false;
        }

        if (movie.EnrichmentAttempts != before.Attempts)
        {
            return false;
        }

        if (movie.LastAttemptAt != before.LastAttemptAt)
        {
            return false;
        }

        return KeptHistory(movie, before);
    }

    private static bool SameHistory(Movie movie, Taken before)
    {
        if (movie.Status != before.Status)
        {
            return false;
        }

        if (movie.EnrichmentAttempts != before.Attempts)
        {
            return false;
        }

        if (movie.LastAttemptAt != before.LastAttemptAt)
        {
            return false;
        }

        if (movie.IsInWatchlist != before.InWatchlist)
        {
            return false;
        }

        return KeptHistory(movie, before);
    }

    private static Taken Take(Movie movie) => new(
        movie.Status,
        movie.EnrichmentAttempts,
        movie.EnrichedAt,
        movie.Metadata,
        movie.LastFailureCategory,
        movie.LastAttemptAt,
        movie.IsInWatchlist);

    private readonly record struct Taken(
        EnrichmentStatus Status,
        int Attempts,
        DateTimeOffset? EnrichedAt,
        MovieMetadata? Metadata,
        EnrichmentFailureCategory? Category,
        DateTimeOffset? LastAttemptAt,
        bool InWatchlist);

    private static bool WatchlistAgrees(string title)
    {
        return MembershipAgrees(title, startInWatchlist: false, add: true, legal: true)
            && MembershipAgrees(title, startInWatchlist: false, add: false, legal: false)
            && MembershipAgrees(title, startInWatchlist: true, add: true, legal: false)
            && MembershipAgrees(title, startInWatchlist: true, add: false, legal: true);
    }

    private static bool MembershipAgrees(string title, bool startInWatchlist, bool add, bool legal)
    {
        var movie = MovieIn(EnrichmentStatus.Enriched, title);
        if (startInWatchlist)
        {
            movie.AddToWatchlist();
        }

        var before = Take(movie);
        if (!TryWatchlist(movie, add, out var rejected))
        {
            return false;
        }

        if (rejected)
        {
            return !legal && SameHistory(movie, before);
        }

        return legal && WatchlistFlipped(movie, before);
    }

    private static bool TryWatchlist(Movie movie, bool add, out bool rejected)
    {
        try
        {
            if (add)
            {
                movie.AddToWatchlist();
            }
            else
            {
                movie.RemoveFromWatchlist();
            }

            rejected = false;
            return true;
        }
        catch (InvalidTransitionException)
        {
            rejected = true;
            return true;
        }
        catch (Exception)
        {
            rejected = false;
            return false;
        }
    }

    private static bool WatchlistFlipped(Movie movie, Taken before) =>
        movie.IsInWatchlist == !before.InWatchlist
        && movie.Status == before.Status
        && movie.EnrichmentAttempts == before.Attempts
        && movie.LastAttemptAt == before.LastAttemptAt
        && KeptHistory(movie, before);

    private static Movie MovieIn(EnrichmentStatus status, string title) =>
        MovieFixture.CreateInStatus(status, 1, title, ArrivedAt);

    private static void Apply(Movie movie, MovieTransition action, DateTimeOffset actedAt, string title)
    {
        switch (action)
        {
            case MovieTransition.MarkEnriched:
                movie.MarkEnriched(new MovieMetadata(title), actedAt);
                return;
            case MovieTransition.MarkNotFound:
                movie.MarkNotFound(actedAt);
                return;
            case MovieTransition.MarkFailed:
                movie.MarkFailed(EnrichmentFailureCategory.Unknown, actedAt);
                return;
            case MovieTransition.RequestEnrichment:
                movie.RequestEnrichment();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown enrichment action.");
        }
    }

    private enum MovieTransition
    {
        MarkEnriched,
        MarkNotFound,
        MarkFailed,
        RequestEnrichment,
    }

    private static bool TryNormalize(string? extension, out string expected)
    {
        expected = string.Empty;
        if (extension is null)
        {
            return false;
        }

        var trimmed = extension.Trim();
        if (trimmed.StartsWith('.'))
        {
            trimmed = trimmed[1..];
        }

        trimmed = trimmed.ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return false;
        }

        expected = trimmed;
        return true;
    }
}
