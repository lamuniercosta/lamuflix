using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LamuFlix.Core.Domain;

public sealed record MovieMetadata
{
    private const int TrailerKeyLength = 11;

    public MovieMetadata(
        string title,
        string? synopsis = null,
        ReleaseYear? releaseYear = null,
        Runtime? runtime = null,
        ImdbRating? imdbRating = null,
        ImdbId? imdbId = null,
        string? posterUrl = null,
        short? rottenTomatoesRating = null,
        short? metaScore = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? actors = null,
        IEnumerable<string>? genres = null,
        IEnumerable<string>? writers = null,
        int? collectionId = null,
        string? collectionName = null,
        MovieSignalSnapshot? signals = null,
        string? trailerKey = null,
        int? tmdbId = null,
        IEnumerable<int>? recommendedTmdbIds = null,
        IEnumerable<RecommendedTmdbMovie>? recommendedTmdbMovies = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        ValidateCollection(collectionId, collectionName);
        RequirePositiveTmdbId(tmdbId, nameof(tmdbId));

        Title = title;
        Synopsis = synopsis;
        ReleaseYear = releaseYear;
        Runtime = runtime;
        ImdbRating = imdbRating;
        ImdbId = imdbId;
        PosterUrl = posterUrl;
        RottenTomatoesRating = rottenTomatoesRating;
        MetaScore = metaScore;
        Directors = CopyStrings(directors);
        Actors = CopyStrings(actors);
        Genres = FreezeStrings(genres);
        Writers = FreezeStrings(writers);
        CollectionId = collectionId;
        CollectionName = collectionName;
        Signals = signals;
        TrailerKey = trailerKey;
        TmdbId = tmdbId;
        RecommendedTmdbIds = CopyRecommendedTmdbIds(recommendedTmdbIds);
        RecommendedTmdbMovies = CopyRecommendedMovies(recommendedTmdbMovies, RecommendedTmdbIds);
        RecommendedTmdbIds ??= RecommendedTmdbMovies?.Select(movie => movie.TmdbId).ToArray();
    }

    public static bool IsValidTrailerKey(string? candidate)
    {
        if (candidate is null || candidate.Length != TrailerKeyLength)
        {
            return false;
        }

        foreach (var character in candidate)
        {
            if (!IsTrailerKeyCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTrailerKeyCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character) || character == '_' || character == '-';

    private static void ValidateCollection(int? collectionId, string? collectionName)
    {
        if (collectionId is null && collectionName is null)
        {
            return;
        }

        RequireCollectionPair(collectionId, collectionName);
        RequirePositiveCollectionId(collectionId);
        RequireCollectionName(collectionName);
    }

    private static void RequireCollectionPair(int? collectionId, string? collectionName)
    {
        if (collectionId is null || collectionName is null)
        {
            throw new ArgumentException("Collection id and name are required together.");
        }
    }

    private static void RequirePositiveCollectionId(int? collectionId)
    {
        if (collectionId is not > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(collectionId), "Collection id must be positive.");
        }
    }

    private static void RequireCollectionName(string? collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("Collection name is required.", nameof(collectionName));
        }
    }

