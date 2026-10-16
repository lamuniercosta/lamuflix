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
  "/api/movies/import": {
    /** Import a movie folder into the library. */
    post: operations["ImportMovie"];
  };
  "/api/movies/{id}/enrichment": {
    /** Request a metadata enrichment retry for a movie. */
    post: operations["RequestEnrichment"];
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
  "/api/genres": {
    /** List the stored genres. */
    get: operations["GetGenres"];
  };
  "/api/people": {
    /** List the stored actor people. */
    get: operations["GetPeople"];
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
    /** @enum {string} */
    EnrichmentStatus: "Pending" | "Enriched" | "NotFound" | "Failed";
    GenreDto: {
      /** Format: int32 */
      id: number | string;
      name: string;
      /** Format: int32 */
      movieCount: number | string;
    };
    ImportMovieRequest: {
      folderPath: string;
    };
    MovieDetailsResponse: {
      /** Format: int32 */
      id: number | string;
      title: string;
      path: string;
      format: string;
      metadata: null | components["schemas"]["MovieMetadataResponse"];
      isInWatchlist: boolean;
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
    };
    PersonDto: {
      /** Format: int32 */
      id: number | string;
      name: string;
    };
    ProblemDetails: {
      type?: null | string;
      title?: null | string;
      /** Format: int32 */
      status?: null | number | string;
      detail?: null | string;
      instance?: null | string;
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
        RuntimeMin?: string;
        RuntimeMax?: string;
        RuntimeIncludeUnknown?: string;
        YearMin?: string;
        YearMax?: string;
        Statuses?: string[];
        InWatchlist?: string;
        sort: "Title" | "Year" | "Rating" | "Runtime";
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
      /** @description Unprocessable Entity */
      422: {
        content: {
          "application/problem+json": components["schemas"]["ProblemDetails"];
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
}
