using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface ISubtitleProvider
{
    Task<IReadOnlyList<SubtitleCandidate>> SearchAsync(SubtitleSearch search, CancellationToken cancellationToken);

    Task<byte[]> DownloadAsync(SubtitleCandidate candidate, CancellationToken cancellationToken);
}
