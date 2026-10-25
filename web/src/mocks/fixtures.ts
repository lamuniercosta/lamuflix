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
      isInWatchlist: false,
      isWatched: false,
      isNotInterested: false,
    },
    {
      id: 2,
      title: 'Arrival',
      posterUrl: null,
      releaseYear: null,
      runtime: null,
      imdbRating: null,
      enrichmentStatus: 'Pending',
      isInWatchlist: false,
      isWatched: false,
      isNotInterested: false,
    },
  ],
  totalCount: 2,
} satisfies Schemas['BrowseMoviesResponse'];

const movieDetailsData = {
  id: 1,
  title: 'Dune',
  path: '/media/dune.mkv',
  format: 'mkv',
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
  metadata: {
    title: 'Dune',
    synopsis: 'A noble family becomes embroiled in a war for the desert planet Arrakis.',
    releaseYear: 2021,
    runtime: 155,
    imdbRating: 8.0,
    imdbId: 'tt1160419',
    posterUrl: null,
    rottenTomatoesRating: null,
    metaScore: null,
    directors: [],
    actors: [],
  },
} satisfies Schemas['MovieDetailsResponse'];

const movieDetailsNullMetadataData = {
  id: 1,
  title: 'Dune',
  path: '/media/dune.mkv',
  format: 'mkv',
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
  metadata: null,
} satisfies Schemas['MovieDetailsResponse'];

const recommendationsData: Schemas['RecommendedMovieDto'][] = [];

const relatedMoviesData = [
  {
    id: 2,
    title: 'Arrival',
    posterUrl: '/posters/arrival.jpg',
    releaseYear: 2016,
    runtime: 116,
    imdbRating: 7.9,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.82,
    reasons: [
      { kind: 'structural', text: 'Same director' },
      { kind: 'personal', text: 'Matches your Drama interest' },
    ],
  },
  {
    id: 3,
    title: 'Blade Runner 2049',
    posterUrl: '/posters/blade-runner-2049.jpg',
    releaseYear: 2017,
    runtime: 164,
    imdbRating: 8.0,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.77,
    reasons: [],
  },
  {
    id: 4,
    title: 'Prisoners',
    posterUrl: null,
    releaseYear: null,
    runtime: 153,
    imdbRating: 8.1,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.64,
    reasons: [{ kind: 'structural', text: 'Same director' }],
  },
] satisfies Schemas['RecommendedMovieDto'][];

const forYouRecommendationsData = [
  {
    id: 7,
    title: 'The Batman',
    posterUrl: '/posters/the-batman.jpg',
    releaseYear: 2022,
    runtime: 176,
    imdbRating: 7.8,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.91,
    reasons: [
      { kind: 'personal', text: 'Because you watched Dune' },
      { kind: 'structural', text: 'Same director' },
    ],
  },
  {
    id: 8,
    title: 'Sicario',
    posterUrl: '/posters/sicario.jpg',
    releaseYear: 2015,
    runtime: 121,
    imdbRating: 7.6,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.84,
    reasons: [],
  },
  {
    id: 9,
    title: 'Nightcrawler',
    posterUrl: '/posters/nightcrawler.jpg',
    releaseYear: 2014,
    runtime: 117,
    imdbRating: 7.8,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.72,
    reasons: [{ kind: 'structural', text: 'Same genre' }],
  },
  {
    id: 10,
    title: 'Enemy',
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Pending',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.61,
    reasons: [{ kind: 'personal', text: 'Matches your Thriller interest' }],
  },
] satisfies Schemas['RecommendedMovieDto'][];

const genresData = [
  { id: 1, name: 'Action', movieCount: 10 },
  { id: 2, name: 'Sci-Fi', movieCount: 5 },
] satisfies Schemas['GenreDto'][];

const peopleData = [
  { id: 1, name: 'Timothée Chalamet' },
  { id: 2, name: 'Rebecca Ferguson' },
] satisfies Schemas['PersonDto'][];

export const browseMovies = Object.freeze(browseMoviesData);
export const movieDetails = Object.freeze(movieDetailsData);
export const movieDetailsNullMetadata = Object.freeze(movieDetailsNullMetadataData);
export const recommendations = Object.freeze(recommendationsData);
export const relatedMovies = Object.freeze(relatedMoviesData);
export const forYouRecommendations = Object.freeze(forYouRecommendationsData);
export const genres = Object.freeze(genresData);
export const people = Object.freeze(peopleData);

export const success = Object.freeze({
  BrowseMovies: browseMovies,
  GetMovieDetails: movieDetails,
  GetRecommendations: recommendations,
  GetForYouRecommendations: forYouRecommendations,
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
