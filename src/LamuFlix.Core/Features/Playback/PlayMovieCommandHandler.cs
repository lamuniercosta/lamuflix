using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
    [SuppressMessage("Style", "IDE0052:Remove unread private members", Justification = "Retained until the recording step uses it.")]
    [SuppressMessage("Performance", "CA1823:Avoid unused private fields", Justification = "Retained until the recording step uses it.")]
    private readonly IPlaybackHistory playbackHistory = playbackHistory;

    [SuppressMessage("Style", "IDE0052:Remove unread private members", Justification = "Retained until the recording step uses it.")]
    [SuppressMessage("Performance", "CA1823:Avoid unused private fields", Justification = "Retained until the recording step uses it.")]
    private readonly TimeProvider timeProvider = timeProvider;

    public async Task<Unit> HandleAsync(PlayMovieCommand command, CancellationToken cancellationToken)
    {
        var details = await catalog.GetDetailsAsync(command.Id, cancellationToken)
                      ?? throw new NotFoundException();
        launcher.Launch(details.Path, details.Format);
        return Unit.Value;
    }
}
