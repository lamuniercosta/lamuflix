using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface IStreamConversionService
{
    Task<StreamConversionResult> PrepareAsync(
        int movieId,
        string sourcePath,
        CancellationToken cancellationToken = default);

    Task<StreamReaderLease> OpenReadAsync(
        StreamConversionResult prepared,
        CancellationToken cancellationToken = default);
}

public sealed record StreamConversionResult(string Path, bool Converted);

public sealed class StreamReaderLease : IDisposable
{
    private readonly Action? release;
    private bool disposed;

    public StreamReaderLease(Stream stream, Action? release = null)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.release = release;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public Stream Stream { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Stream.Dispose();
        release?.Invoke();
    }
}
