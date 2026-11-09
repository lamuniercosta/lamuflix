using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Playback;

public sealed class GetPlaybackCapabilitiesQueryHandler
    : IQueryHandler<GetPlaybackCapabilitiesQuery, PlaybackCapabilitiesResult>
{
    public Task<PlaybackCapabilitiesResult> HandleAsync(
        GetPlaybackCapabilitiesQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PlaybackCapabilitiesResult(query.LocalPlayEnabled));
}
