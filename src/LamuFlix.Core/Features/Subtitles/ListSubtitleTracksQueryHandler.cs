using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

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

        SubtitleDiagnostics.TagMovie(query.Id);
        try
        {
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
        catch (Exception exception) when (exception is NotFoundException or ValidationException or LibraryFileAccessDeniedException)
        {
            SubtitleDiagnostics.TagFailure(exception);
            throw;
        }
    }
}
