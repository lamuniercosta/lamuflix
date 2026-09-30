using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Core.Features.Playback;

public sealed class PlayMovieCommandHandler(IMovieCatalog catalog, IMediaPlayerLauncher launcher)
    : ICommandHandler<PlayMovieCommand, Unit>
{
    public async Task<Unit> HandleAsync(PlayMovieCommand command, CancellationToken cancellationToken)
    {
        var details = await catalog.GetDetailsAsync(command.Id, cancellationToken)
                      ?? throw new NotFoundException();
        launcher.Launch(details.Path, details.Format);
        return Unit.Value;
    }
}
