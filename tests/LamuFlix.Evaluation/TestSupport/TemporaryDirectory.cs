using System;
using System.IO;

namespace LamuFlix.Evaluation.TestSupport;

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
