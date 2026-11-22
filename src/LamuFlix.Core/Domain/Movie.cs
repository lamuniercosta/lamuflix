using System;
using System.Collections.Generic;

namespace LamuFlix.Core.Domain;

public sealed class Movie
{
    private const string InWatchlist = "InWatchlist";
    private const string NotInWatchlist = "NotInWatchlist";
    private const string Watched = "Watched";
    private const string NotWatched = "NotWatched";
    private const string NotInterested = "NotInterested";
    private const string Interested = "Interested";

    private Movie(MovieId id, string title, LibraryPath path, MediaFormat format, ReleaseYear? parsedReleaseYear)
    {
        Id = id;
        Title = title;
        Path = path;
        Format = format;
        ParsedReleaseYear = parsedReleaseYear;
        Status = EnrichmentStatus.Pending;
    }

    public MovieId Id { get; private set; }

    public string Title { get; private set; }

    public LibraryPath Path { get; private set; }

    public MediaFormat Format { get; private set; }

    /// <summary>The release year parsed from the library folder name, used before any metadata is known.</summary>
    public ReleaseYear? ParsedReleaseYear { get; private set; }

    public bool IsInWatchlist { get; private set; }

    public bool IsWatched { get; private set; }

    public Review Review { get; private set; } = Review.None;

    public bool IsNotInterested => Review == Review.Disliked;

    public MovieMetadata? Metadata { get; private set; }

    public EnrichmentStatus Status { get; private set; }

    public DateTimeOffset? EnrichedAt { get; private set; }

    public int EnrichmentAttempts { get; private set; }

    public EnrichmentFailureCategory? LastFailureCategory { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    /// <summary>The UTC instant before which quota-deferred enrichment must not be claimed again.</summary>
    public DateTimeOffset? EnrichmentDeferredUntil { get; private set; }

    /// <summary>An IMDb id supplied by the user when automatic matching cannot find the movie; used instead of title search.</summary>
    public ImdbId? ManualImdbId { get; private set; }

    /// <summary>The TMDb catalogue identity stored for this movie when enrichment or backfill resolved one.</summary>
    public int? TmdbId { get; private set; }

    /// <summary>
    /// The first-page recommended TMDb ids carried by the last successful fetch, or null when that fetch did not succeed.
    /// An empty list is a successful fetch that returned no recommendations.
    /// </summary>
    public IReadOnlyList<int>? RecommendedTmdbIds { get; private set; }

    public static Movie Create(
        MovieId id,
        string title,
        LibraryPath path,
        MediaFormat format,
        ReleaseYear? parsedReleaseYear = null)
    {
        RequireTitle(title);
        return new Movie(id, title, path, format, parsedReleaseYear);
    }

    public static Movie Rehydrate(
        MovieId id,
        string title,
        LibraryPath path,
        MediaFormat format,
        bool isInWatchlist,
        MovieMetadata? metadata,
        EnrichmentStatus status,
        DateTimeOffset? enrichedAt,
        int enrichmentAttempts,
        EnrichmentFailureCategory? lastFailureCategory,
        DateTimeOffset? lastAttemptAt,
        ReleaseYear? parsedReleaseYear = null,
        ImdbId? manualImdbId = null,
        bool isWatched = false,
        Review? review = null,
        DateTimeOffset? enrichmentDeferredUntil = null,
        int? tmdbId = null,
        IReadOnlyList<int>? recommendedTmdbIds = null)
    {
        RequireTitle(title);
        return new Movie(id, title, path, format, parsedReleaseYear)
        {
            IsInWatchlist = isInWatchlist,
            IsWatched = isWatched,
            Review = review ?? Review.None,
            Metadata = metadata,
            Status = status,
            EnrichedAt = enrichedAt,
            EnrichmentAttempts = enrichmentAttempts,
            LastFailureCategory = lastFailureCategory,
            LastAttemptAt = lastAttemptAt,
            ManualImdbId = manualImdbId,
            EnrichmentDeferredUntil = enrichmentDeferredUntil,
            TmdbId = tmdbId,
            RecommendedTmdbIds = recommendedTmdbIds,
        };
    }

    public void MarkEnriched(MovieMetadata metadata, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        BeginPendingAttempt(nameof(MarkEnriched), now);
        EnrichedAt = now;
        Metadata = metadata;
        TmdbId = metadata.TmdbId;
        RecommendedTmdbIds = metadata.RecommendedTmdbIds;
        LastFailureCategory = null;
        Status = EnrichmentStatus.Enriched;
    }

    /// <summary>
    /// Stores the TMDb identity and its first-page recommendations resolved by the one-off operator backfill.
    /// The enrichment state is untouched: this carries catalogue identity, it is not an enrichment result.
    /// </summary>
    public void RecordBackfilledTmdbIdentity(int tmdbId, IReadOnlyList<int> recommendedTmdbIds)
    {
        if (tmdbId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tmdbId), tmdbId, "TMDb id must be positive.");
        }

