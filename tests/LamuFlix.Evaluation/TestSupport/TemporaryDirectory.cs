using System;
using System.IO;

namespace LamuFlix.Evaluation.TestSupport;

/// <summary>
/// Disposable scratch directory for command/publication tests. Nothing outside
/// this directory (except the ignored default output path explicitly asserted by
/// the defaults test) is written by the suite.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        FullPath = Path.Combine(Path.GetTempPath(), "lamuflix-evaluation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FullPath);
    }

    public string FullPath { get; }

    public string Combine(params string[] parts) => Path.Combine([FullPath, .. parts]);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp directory must never fail an assertion.
        }
        catch (UnauthorizedAccessException)
        {
            // Same: cleanup is best-effort.
        }
    }
}
