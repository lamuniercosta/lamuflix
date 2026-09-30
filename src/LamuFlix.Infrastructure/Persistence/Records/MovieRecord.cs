using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class MovieRecord
{
    public MovieId? Id { get; set; }

    public required string Title { get; set; }

    public required LibraryPath LibraryPath { get; set; }

    public required MediaFormat Format { get; set; }

    public bool IsInWatchlist { get; set; }

    public required EnrichmentStatus Status { get; set; }

    public DateTimeOffset? EnrichedAt { get; set; }

    public int EnrichmentAttempts { get; set; }

    public EnrichmentFailureCategory? EnrichmentFailureCategory { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public string? MetadataTitle { get; set; }

    public Runtime? RuntimeMinutes { get; set; }

    public ReleaseYear? ReleaseYear { get; set; }

    public ImdbRating? ImdbRating { get; set; }

    public ImdbId? ImdbId { get; set; }

    public short? RottenTomatoesRating { get; set; }

    public short? MetaScore { get; set; }

    public string? Plot { get; set; }

    public string? PosterUrl { get; set; }

    public ICollection<ActorRecord> Actors { get; } = new HashSet<ActorRecord>();

    public ICollection<DirectorRecord> Directors { get; } = new HashSet<DirectorRecord>();

    public ICollection<GenreRecord> Genres { get; } = new HashSet<GenreRecord>();
}
