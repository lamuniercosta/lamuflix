using System;

namespace LamuFlix.Core.Domain;

public sealed class Movie
{
    private const string InWatchlist = "InWatchlist";
    private const string NotInWatchlist = "NotInWatchlist";

    private Movie(MovieId id, string title, LibraryPath path, MediaFormat format)
    {
        Id = id;
        Title = title;
        Path = path;
        Format = format;
        Status = EnrichmentStatus.Pending;
    }

    public MovieId Id { get; private set; }

    public string Title { get; private set; }

    public LibraryPath Path { get; private set; }

    public MediaFormat Format { get; private set; }

    public bool IsInWatchlist { get; private set; }

    public MovieMetadata? Metadata { get; private set; }

    public EnrichmentStatus Status { get; private set; }

    public DateTimeOffset? EnrichedAt { get; private set; }

    public int EnrichmentAttempts { get; private set; }

    public EnrichmentFailureCategory? LastFailureCategory { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    public static Movie Create(MovieId id, string title, LibraryPath path, MediaFormat format)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        return new Movie(id, title, path, format);
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
        DateTimeOffset? lastAttemptAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        return new Movie(id, title, path, format)
        {
            IsInWatchlist = isInWatchlist,
            Metadata = metadata,
            Status = status,
            EnrichedAt = enrichedAt,
            EnrichmentAttempts = enrichmentAttempts,
            LastFailureCategory = lastFailureCategory,
            LastAttemptAt = lastAttemptAt,
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

    private void BeginPendingAttempt(string action, DateTimeOffset now)
    {
        RequirePending(action);
        EnrichmentAttempts++;
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
