using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

/// <summary>
/// Lists the eligible sidecar subtitle tracks of a movie: resolves the stored movie, reauthorizes its
/// library folder, resolves the primary video, enumerates that single directory through the authorized
/// reader, and recognizes the eligible sidecars for the primary video's stem. The handler owns the whole
/// policy; the Api route is transport only.
/// </summary>
public sealed class ListSubtitleTracksQueryHandler(
    IMovieCatalog catalog,
    IMediaLibraryScanner scanner,
    ILibraryFileReader reader)
    : IQueryHandler<ListSubtitleTracksQuery, ListSubtitleTracksResult>
{
    public async Task<ListSubtitleTracksResult> HandleAsync(
        ListSubtitleTracksQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var movie = await SubtitleMovieResolution.ResolveAsync(catalog, scanner, reader, query.Id, cancellationToken);

        var fileNames = reader.EnumerateFiles(movie.Folder);
        var sidecars = SubtitleSidecarDiscovery.Discover(movie.PrimaryFileName, fileNames);

        var tracks = ImmutableList.CreateBuilder<SubtitleTrack>();
        foreach (var sidecar in sidecars)
        {
            tracks.Add(new SubtitleTrack(
                SubtitleTrackToken.For(query.Id, sidecar.FileName),
                sidecar.Label,
                sidecar.Language,
                sidecar.IsDefault));
        }

        return new ListSubtitleTracksResult(tracks.ToImmutable());
    }
}
