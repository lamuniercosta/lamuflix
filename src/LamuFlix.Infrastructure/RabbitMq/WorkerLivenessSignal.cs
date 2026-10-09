using System;
using System.IO.Abstractions;

namespace LamuFlix.Infrastructure.RabbitMq;

public interface IWorkerLiveness
{
    void MarkListening();

    void MarkStopped();
}

public sealed class WorkerLivenessSignal(IFileSystem fileSystem, TimeProvider timeProvider, string directory)
    : IWorkerLiveness
{
    public const string FileName = "worker.live";

    private readonly IFileSystem fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly string directory = directory ?? throw new ArgumentNullException(nameof(directory));

    public void MarkListening()
    {
        fileSystem.Directory.CreateDirectory(directory);
        fileSystem.File.WriteAllText(Path(), timeProvider.GetUtcNow().ToString("O"));
    }

    public void MarkStopped()
    {
        var path = Path();
        if (fileSystem.File.Exists(path))
        {
            fileSystem.File.Delete(path);
        }
    }

    private string Path() => fileSystem.Path.Combine(directory, FileName);
}
