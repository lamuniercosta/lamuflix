using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record ResolvedSubtitleMovie(LibraryPath Folder, string PrimaryFileName);

public static class SubtitleMovieResolution
{
    public static async Task<ResolvedSubtitleMovie> ResolveAsync(
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

        try
        {
            var primary = scanner.ResolvePrimaryVideo(details.Path);
            return new ResolvedSubtitleMovie(details.Path, Path.GetFileName(primary.Path.Value));
        }
        catch (InvalidTransitionException)
        {
            throw new NotFoundException();
        }
    }
}
