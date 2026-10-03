using System;
using System.IO.Abstractions;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Features.Watchlist;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LamuFlix.Api;

internal static class HandlerRegistration
{
    internal static IServiceCollection AddLamuFlixHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IFileSystem, FileSystem>();
        services.AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>();
        services.AddHandler<RequestEnrichmentCommandHandler, RequestEnrichmentCommand, MovieId>();
        services.AddHandler<ImportMovieFolderCommandHandler, ImportMovieFolderCommand, MovieId>();
        services.AddHandler<BrowseMoviesQueryHandler, BrowseMoviesQuery, PagedResult<MovieSummary>>();
        services.AddHandler<GetMovieDetailsQueryHandler, GetMovieDetailsQuery, MovieDetails>();
        services.AddHandler<PlayMovieCommandHandler, PlayMovieCommand, Unit>();
        services.AddHandler<AddToWatchlistCommandHandler, AddToWatchlistCommand, Unit>();
        services.AddHandler<RemoveFromWatchlistCommandHandler, RemoveFromWatchlistCommand, Unit>();

        return services;
    }
}
