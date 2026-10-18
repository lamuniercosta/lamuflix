using System;

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

    public bool IsNotInterested { get; private set; }

    public MovieMetadata? Metadata { get; private set; }

    public EnrichmentStatus Status { get; private set; }

    public DateTimeOffset? EnrichedAt { get; private set; }

    public int EnrichmentAttempts { get; private set; }

    public EnrichmentFailureCategory? LastFailureCategory { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    /// <summary>An IMDb id supplied by the user when automatic matching cannot find the movie; used instead of title search.</summary>
    public ImdbId? ManualImdbId { get; private set; }

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
        bool isNotInterested = false)
    {
        RequireTitle(title);
        return new Movie(id, title, path, format, parsedReleaseYear)
        {
            IsInWatchlist = isInWatchlist,
            IsWatched = isWatched,
            IsNotInterested = isNotInterested,
            Metadata = metadata,
            Status = status,
            EnrichedAt = enrichedAt,
            EnrichmentAttempts = enrichmentAttempts,
            LastFailureCategory = lastFailureCategory,
            LastAttemptAt = lastAttemptAt,
            ManualImdbId = manualImdbId,
        };
    }

    public void MarkEnriched(MovieMetadata metadata, DateTimeOffset now)
    {
        BeginPendingAttempt(nameof(MarkEnriched), now);
        EnrichedAt = now;
        Metadata = metadata;
        LastFailureCategory = null;
        Status = EnrichmentStatus.Enriched;
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

    public void RequestEnrichment()
    {
        RequireNotPending(nameof(RequestEnrichment));
        Status = EnrichmentStatus.Pending;
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
        if (IsNotInterested)
        {
            throw new InvalidTransitionException(nameof(MarkNotInterested), NotInterested);
        }

        IsNotInterested = true;
    }

    public void UnmarkNotInterested()
    {
        if (!IsNotInterested)
        {
            throw new InvalidTransitionException(nameof(UnmarkNotInterested), Interested);
        }

        IsNotInterested = false;
    }

    private static void RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }
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
