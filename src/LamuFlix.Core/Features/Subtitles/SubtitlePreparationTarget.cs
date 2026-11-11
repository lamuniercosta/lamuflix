using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitlePreparationTarget(
    LibraryPath Folder,
    LibraryPath PrimaryFile,
    string PrimaryFileName,
    string Title)
{
    public static async Task<SubtitlePreparationTarget> ResolveAsync(
        IMovieCatalog catalog,
        IMediaLibraryScanner scanner,
        ILibraryFileReader reader,
        MovieId movieId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(scanner);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(movieId);

        var details = await catalog.GetDetailsAsync(movieId, cancellationToken)
            ?? throw new NotFoundException();

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

        return new SubtitlePreparationTarget(
            details.Path,
            primary.Path,
            Path.GetFileName(primary.Path.Value),
            details.Title);
    }
}
