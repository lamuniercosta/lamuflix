using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

internal sealed record ResolvedSubtitleMovie(LibraryPath Folder, string PrimaryFileName);

internal static class SubtitleMovieResolution
{
    internal static async Task<ResolvedSubtitleMovie> ResolveAsync(
        IMovieCatalog catalog,
        IMediaLibraryScanner scanner,
        ILibraryFileReader reader,
        MovieId movieId,
        CancellationToken cancellationToken)
    {
        var details = await catalog.GetDetailsAsync(movieId, cancellationToken);
        if (details is null)
        {
            throw new NotFoundException();
        }

        reader.AuthorizeFolder(details.Path);

        PrimaryVideo primary;
        try
        {
            primary = scanner.ResolvePrimaryVideo(details.Path);
        }
        catch (InvalidTransitionException)
        {
            throw new NotFoundException();
        }

        return new ResolvedSubtitleMovie(details.Path, Path.GetFileName(primary.Path.Value));
    }
}
