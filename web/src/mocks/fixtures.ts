import type { components } from '../api/types';

type Schemas = components['schemas'];

const browseMoviesData = {
  items: [
    {
      id: 1,
      title: 'Dune',
      posterUrl: '/posters/dune.jpg',
      releaseYear: 2021,
      runtime: 155,
      imdbRating: 8.0,
      enrichmentStatus: 'Enriched',
    },
    {
      id: 2,
      title: 'Arrival',
      posterUrl: null,
      releaseYear: null,
      runtime: null,
      imdbRating: null,
      enrichmentStatus: 'Pending',
    },
  ],
  totalCount: 2,
} satisfies Schemas['BrowseMoviesResponse'];

const movieDetailsData = {
  id: 1,
  title: 'Dune',
  path: '/media/dune.mkv',
  format: 'mkv',
  metadata: {
    title: 'Dune',
    synopsis: 'A noble family becomes embroiled in a war for the desert planet Arrakis.',
    releaseYear: 2021,
    runtime: 155,
    imdbRating: 8.0,
    imdbId: 'tt1160419',
  },
} satisfies Schemas['MovieDetailsResponse'];

const movieDetailsNullMetadataData = {
  id: 1,
  title: 'Dune',
  path: '/media/dune.mkv',
  format: 'mkv',
  metadata: null,
} satisfies Schemas['MovieDetailsResponse'];

const genresData = [
  { id: 1, name: 'Action' },
  { id: 2, name: 'Sci-Fi' },
] satisfies Schemas['GenreDto'][];

const peopleData = [
  { id: 1, name: 'Timothée Chalamet' },
  { id: 2, name: 'Rebecca Ferguson' },
] satisfies Schemas['PersonDto'][];

export const browseMovies = Object.freeze(browseMoviesData);
export const movieDetails = Object.freeze(movieDetailsData);
export const movieDetailsNullMetadata = Object.freeze(movieDetailsNullMetadataData);
export const genres = Object.freeze(genresData);
export const people = Object.freeze(peopleData);

export const success = Object.freeze({
  BrowseMovies: browseMovies,
  GetMovieDetails: movieDetails,
  GetGenres: genres,
  GetPeople: people,
});

export const acceptedLocation = '/api/movies/1';

export function problemDetails(status: number): Schemas['ProblemDetails'] {
  return {
    type: 'about:blank',
    title: `HTTP ${status}`,
    status,
    detail: `Mock problem for status ${status}`,
    instance: null,
  };
}
