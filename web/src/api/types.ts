/**
 * AUTO-GENERATED FILE — DO NOT EDIT DIRECTLY.
 *
 * Source:     src/api/openapi.json
 * Regenerate: npm run generate:api
 *
 * Any manual changes will be overwritten by the generator.
 */

export interface paths {
  "/api/movies": {
    /** Browse movies using the canonical library query string keys. */
    get: operations["BrowseMovies"];
  };
  "/api/movies/{id}": {
    /** Retrieve the details of a single movie by its identifier. */
    get: operations["GetMovieDetails"];
    /** Remove a movie from the library, leaving the file on disk. */
    delete: operations["DeleteMovie"];
  };
  "/api/movies/{id}/thumbnail": {
    /** Stream the cached thumbnail JPEG for a movie. */
    get: operations["GetMovieThumbnail"];
  };
  "/api/movies/{id}/stream": {
    /** Stream the primary video file of a movie. */
    get: operations["StreamMovie"];
  };
  "/api/subtitles/languages": {
    /** List the supported subtitle languages with the configured preferences first. */
    get: operations["ListSubtitleLanguages"];
  };
  "/api/movies/{id}/subtitles": {
    /** List the eligible sidecar subtitle tracks of a movie. */
    get: operations["ListMovieSubtitleTracks"];
  };
  "/api/movies/{id}/subtitles/{trackId}/content": {
    /** Serve one eligible sidecar subtitle track converted to WebVTT. */
    get: operations["GetMovieSubtitleContent"];
  };
  "/api/movies/{id}/subtitles/prepare": {
    /** Acquire the exact-basename sidecar subtitle of a movie when acquisition is enabled. */
    post: operations["PrepareMovieSubtitle"];
  };
  "/api/movies/{id}/subtitles/sidecar": {
    /** Serve the existing exact-basename sidecar subtitle of a movie without acquisition. */
    get: operations["GetMovieSubtitleSidecar"];
  };
  "/api/movies/import": {
    /** Import a movie folder into the library. */
    post: operations["ImportMovie"];
  };
  "/api/movies/import/candidates": {
    /** List library folders not yet imported. */
    get: operations["ListImportCandidates"];
  };
  "/api/movies/import/library": {
    /** Import selected library folders and retry failed enrichments. */
    post: operations["ImportLibraryFolders"];
  };
  "/api/movies/rescan": {
    /** Re-probe every stored movie folder and refresh changed formats. */
    post: operations["RescanLibrary"];
  };
  "/api/movies/{id}/rescan": {
    /** Re-probe a movie folder and refresh its stored format. */
    post: operations["RescanMovie"];
  };
  "/api/movies/{id}/enrichment": {
    /** Request a metadata enrichment retry for a movie. */
    post: operations["RequestEnrichment"];
  };
  "/api/movies/{id}/imdb": {
    /** Match a movie to a user-supplied IMDb id and request enrichment. */
    put: operations["MatchMovieImdbId"];
  };
  "/api/movies/{id}/watchlist": {
    /** Add a movie to the watchlist. */
    post: operations["AddToWatchlist"];
    /** Remove a movie from the watchlist. */
    delete: operations["RemoveFromWatchlist"];
  };
  "/api/movies/{id}/play": {
    /** Start local playback of a movie. */
    post: operations["PlayMovie"];
  };
  "/api/playback/capabilities": {
    /** Report the effective local playback capability. */
    get: operations["GetPlaybackCapabilities"];
  };
  "/api/genres": {
    /** List the stored genres. */
    get: operations["GetGenres"];
  };
  "/api/collections": {
    /** List the stored collections with current members. */
    get: operations["GetCollections"];
  };
  "/api/people": {
    /** List the stored actor people. */
    get: operations["GetPeople"];
  };
  "/api/directors": {
    /** List the stored directors linked to at least one movie. */
    get: operations["GetDirectors"];
  };
  "/api/movies/{id}/watched": {
    /** Mark a movie as watched. */
    post: operations["MarkWatched"];
    /** Remove the watched mark from a movie. */
    delete: operations["UnmarkWatched"];
  };
  "/api/movies/{id}/not-interested": {
    /** Mark a movie as not interested. */
    post: operations["MarkNotInterested"];
    /** Remove the not-interested mark from a movie. */
    delete: operations["UnmarkNotInterested"];
  };
  "/api/movies/{id}/review": {
    /** Set the review for a movie. */
    put: operations["SetMovieReview"];
  };
  "/api/movies/{id}/recommendations": {
    /** Get recommendations for a library movie */
    get: operations["GetRecommendations"];
  };
  "/api/recommendations/for-you": {
    /** Get personalized recommendations from the taste profile */
    get: operations["GetForYouRecommendations"];
  };
}

