using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Playback;

public sealed class PlayMovieCommandHandler(
    IMovieCatalog catalog,
    IMediaPlayerLauncher launcher,
    IPlaybackHistory playbackHistory,
    TimeProvider timeProvider)
    : ICommandHandler<PlayMovieCommand, Unit>
{
    public async Task<Unit> HandleAsync(PlayMovieCommand command, CancellationToken cancellationToken)
    {
        var details = await catalog.GetDetailsAsync(command.Id, cancellationToken)
                      ?? throw new NotFoundException();
        launcher.Launch(details.Path, details.Format);
        var playedAt = timeProvider.GetUtcNow();
        await playbackHistory.RecordAsync(new PlaybackEvent(command.Id, playedAt), cancellationToken);
        return Unit.Value;
    }
}
