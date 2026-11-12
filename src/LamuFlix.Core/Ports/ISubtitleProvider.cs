using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Ports;

public interface ISubtitleProvider
{
    Task<SubtitleSearchResult> SearchAsync(SubtitleSearch search, CancellationToken cancellationToken);

    Task<SubtitleDownload> DownloadAsync(SubtitleCandidate candidate, CancellationToken cancellationToken);
}