export type webhooks = Record<string, never>;

export interface components {
  schemas: {
    BrowseMoviesResponse: {
      items: components["schemas"]["MovieSummaryResponse"][];
      /** Format: int32 */
      totalCount: number | string;
    };
    CollectionDto: {
      /** Format: int32 */
      id: number | string;
      name: string;
      /** Format: int32 */
      movieCount: number | string;
    };
    DirectorDto: {
      /** Format: int32 */
      id: number | string;
      name: string;
      /** Format: int32 */
      movieCount: number | string;
    };
    /** @enum {string} */
    EnrichmentStatus: "Pending" | "Enriched" | "NotFound" | "Failed";
    EnrichmentStatusChangedDto: {
      /** Format: int32 */
      id: number;
      enrichmentStatus: components["schemas"]["EnrichmentStatus"];
    };
    GenreDto: {
      /** Format: int32 */
      id: number | string;
      name: string;
      /** Format: int32 */
      movieCount: number | string;
    };
    ImportCandidatesResponse: {
      newFolders: string[];
    };
    ImportFolderFailureDto: {
      folder: string;
      reason: string;
    };
    ImportLibraryRequest: {
      folders: null | string[];
      retryFailed: boolean;
    };
    ImportLibraryResponse: {
      /** Format: int32 */
      imported: number | string;
      /** Format: int32 */
      skipped: number | string;
      /** Format: int32 */
      retried: number | string;
      failures: components["schemas"]["ImportFolderFailureDto"][];
    };
    ImportMovieRequest: {
      folderPath: string;
    };
    MatchImdbIdRequest: {
      imdbId: null | string;
    };
    MovieDetailsResponse: {
      /** Format: int32 */
      id: number | string;
      title: string;
      path: string;
      format: string;
      metadata: null | components["schemas"]["MovieMetadataResponse"];
      isInWatchlist: boolean;
      isWatched: boolean;
      isNotInterested: boolean;
      review: string;
      enrichmentStatus: components["schemas"]["EnrichmentStatus"];
    };
    MovieMetadataResponse: {
      title: string;
      synopsis: null | string;
      /** Format: int32 */
      releaseYear: null | number | string;
      /** Format: int32 */
      runtime: null | number | string;
      /** Format: double */
      imdbRating: null | number | string;
      imdbId: null | string;
      posterUrl: null | string;
      /** Format: int16 */
      rottenTomatoesRating: null | number | string;
      /** Format: int16 */
      metaScore: null | number | string;
      directors: string[];
      actors: string[];
      trailerKey: null | string;
    };
    MovieSummaryResponse: {
      /** Format: int32 */
      id: number | string;
      title: string;
      posterUrl: null | string;
      /** Format: int32 */
      releaseYear: null | number | string;
      /** Format: int32 */
      runtime: null | number | string;
      /** Format: double */
      imdbRating: null | number | string;
      enrichmentStatus: components["schemas"]["EnrichmentStatus"];
      isInWatchlist: boolean;
      isWatched: boolean;
      isNotInterested: boolean;
      review: string;
    };
    PersonDto: {
      /** Format: int32 */
      id: number | string;
      name: string;
    };
    PlaybackCapabilitiesDto: {
      localPlayEnabled: boolean;
    };
    ProblemDetails: {
      type?: null | string;
      title?: null | string;
      /** Format: int32 */
      status?: null | number | string;
      detail?: null | string;
      instance?: null | string;
    };
    RecommendedMovieDto: {
      /** Format: int32 */
      id: number | string;
      title: string;
      posterUrl: null | string;
      /** Format: int32 */
      releaseYear: null | number | string;
      /** Format: int32 */
      runtime: null | number | string;
      /** Format: double */
      imdbRating: null | number | string;
      enrichmentStatus: components["schemas"]["EnrichmentStatus"];
      isInWatchlist: boolean;
      isWatched: boolean;
      isNotInterested: boolean;
      review: string;
      /** Format: double */
      score: number | string;
      reasons: components["schemas"]["RecommendedMovieReasonDto"][];
    };
    RecommendedMovieReasonDto: {
      kind: string;
      text: string;
    };
    RescanLibraryResponse: {
      /** Format: int32 */
      changed: number | string;
      /** Format: int32 */
      unchanged: number | string;
      /** Format: int32 */
      failed: number | string;
      failedMovieIds: (number | string)[];
    };
    RescanMovieResponse: {
      /** Format: int32 */
      movieId: number | string;
      changed: boolean;
      oldFormat: string;
      newFormat: string;
    };
    SetMovieReviewRequest: {
      review: null | string;
    };
    /** Format: binary */
    Stream: string;
    SubtitleLanguageOptionDto: {
      code: string;
      englishName: string;
      nativeName: string;
    };
    SubtitleLanguagesResponse: {
      defaultLanguage: null | string;
      languages: components["schemas"]["SubtitleLanguageOptionDto"][];
    };
    SubtitlePrepareResponse: {
      outcome: string;
      selectedLanguage: null | string;
    };
    SubtitleTrackDto: {
      id: string;
      label: string;
      language: string;
      isDefault: boolean;
    };
  };
  responses: never;
  parameters: never;
  requestBodies: never;
  headers: never;
  pathItems: never;
}

