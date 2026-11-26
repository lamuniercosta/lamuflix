using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;

namespace LamuFlix.Infrastructure.Streaming;

internal readonly record struct SourceVersion(string Path, long Length, long LastWriteTicks);

internal sealed record ProbeSelection(int AudioOrdinal, string Codec)
{
    public bool NeedsConversion => Codec is "ac3" or "eac3" or "dts";
}

internal sealed record ProbeCacheEntry(ProbeSelection Selection);

internal sealed class CompletedAsset(
    SourceVersion version,
    string path,
    long length,
    DateTimeOffset lastAccess)
{
    public SourceVersion Version { get; } = version;

    public string Path { get; } = path;

    public long Length { get; } = length;

    public DateTimeOffset LastAccess { get; set; } = lastAccess;

    public int Readers { get; set; }
}

internal sealed class InFlightConversion(
    SourceVersion version,
    int movieId,
    string sourcePath,
    int audioOrdinal,
    string partialPath,
    string finalFileName,
    CancellationTokenSource cancellation,
    TaskCompletionSource<StreamConversionResult> completion)
{
    public SourceVersion Version { get; } = version;

    public int MovieId { get; } = movieId;

    public string SourcePath { get; } = sourcePath;

    public int AudioOrdinal { get; } = audioOrdinal;

    public string PartialPath { get; } = partialPath;

    public string FinalFileName { get; } = finalFileName;

    public CancellationTokenSource Cancellation { get; } = cancellation;

    public TaskCompletionSource<StreamConversionResult> Completion { get; } = completion;

    public int Waiters { get; set; }

    public ITimer? GraceTimer { get; set; }
}

internal sealed class SourceVersionChangedException : Exception
{
}
