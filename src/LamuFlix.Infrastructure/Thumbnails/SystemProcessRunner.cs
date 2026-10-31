using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Infrastructure.Thumbnails;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class SystemProcessRunner : IProcessRunner
{
    public const int MaxRetainedBytes = 64 * 1024;

    private const int ReadBufferSize = 8192;

    private readonly TimeProvider timeProvider;

    public SystemProcessRunner(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(arguments);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero.");
        }

        var startInfo = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return LaunchFailed();
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
        {
            return LaunchFailed();
        }

        var standardOutput = ReadStreamAsync(process.StandardOutput.BaseStream);
        var standardError = ReadStreamAsync(process.StandardError.BaseStream);

        using var timeoutSource = new CancellationTokenSource(timeout, timeProvider);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timedOut = !cancellationToken.IsCancellationRequested;
            KillTree(process);
        }

        var stdout = await DrainAsync(standardOutput).ConfigureAwait(false);
        var stderr = await DrainAsync(standardError).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (timedOut)
        {
            return new ProcessResult(false, null, stdout.Value, stderr.Value, "TimedOut");
        }

        if (stdout.Truncated || stderr.Truncated)
        {
            return new ProcessResult(
                false,
                process.ExitCode,
                stdout.Value,
                stderr.Value,
                "OutputTruncated");
        }

        if (process.ExitCode != 0)
        {
            return new ProcessResult(
                false,
                process.ExitCode,
                stdout.Value,
                stderr.Value,
                "NonZeroExit");
        }

        return new ProcessResult(true, 0, stdout.Value, stderr.Value, null);
    }

    private static ProcessResult LaunchFailed() =>
        new(false, null, string.Empty, string.Empty, "LaunchFailed");

    private static async Task<StreamCapture> ReadStreamAsync(Stream stream)
    {
        var buffer = new byte[ReadBufferSize];
        using var retained = new MemoryStream();
        var truncated = false;

        int read;
        while ((read = await stream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            var room = MaxRetainedBytes - (int)retained.Length;
            if (room > 0)
            {
                retained.Write(buffer, 0, Math.Min(room, read));
            }

            if (read > room)
            {
                truncated = true;
            }
        }

        return new StreamCapture(Encoding.UTF8.GetString(retained.ToArray()), truncated);
    }

    private static async Task<StreamCapture> DrainAsync(Task<StreamCapture> capture)
    {
        try
        {
            return await capture.ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            return new StreamCapture(string.Empty, false);
        }
    }

    private static void KillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or Win32Exception
            or NotSupportedException
            or AggregateException)
        {
            // The process already exited or part of the tree could not be terminated; cleanup is best effort.
        }
    }

    private readonly record struct StreamCapture(string Value, bool Truncated);
}
