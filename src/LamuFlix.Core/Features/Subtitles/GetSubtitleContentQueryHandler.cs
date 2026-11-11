using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Subtitles;

public sealed class GetSubtitleContentQueryHandler(
    IMovieCatalog catalog,
    IMediaLibraryScanner scanner,
    ILibraryFileReader reader,
    IOptions<LibraryOptions> libraryOptions)
    : IQueryHandler<GetSubtitleContentQuery, SubtitleContentResult>
{
    private const int ReadBufferSize = 81920;

    public async Task<SubtitleContentResult> HandleAsync(
        GetSubtitleContentQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        SubtitleDiagnostics.TagMovie(query.Id);
        try
        {
            var options = libraryOptions.Value;
            var movie = await SubtitleMovieResolution.ResolveAsync(catalog, scanner, reader, query.Id, cancellationToken);

            var fileName = ResolveEligibleFileName(query, movie.Folder, movie.PrimaryFileName);
            var payload = await ReadBoundedAsync(
                CombinePath(movie.Folder, fileName),
                options.MaxSubtitleInputBytes,
                cancellationToken);

            return new SubtitleContentResult(Convert(SubtitleTextDecoder.Decode(payload), options));
        }
        catch (Exception exception) when (exception is NotFoundException or ValidationException or LibraryFileAccessDeniedException)
        {
            SubtitleDiagnostics.TagFailure(exception);
            throw;
        }
    }

    private static string Convert(string document, LibraryOptions options)
    {
        if (document.Length == 0)
        {
            throw SubtitlePayloadErrors.Invalid();
        }

        return SubtitleWebVttConverter.Convert(
            document,
            new SubtitleConversionLimits(
                options.MaxSubtitleCues,
                options.MaxSubtitleLineCharacters,
                options.MaxSubtitleOutputBytes));
    }

    private string ResolveEligibleFileName(
        GetSubtitleContentQuery query,
        LibraryPath folder,
        string primaryFileName)
    {
        var sidecars = SubtitleSidecarDiscovery.Discover(
            primaryFileName,
            reader.EnumerateFiles(folder));

        var matched = default(string);
        foreach (var sidecar in sidecars)
        {
            if (!string.Equals(SubtitleTrackToken.For(query.Id, sidecar.FileName), query.TrackId, StringComparison.Ordinal))
            {
                continue;
            }

            if (matched is not null)
            {
                throw new NotFoundException();
            }

            matched = sidecar.FileName;
        }

        return matched ?? throw new NotFoundException();
    }

    private static LibraryPath CombinePath(LibraryPath folder, string fileName)
    {
        var combined = Path.Combine(folder.Value, fileName);
        return LibraryPath.TryCreate(combined, out var path) ? path : throw new NotFoundException();
    }

    private async Task<byte[]> ReadBoundedAsync(
        LibraryPath file,
        int maxInputBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = reader.OpenRead(file);
        var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
        try
        {
            using var content = new MemoryStream();
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, ReadBufferSize), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (content.Length + read > maxInputBytes)
                {
                    throw SubtitlePayloadErrors.Invalid();
                }

                content.Write(buffer, 0, read);
            }

            return content.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