export type $defs = Record<string, never>;

export type external = Record<string, never>;

export interface operations {

  /** Browse movies using the canonical library query string keys. */
  BrowseMovies: {
    parameters: {
      query: {
        Text?: string;
        GenreIds?: string[];
        ActorIds?: string[];
        DirectorId?: string;
        RuntimeMin?: string;
        RuntimeMax?: string;
        RuntimeIncludeUnknown?: string;
        YearMin?: string;
        YearMax?: string;
        MinRating?: string;
        MaxRating?: string;
        Statuses?: string[];
        InWatchlist?: string;
        CollectionId?: string;
        sort: "Title" | "Year" | "Rating" | "Runtime" | "Added";
        direction: "Ascending" | "Descending";
        page: number;
        pageSize: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["BrowseMoviesResponse"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Retrieve the details of a single movie by its identifier. */
  GetMovieDetails: {
    parameters: {
      path: {
        id: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["MovieDetailsResponse"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Remove a movie from the library, leaving the file on disk. */
  DeleteMovie: {
    parameters: {
      path: {
        id: number;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Stream the cached thumbnail JPEG for a movie. */
  GetMovieThumbnail: {
    parameters: {
      path: {
        id: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "image/jpeg": string;
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Stream the primary video file of a movie. */
  StreamMovie: {
    parameters: {
      path: {
        id: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/octet-stream": components["schemas"]["Stream"];
        };
      };
      /** @description Partial Content */
      206: {
        content: {
          "application/octet-stream": components["schemas"]["Stream"];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Forbidden */
      403: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Range Not Satisfiable */
      416: {
        content: never;
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** List the supported subtitle languages with the configured preferences first. */
  ListSubtitleLanguages: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["SubtitleLanguagesResponse"];
        };
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** List the eligible sidecar subtitle tracks of a movie. */
  ListMovieSubtitleTracks: {
    parameters: {
      path: {
        id: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["SubtitleTrackDto"][];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Forbidden */
      403: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Serve one eligible sidecar subtitle track converted to WebVTT. */
  GetMovieSubtitleContent: {
    parameters: {
      path: {
        id: number;
        trackId: string;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "text/vtt; charset=utf-8": string;
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Forbidden */
      403: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Acquire the exact-basename sidecar subtitle of a movie when acquisition is enabled. */
  PrepareMovieSubtitle: {
    parameters: {
      path: {
        id: string;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["SubtitlePrepareResponse"];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Forbidden */
      403: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Too Many Requests */
      429: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Bad Gateway */
      502: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Gateway Timeout */
      504: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Serve the existing exact-basename sidecar subtitle of a movie without acquisition. */
  GetMovieSubtitleSidecar: {
    parameters: {
      path: {
        id: string;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/x-subrip": string;
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Forbidden */
      403: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Import a movie folder into the library. */
  ImportMovie: {
    requestBody: {
      content: {
        "application/json": components["schemas"]["ImportMovieRequest"];
      };
    };
    responses: {
      /** @description Accepted */
      202: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** List library folders not yet imported. */
  ListImportCandidates: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["ImportCandidatesResponse"];
        };
      };
      /** @description Internal Server Error */
      500: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Import selected library folders and retry failed enrichments. */
  ImportLibraryFolders: {
    requestBody: {
      content: {
        "application/json": components["schemas"]["ImportLibraryRequest"];
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["ImportLibraryResponse"];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Re-probe every stored movie folder and refresh changed formats. */
  RescanLibrary: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["RescanLibraryResponse"];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Re-probe a movie folder and refresh its stored format. */
  RescanMovie: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["RescanMovieResponse"];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Request a metadata enrichment retry for a movie. */
  RequestEnrichment: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description Accepted */
      202: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Match a movie to a user-supplied IMDb id and request enrichment. */
  MatchMovieImdbId: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    requestBody: {
      content: {
        "application/json": components["schemas"]["MatchImdbIdRequest"];
      };
    };
    responses: {
      /** @description Accepted */
      202: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Add a movie to the watchlist. */
  AddToWatchlist: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Remove a movie from the watchlist. */
  RemoveFromWatchlist: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Start local playback of a movie. */
  PlayMovie: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Forbidden */
      403: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Report the effective local playback capability. */
  GetPlaybackCapabilities: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["PlaybackCapabilitiesDto"];
        };
      };
    };
  };
  /** List the stored genres. */
  GetGenres: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["GenreDto"][];
        };
      };
    };
  };
  /** List the stored collections with current members. */
  GetCollections: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["CollectionDto"][];
        };
      };
    };
  };
  /** List the stored actor people. */
  GetPeople: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["PersonDto"][];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** List the stored directors linked to at least one movie. */
  GetDirectors: {
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["DirectorDto"][];
        };
      };
    };
  };
  /** Mark a movie as watched. */
  MarkWatched: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Remove the watched mark from a movie. */
  UnmarkWatched: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Mark a movie as not interested. */
  MarkNotInterested: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Remove the not-interested mark from a movie. */
  UnmarkNotInterested: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Conflict */
      409: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Set the review for a movie. */
  SetMovieReview: {
    parameters: {
      path: {
        id: number | string;
      };
    };
    requestBody: {
      content: {
        "application/json": components["schemas"]["SetMovieReviewRequest"];
      };
    };
    responses: {
      /** @description No Content */
      204: {
        content: never;
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Get recommendations for a library movie */
  GetRecommendations: {
    parameters: {
      query?: {
        /** @description The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize. */
        take?: number;
      };
      path: {
        id: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["RecommendedMovieDto"][];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Not Found */
      404: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
  /** Get personalized recommendations from the taste profile */
  GetForYouRecommendations: {
    parameters: {
      query?: {
        /** @description The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize. */
        take?: number;
      };
    };
    responses: {
      /** @description OK */
      200: {
        content: {
          "application/json": components["schemas"]["RecommendedMovieDto"][];
        };
      };
      /** @description Bad Request */
      400: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
        };
      };
    };
  };
}
