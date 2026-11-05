using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Library;

public sealed class RescanLibraryCommandHandler(IMovieRepository movies, IMediaLibraryScanner scanner)
    : ICommandHandler<RescanLibraryCommand, RescanLibraryResult>
{
    private readonly RescanMovieOperation operation = new(movies, scanner);

    public async Task<RescanLibraryResult> HandleAsync(
        RescanLibraryCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await movies.SnapshotMovieIdsAsync(cancellationToken);
        var changed = 0;
        var unchanged = 0;
        List<MovieId> failed = [];

        foreach (var id in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RescanExpectedAsync(id, cancellationToken);
            if (result is null)
            {
                failed.Add(id);
                continue;
            }

            if (!result.Changed)
            {
                unchanged++;
                continue;
            }

            await operation.SaveAsync(cancellationToken);
            changed++;
        }

        return new RescanLibraryResult(changed, unchanged, failed.Count, failed);
    }

    private async Task<RescanMovieResult?> RescanExpectedAsync(MovieId id, CancellationToken cancellationToken)
    {
        try
        {
            return await operation.PrepareAsync(id, cancellationToken);
        }
        catch (System.Exception exception) when (exception is NotFoundException or InvalidTransitionException)
        {
            return null;
        }
    }
}
