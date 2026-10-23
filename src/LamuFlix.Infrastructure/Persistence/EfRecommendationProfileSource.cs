using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfRecommendationProfileSource(LamuFlixDbContext db) : IRecommendationProfileSource
{
    private readonly LamuFlixDbContext db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<RecommendationProfileInputs> GetProfileInputsAsync(
        IReadOnlyCollection<MovieId> candidateMovieIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(candidateMovieIds);
        ct.ThrowIfCancellationRequested();

        var launches = await ReadLaunchesAsync(ct);
        ct.ThrowIfCancellationRequested();

        var candidateEmbeddings = await ReadEmbeddingsAsync([.. candidateMovieIds], ct);
        ct.ThrowIfCancellationRequested();

        var movies = await ReadTasteProfileMoviesAsync(launches, ct);

        return new RecommendationProfileInputs(movies, candidateEmbeddings);
    }

    private async Task<ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>> ReadLaunchesAsync(
        CancellationToken ct)
    {
        var events = await db.PlaybackEvents
            .AsNoTracking()
            .Select(playbackEvent => new { playbackEvent.MovieId, playbackEvent.PlayedAt })
            .ToListAsync(ct);

        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<DateTimeOffset>>();

        foreach (var launchGroup in events.GroupBy(playbackEvent => playbackEvent.MovieId))
        {
            builder[launchGroup.Key] =
            [
                .. launchGroup
                    .Select(playbackEvent => playbackEvent.PlayedAt)
                    .OrderBy(playedAt => playedAt),
            ];
        }

        return builder.ToImmutable();
    }

    private async Task<ImmutableDictionary<MovieId, ImmutableArray<float>>> ReadEmbeddingsAsync(
        List<MovieId> movieIds,
        CancellationToken ct)
    {
        if (movieIds.Count == 0)
        {
            return ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty;
        }

        var rows = await db.MovieFeatures
            .AsNoTracking()
            .Where(feature => movieIds.Contains(feature.MovieId))
            .Select(feature => new { feature.MovieId, feature.Vector })
            .ToListAsync(ct);

        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<float>>();

        foreach (var row in rows)
        {
            builder[row.MovieId] = ImmutableArray.CreateRange(row.Vector.ToArray());
        }

        return builder.ToImmutable();
    }

    private async Task<ImmutableArray<TasteProfileMovie>> ReadTasteProfileMoviesAsync(
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> launches,
        CancellationToken ct)
    {
        var launchedIds = launches.Keys.ToList();

        var rows = await db.Movies
            .AsNoTracking()
            .Include(movie => movie.Genres)
            .Where(movie => movie.Id != null && (movie.IsWatched || launchedIds.Contains(movie.Id)))
            .OrderBy(movie => movie.Id)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return [];
        }

        var plotEmbeddings = await ReadEmbeddingsAsync([.. rows.Select(row => row.Id).OfType<MovieId>()], ct);
        var builder = ImmutableArray.CreateBuilder<TasteProfileMovie>(rows.Count);

        foreach (var row in rows)
        {
            var movieId = row.Id
                ?? throw new InvalidOperationException("The database returned a movie row without an id.");

            builder.Add(new TasteProfileMovie(
                movieId,
                [.. row.Genres.Select(genre => genre.Name).Order(StringComparer.Ordinal)],
                plotEmbeddings.GetValueOrDefault(movieId, ImmutableArray<float>.Empty),
                launches.GetValueOrDefault(movieId, ImmutableArray<DateTimeOffset>.Empty),
                row.IsWatched));
        }

        return builder.MoveToImmutable();
    }
}
