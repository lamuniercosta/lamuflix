using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Subtitles;

public static class MovieHashReader
{
    public static async Task<MovieHashLookup> ReadAsync(
        ILibraryFileReader reader,
        LibraryPath file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(file);

        await using var stream = reader.OpenRead(file);
        if (!stream.CanSeek)
        {
            return new MovieHashLookup(MovieHash: null, FileLength: 0);
        }

        var fileLength = stream.Length;
        if (!MovieHash.IsEligible(fileLength))
        {
            return new MovieHashLookup(MovieHash: null, FileLength: fileLength);
        }

        var head = new byte[MovieHash.WindowLength];
        var tail = new byte[MovieHash.WindowLength];
        await stream.ReadExactlyAsync(head, cancellationToken);
        stream.Seek(-MovieHash.WindowLength, SeekOrigin.End);
        await stream.ReadExactlyAsync(tail, cancellationToken);
        return new MovieHashLookup(MovieHash.Compute(fileLength, head, tail), fileLength);
    }
}
