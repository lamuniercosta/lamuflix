using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Abstractions;
using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Feedback;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Features.Watchlist;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.Infrastructure.Import;
using LamuFlix.Infrastructure.Library;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.Recommendations;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api;

internal static class HandlerRegistration
{
    internal static IServiceCollection AddLamuFlixHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IFileSystem, FileSystem>();
        services.AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>();
        services.AddScoped<ILibraryFileReader, LibraryFileReader>();
        services.AddScoped<ISubtitleSidecarStore, SubtitleSidecarStore>();
        services.TryAddSingleton<FileExtensionContentTypeProvider>();
        services.AddScoped<ILibraryFolderDiscovery, FileSystemLibraryFolderDiscovery>();
        services.AddScoped<IMovieFolderImporter, ScopedMovieFolderImporter>();
        services.AddHandler<RequestEnrichmentCommandHandler, RequestEnrichmentCommand, MovieId>();
        services.AddHandler<MatchMovieImdbIdCommandHandler, MatchMovieImdbIdCommand, MovieId>();
        services.AddHandler<RequeueStrandedMoviesCommandHandler, RequeueStrandedMoviesCommand, int>();
        services.AddHandler<SweepStrandedMoviesCommandHandler, SweepStrandedMoviesCommand, int>();
        services.AddHandler<ImportMovieFolderCommandHandler, ImportMovieFolderCommand, MovieId>();
        services.AddHandler<ImportLibraryCommandHandler, ImportLibraryCommand, ImportLibraryResult>();
        services.AddHandler<ImportCandidatesQueryHandler, ImportCandidatesQuery, ImportCandidatesResult>();
        services.AddScoped<ImportLibraryFoldersCommandValidator>();
        services.AddScoped<IValidator<ImportLibraryFoldersCommand>>(
            serviceProvider => serviceProvider.GetRequiredService<ImportLibraryFoldersCommandValidator>());
        services.AddHandler<ImportLibraryFoldersCommandHandler, ImportLibraryFoldersCommand, ImportLibraryFoldersResult>();
        services.AddScoped<BrowseMoviesQueryValidator>();
        services.AddScoped<IValidator<BrowseMoviesQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<BrowseMoviesQueryValidator>());
        services.AddHandler<BrowseMoviesQueryHandler, BrowseMoviesQuery, PagedResult<MovieSummary>>();
        services.AddScoped<GetMovieDetailsQueryValidator>();
        services.AddScoped<IValidator<GetMovieDetailsQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<GetMovieDetailsQueryValidator>());
        services.AddHandler<GetMovieDetailsQueryHandler, GetMovieDetailsQuery, MovieDetails>();
        services.AddScoped<GetRecommendationsQueryValidator>();
        services.AddScoped<IValidator<GetRecommendationsQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<GetRecommendationsQueryValidator>());
        services.AddHandler<GetRecommendationsQueryHandler, GetRecommendationsQuery, ImmutableList<RecommendedMovie>>();
        services.AddScoped<GetForYouRecommendationsQueryValidator>();
        services.AddScoped<IValidator<GetForYouRecommendationsQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<GetForYouRecommendationsQueryValidator>());
        services.AddHandler<GetForYouRecommendationsQueryHandler, GetForYouRecommendationsQuery, ImmutableList<RecommendedMovie>>();
        services.AddHandler<DeleteMovieCommandHandler, DeleteMovieCommand, Unit>();
        services.AddHandler<GetGenresQueryHandler, GetGenresQuery, IReadOnlyList<GenreFacet>>();
        services.AddHandler<GetCollectionsQueryHandler, GetCollectionsQuery, IReadOnlyList<CollectionFacet>>();
        services.AddHandler<GetPeopleQueryHandler, GetPeopleQuery, IReadOnlyList<PersonFacet>>();
        services.AddHandler<GetDirectorsQueryHandler, GetDirectorsQuery, IReadOnlyList<DirectorFacet>>();
        services.AddScoped<RescanMovieCommandValidator>();
        services.AddScoped<IValidator<RescanMovieCommand>>(
            serviceProvider => serviceProvider.GetRequiredService<RescanMovieCommandValidator>());
        services.AddHandler<RescanMovieCommandHandler, RescanMovieCommand, RescanMovieResult>();
        services.AddScoped<RescanLibraryCommandValidator>();
        services.AddScoped<IValidator<RescanLibraryCommand>>(
            serviceProvider => serviceProvider.GetRequiredService<RescanLibraryCommandValidator>());
        services.AddHandler<RescanLibraryCommandHandler, RescanLibraryCommand, RescanLibraryResult>();
        services.AddHandler<PlayMovieCommandHandler, PlayMovieCommand, Unit>();
        services.AddHandler<GetPlaybackCapabilitiesQueryHandler, GetPlaybackCapabilitiesQuery, PlaybackCapabilitiesResult>();
        services.AddHandler<ListSubtitleTracksQueryHandler, ListSubtitleTracksQuery, ListSubtitleTracksResult>();
        services.AddHandler<GetSubtitleLanguagesQueryHandler, GetSubtitleLanguagesQuery, GetSubtitleLanguagesResult>();
        services.AddScoped<SearchSubtitlesQueryValidator>();
        services.AddScoped<IValidator<SearchSubtitlesQuery>>(
            serviceProvider => serviceProvider.GetRequiredService<SearchSubtitlesQueryValidator>());
        services.AddHandler<SearchSubtitlesQueryHandler, SearchSubtitlesQuery, SearchSubtitlesResult>();
        services.AddHandler<GetSubtitleContentQueryHandler, GetSubtitleContentQuery, SubtitleContentResult>();
        services.AddHandler<PrepareSubtitleCommandHandler, PrepareSubtitleCommand, SubtitlePreparationResult>();
        services.AddHandler<GetSubtitleSidecarQueryHandler, GetSubtitleSidecarQuery, SubtitleSidecarResult>();
        services.AddHandler<AddToWatchlistCommandHandler, AddToWatchlistCommand, Unit>();
        services.AddHandler<RemoveFromWatchlistCommandHandler, RemoveFromWatchlistCommand, Unit>();
        services.AddHandler<MarkWatchedCommandHandler, MarkWatchedCommand, Unit>();
        services.AddHandler<UnmarkWatchedCommandHandler, UnmarkWatchedCommand, Unit>();
        services.AddHandler<MarkNotInterestedCommandHandler, MarkNotInterestedCommand, Unit>();
        services.AddHandler<UnmarkNotInterestedCommandHandler, UnmarkNotInterestedCommand, Unit>();
        services.AddScoped<SetMovieReviewCommandValidator>();
        services.AddScoped<IValidator<SetMovieReviewCommand>>(
            serviceProvider => serviceProvider.GetRequiredService<SetMovieReviewCommandValidator>());
        services.AddHandler<SetMovieReviewCommandHandler, SetMovieReviewCommand, Unit>();
        AddSubtitleAcquisitionOptions(services);
        services.AddSubtitleProvider();

        return services;
    }

    private static void AddSubtitleAcquisitionOptions(IServiceCollection services)
    {
        services.AddOptions<SubtitlesOptions>().BindConfiguration(SubtitlesOptions.SectionName);
        services.AddSingleton<IValidateOptions<SubtitlesOptions>, SubtitlesOptionsValidator>();
        services.AddOptions<SubtitleProviderOptions>().BindConfiguration(SubtitleProviderOptions.SectionName);
        services.AddSingleton<IValidateOptions<SubtitleProviderOptions>, SubtitleProviderOptionsValidator>();
        services.AddSingleton(serviceProvider => new SubtitleCandidateBinder(
            serviceProvider.GetRequiredService<TimeProvider>(),
            new Random(),
            () => serviceProvider.GetRequiredService<IOptions<SubtitlesOptions>>().Value.CandidateTokenTtl,
            () => serviceProvider.GetRequiredService<IOptions<SubtitlesOptions>>().Value.CandidateStoreMaxEntries));
    }
}
