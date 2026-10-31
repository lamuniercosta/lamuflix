using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface IThumbnailGenerator
{
    Task<ThumbnailResult> GenerateAsync(
        string sourcePath,
        CancellationToken cancellationToken = default);
}