        ArgumentNullException.ThrowIfNull(recommendedTmdbIds);
        var ids = new List<int>(recommendedTmdbIds.Count);
        foreach (var recommendedTmdbId in recommendedTmdbIds)
        {
            if (recommendedTmdbId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recommendedTmdbIds),
                    recommendedTmdbId,
                    "Recommended TMDb id must be positive.");
            }

            ids.Add(recommendedTmdbId);
        }

        TmdbId = tmdbId;
        RecommendedTmdbIds = ids;
    }

    public void MarkNotFound(DateTimeOffset now)
    {
        BeginPendingAttempt(nameof(MarkNotFound), now);
        Status = EnrichmentStatus.NotFound;
    }

    public void MarkFailed(EnrichmentFailureCategory category, DateTimeOffset now)
    {
        BeginPendingAttempt(nameof(MarkFailed), now);
        LastFailureCategory = category;
        Status = EnrichmentStatus.Failed;
    }

    public void DeferEnrichmentUntil(DateTimeOffset until)
    {
        RequirePending(nameof(DeferEnrichmentUntil));
        EnrichmentDeferredUntil = until;
    }

    public void RequestEnrichment()
    {
        RequireNotPending(nameof(RequestEnrichment));
        Status = EnrichmentStatus.Pending;
    }

    public void RefreshFormat(MediaFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        Format = format;
    }

    public void MatchManually(ImdbId imdbId)
    {
        ArgumentNullException.ThrowIfNull(imdbId);
        ManualImdbId = imdbId;
        LastFailureCategory = null;
        Status = EnrichmentStatus.Pending;
    }

    public void ClearMetadata()
    {
        Metadata = null;
    }

    public void AddToWatchlist()
    {
        if (IsInWatchlist)
        {
            throw new InvalidTransitionException(nameof(AddToWatchlist), InWatchlist);
        }

        IsInWatchlist = true;
    }

    public void RemoveFromWatchlist()
    {
        if (!IsInWatchlist)
        {
            throw new InvalidTransitionException(nameof(RemoveFromWatchlist), NotInWatchlist);
        }

        IsInWatchlist = false;
    }

    public void MarkWatched()
    {
        if (IsWatched)
        {
            throw new InvalidTransitionException(nameof(MarkWatched), Watched);
        }

        IsWatched = true;
    }

    public void UnmarkWatched()
    {
        if (!IsWatched)
        {
            throw new InvalidTransitionException(nameof(UnmarkWatched), NotWatched);
        }

        IsWatched = false;
    }

    public void MarkNotInterested()
    {
        if (Review == Review.Disliked)
        {
            throw new InvalidTransitionException(nameof(MarkNotInterested), NotInterested);
        }

        Review = Review.Disliked;
    }

    public void UnmarkNotInterested()
    {
        if (Review != Review.Disliked)
        {
            throw new InvalidTransitionException(nameof(UnmarkNotInterested), Interested);
        }

        Review = Review.None;
    }

    public void AssignReview(Review review)
    {
        ArgumentNullException.ThrowIfNull(review);
        Review = review;
    }

    private static void RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || IsEffectivelyBlank(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }
    }

    private static bool IsEffectivelyBlank(string title)
    {
        foreach (var character in title)
        {
            if (!char.IsWhiteSpace(character) && !char.IsControl(character))
            {
                return false;
            }
        }

        return true;
    }

    private void BeginPendingAttempt(string action, DateTimeOffset now)
    {
        RequirePending(action);
        LastAttemptAt = now;
    }

    private void RequirePending(string action)
    {
        if (Status != EnrichmentStatus.Pending)
        {
            throw new InvalidTransitionException(action, Status.ToString());
        }
    }

    private void RequireNotPending(string action)
    {
        if (Status == EnrichmentStatus.Pending)
        {
            throw new InvalidTransitionException(action, Status.ToString());
        }
    }
}