    private static void RequirePositiveTmdbId(int? tmdbId, string parameterName)
    {
        if (tmdbId is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, tmdbId, "TMDb id must be positive.");
        }
    }

    private static IReadOnlyList<RecommendedTmdbMovie>? CopyRecommendedMovies(
        IEnumerable<RecommendedTmdbMovie>? source,
        IReadOnlyList<int>? ids)
    {
        if (source is null)
        {
            return ids?.Select(RecommendedTmdbMovie.FromId).ToArray();
        }

        var movies = new List<RecommendedTmdbMovie>();
        var seen = new HashSet<int>();
        foreach (var movie in source)
        {
            if (movie.TmdbId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source), movie.TmdbId, "Recommended TMDb id must be positive.");
            }

            if (seen.Add(movie.TmdbId))
            {
                movies.Add(movie);
            }
        }

        return movies;
    }

    private static IReadOnlyList<int>? CopyRecommendedTmdbIds(IEnumerable<int>? source)
    {
        if (source is null)
        {
            return null;
        }

        var ids = new List<int>();
        var seen = new HashSet<int>();
        foreach (var id in source)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source), id, "Recommended TMDb id must be positive.");
            }

            if (seen.Add(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static IReadOnlyList<string> CopyStrings(IEnumerable<string>? source) =>
        source?.ToArray() ?? [];

    private static ImmutableArray<string> FreezeStrings(IEnumerable<string>? source) =>
        source?.ToImmutableArray() ?? ImmutableArray<string>.Empty;

    public string Title { get; }

    public string? Synopsis { get; }

    public ReleaseYear? ReleaseYear { get; }

    public Runtime? Runtime { get; }

    public ImdbRating? ImdbRating { get; }

    public ImdbId? ImdbId { get; }

    public string? PosterUrl { get; }

    public short? RottenTomatoesRating { get; }

    public short? MetaScore { get; }

    public IReadOnlyList<string> Directors { get; }

    public IReadOnlyList<string> Actors { get; }

    public ImmutableArray<string> Genres { get; }

    public ImmutableArray<string> Writers { get; }

    public int? CollectionId { get; }

    public string? CollectionName { get; }

    public MovieSignalSnapshot? Signals { get; }

    public string? TrailerKey { get; }

    public int? TmdbId { get; }

    public IReadOnlyList<int>? RecommendedTmdbIds { get; }

    public IReadOnlyList<RecommendedTmdbMovie>? RecommendedTmdbMovies { get; }

    public MovieMetadata WithSignals(MovieSignalSnapshot? signals) =>
        new(
            Title,
            Synopsis,
            ReleaseYear,
            Runtime,
            ImdbRating,
            ImdbId,
            PosterUrl,
            RottenTomatoesRating,
            MetaScore,
            Directors,
            Actors,
            Genres,
            Writers,
            CollectionId,
            CollectionName,
            signals,
            TrailerKey,
            TmdbId,
            RecommendedTmdbIds,
            RecommendedTmdbMovies);

    public MovieMetadata WithTrailerKey(string? trailerKey) =>
        new(
            Title,
            Synopsis,
            ReleaseYear,
            Runtime,
            ImdbRating,
            ImdbId,
            PosterUrl,
            RottenTomatoesRating,
            MetaScore,
            Directors,
            Actors,
            Genres,
            Writers,
            CollectionId,
            CollectionName,
            Signals,
            trailerKey,
            TmdbId,
            RecommendedTmdbIds,
            RecommendedTmdbMovies);

    public MovieMetadata WithTmdbIdentity(int? tmdbId, IReadOnlyList<int>? recommendedTmdbIds) =>
        WithTmdbIdentity(tmdbId, recommendedTmdbIds?.Select(RecommendedTmdbMovie.FromId).ToArray());

    public MovieMetadata WithTmdbIdentity(int? tmdbId, IReadOnlyList<RecommendedTmdbMovie>? recommendedTmdbMovies) =>
        new(
            Title,
            Synopsis,
            ReleaseYear,
            Runtime,
            ImdbRating,
            ImdbId,
            PosterUrl,
            RottenTomatoesRating,
            MetaScore,
            Directors,
            Actors,
            Genres,
            Writers,
            CollectionId,
            CollectionName,
            Signals,
            TrailerKey,
            tmdbId,
            recommendedTmdbIds: null,
            recommendedTmdbMovies: recommendedTmdbMovies);

    public MovieMetadata WithCollection(int? collectionId, string? collectionName) =>
        new(
            Title,
            Synopsis,
            ReleaseYear,
            Runtime,
            ImdbRating,
            ImdbId,
            PosterUrl,
            RottenTomatoesRating,
            MetaScore,
            Directors,
            Actors,
            Genres,
            Writers,
            collectionId,
            collectionName,
            Signals,
            TrailerKey,
            TmdbId,
            RecommendedTmdbIds,
            RecommendedTmdbMovies);

    public bool Equals(MovieMetadata? other) =>
        other is not null
        && SameScalars(this, other)
        && SameOptionals(this, other)
        && SameCollections(this, other)
        && SameCollectionIdentity(this, other)
        && SameTmdbIdentity(this, other);

    private static bool SameScalars(MovieMetadata left, MovieMetadata right) =>
        left.Title == right.Title
        && left.Synopsis == right.Synopsis
        && left.PosterUrl == right.PosterUrl
        && left.RottenTomatoesRating == right.RottenTomatoesRating
        && left.MetaScore == right.MetaScore
        && left.TrailerKey == right.TrailerKey;

    private static bool SameOptionals(MovieMetadata left, MovieMetadata right) =>
        Equals(left.ReleaseYear, right.ReleaseYear)
        && Equals(left.Runtime, right.Runtime)
        && Equals(left.ImdbRating, right.ImdbRating)
        && Equals(left.ImdbId, right.ImdbId);

    private static bool SameCollections(MovieMetadata left, MovieMetadata right) =>
        left.Directors.SequenceEqual(right.Directors)
        && left.Actors.SequenceEqual(right.Actors)
        && left.Genres.SequenceEqual(right.Genres)
        && left.Writers.SequenceEqual(right.Writers)
        && Equals(left.Signals, right.Signals);

    private static bool SameCollectionIdentity(MovieMetadata left, MovieMetadata right) =>
        left.CollectionId == right.CollectionId
        && left.CollectionName == right.CollectionName;

    private static bool SameTmdbIdentity(MovieMetadata left, MovieMetadata right) =>
        left.TmdbId == right.TmdbId
        && SameRecommendedTmdbMovies(left.RecommendedTmdbMovies, right.RecommendedTmdbMovies);

    private static bool SameRecommendedTmdbMovies(
        IReadOnlyList<RecommendedTmdbMovie>? left,
        IReadOnlyList<RecommendedTmdbMovie>? right) =>
        left is null ? right is null : right is not null && left.SequenceEqual(right);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        AddScalars(ref hash);
        AddRecommended(ref hash);
        AddCollections(ref hash);
        return hash.ToHashCode();
    }

    private void AddScalars(ref HashCode hash)
    {
        hash.Add(Title);
        hash.Add(Synopsis);
        hash.Add(ReleaseYear);
        hash.Add(Runtime);
        hash.Add(ImdbRating);
        hash.Add(ImdbId);
        hash.Add(PosterUrl);
        hash.Add(RottenTomatoesRating);
        hash.Add(MetaScore);
        hash.Add(CollectionId);
        hash.Add(CollectionName);
        hash.Add(Signals);
        hash.Add(TrailerKey);
        hash.Add(TmdbId);
    }

    private void AddRecommended(ref HashCode hash)
    {
        if (RecommendedTmdbIds is null)
        {
            return;
        }

        if (RecommendedTmdbMovies is null)
        {
            return;
        }

        foreach (var movie in RecommendedTmdbMovies)
        {
            hash.Add(movie);
        }
    }

    private void AddCollections(ref HashCode hash)
    {
        foreach (var director in Directors)
        {
            hash.Add(director);
        }

        foreach (var actor in Actors)
        {
            hash.Add(actor);
        }

        foreach (var genre in Genres)
        {
            hash.Add(genre);
        }

        foreach (var writer in Writers)
        {
            hash.Add(writer);
        }
    }
}

