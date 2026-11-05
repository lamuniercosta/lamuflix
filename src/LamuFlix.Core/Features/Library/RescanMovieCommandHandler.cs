using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class RescanMovieCommandHandler(IMovieRepository movies, IMediaLibraryScanner scanner)
    : ICommandHandler<RescanMovieCommand, RescanMovieResult>
{
    private readonly RescanMovieOperation operation = new(movies, scanner);

    public Task<RescanMovieResult> HandleAsync(RescanMovieCommand command, CancellationToken cancellationToken) =>
        operation.ExecuteAsync(command.Id, cancellationToken);
}
