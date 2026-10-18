using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface IPlaybackHistory
{
    Task RecordAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken = default);
}
