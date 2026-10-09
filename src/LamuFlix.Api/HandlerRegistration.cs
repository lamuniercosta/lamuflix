using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Features.Watchlist;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Library;
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
        services.AddHandler<RequeueStrandedMoviesCommandHandler, RequeueStrandedMoviesCommand, int>();
        services.AddHandler<SweepStrandedMoviesCommandHandler, SweepStrandedMoviesCommand, int>();
        services.AddHandler<ImportMovieFolderCommandHandler, ImportMovieFolderCommand, MovieId>();
        services.AddScoped<BrowseMoviesQueryValidator>();
        services.AddScoped<IValidator<BrowseMoviesQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<BrowseMoviesQueryValidator>());
        services.AddHandler<BrowseMoviesQueryHandler, BrowseMoviesQuery, PagedResult<MovieSummary>>();
        services.AddScoped<GetMovieDetailsQueryValidator>();
        services.AddScoped<IValidator<GetMovieDetailsQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<GetMovieDetailsQueryValidator>());
        services.AddHandler<GetMovieDetailsQueryHandler, GetMovieDetailsQuery, MovieDetails>();
        services.AddHandler<GetGenresQueryHandler, GetGenresQuery, IReadOnlyList<GenreFacet>>();
        services.AddHandler<GetPeopleQueryHandler, GetPeopleQuery, IReadOnlyList<PersonFacet>>();
        services.AddHandler<PlayMovieCommandHandler, PlayMovieCommand, Unit>();
        services.AddHandler<AddToWatchlistCommandHandler, AddToWatchlistCommand, Unit>();
        services.AddHandler<RemoveFromWatchlistCommandHandler, RemoveFromWatchlistCommand, Unit>();

        return services;
    }
}
