using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.Infrastructure.Import;

/// <summary>
/// Imports each Candidate Movie Folder inside a fresh async scope so no DbContext or
/// scoped adapter is ever shared between concurrent consumers.
/// </summary>
public sealed class ScopedMovieFolderImporter(IServiceScopeFactory scopeFactory) : IMovieFolderImporter
{
    public async Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);

        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportMovieFolderCommand, MovieId>>();
        return await handler.HandleAsync(new ImportMovieFolderCommand(folder), cancellationToken);
    }
}
