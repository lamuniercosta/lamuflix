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
      review: 'none',
      failureCategory: null,
      failureMessage: null,
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
      review: 'none',
      failureCategory: null,
      failureMessage: null,
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
  review: 'none',
  enrichmentStatus: 'Enriched',
  failureCategory: null,
  failureMessage: null,
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
    trailerKey: 'dQw4w9WgXcQ',
  },
} satisfies Schemas['MovieDetailsResponse'];

const movieDetailsNoTrailerData = {
  id: 1,
  title: 'Dune',
  path: '/media/dune.mkv',
  format: 'mkv',
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
  review: 'none',
  enrichmentStatus: 'Enriched',
  failureCategory: null,
  failureMessage: null,
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
    trailerKey: null,
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
  review: 'none',
  enrichmentStatus: 'NotFound',
  failureCategory: null,
  failureMessage: null,
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
    review: 'liked',
    score: 0.82,
    evidence: [
      {
        criterion: 'Director',
        text: 'Same director: Denis Villeneuve',
        matchedNames: ['Denis Villeneuve'],
      },
      {
        criterion: 'Themes',
        text: 'Shared themes: first contact, language',
        matchedNames: ['first contact', 'language'],
      },
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
    review: 'none',
    score: 0.77,
    evidence: [],
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
    review: 'none',
    score: 0.64,
    evidence: [
      {
        criterion: 'Director',
        text: 'Same director: Denis Villeneuve',
        matchedNames: ['Denis Villeneuve'],
      },
    ],
  },
] satisfies Schemas['RecommendedMovieDto'][];

const forYouRecommendationsData = {
  items: [
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
      review: 'none',
      score: 0.91,
      evidence: [
        {
          criterion: 'Director',
          text: 'Same director: Denis Villeneuve',
          matchedNames: ['Denis Villeneuve'],
        },
        {
          criterion: 'Cast',
          text: 'Shared cast: Name One, Name Two, Name Three +2',
          matchedNames: ['Name One', 'Name Two', 'Name Three', 'Name Four', 'Name Five'],
        },
      ],
      seed: { movieId: 2, title: 'Dune', signal: 'watched' },
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
      review: 'none',
      score: 0.84,
      evidence: [],
      seed: { movieId: 1, title: 'Dune', signal: 'liked' },
      startedCollectionId: 1,
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
      review: 'none',
      score: 0.72,
      evidence: [{ criterion: 'Genre', text: 'Same genres: Drama', matchedNames: ['Drama'] }],
      seed: { movieId: 1, title: 'Dune', signal: 'watchlist' },
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
      review: 'none',
      score: 0.61,
      evidence: [
        { criterion: 'Themes', text: 'Shared themes: identity', matchedNames: ['identity'] },
      ],
      seed: { movieId: 2, title: 'Dune', signal: 'watched' },
    },
  ],
  hasMore: false,
  hasSeeds: true,
} satisfies Schemas['ForYouRecommendationsPageDto'];

const outsideLibraryRecommendationsData = [
  {
    tmdbId: 945961,
    title: 'Outside Library Movie',
    releaseYear: 1995,
    posterPath: '/outside-library.jpg',
    reason: 'Recommended for Heat and Collateral',
  },
] satisfies Schemas['OutsideLibraryRecommendation'][];

export const outsideLibraryRecommendations = Object.freeze(outsideLibraryRecommendationsData);

const genresData = [
  { id: 1, name: 'Action', movieCount: 10 },
  { id: 2, name: 'Sci-Fi', movieCount: 5 },
] satisfies Schemas['GenreDto'][];

const collectionsData = [
  { id: 1, name: 'Dune Collection', movieCount: 2 },
] satisfies Schemas['CollectionDto'][];

const hostileCollectionsData = [
  { id: 101, name: '<img src=x onerror=alert(1)> & "Dune"', movieCount: 2 },
] satisfies Schemas['CollectionDto'][];

const overlongCollectionsData = [
  { id: 102, name: 'C'.repeat(180), movieCount: 2 },
] satisfies Schemas['CollectionDto'][];

const peopleData = [
  { id: 1, name: 'Timothée Chalamet' },
  { id: 2, name: 'Rebecca Ferguson' },
] satisfies Schemas['PersonDto'][];

const directorsData = [
  { id: 1, name: 'Denis Villeneuve', movieCount: 2 },
  { id: 2, name: 'Christopher Nolan', movieCount: 1 },
] satisfies Schemas['DirectorDto'][];

const movieThumbnailData = '/9j/4AAQSkZJRg==';

const movieStreamData: Schemas['Stream'] = '';

const subtitleTracksData = [
  { id: 'f8a2c1', label: 'English', language: 'en', isDefault: true },
  { id: '9d3e70', label: 'Portuguese', language: 'pt', isDefault: false },
] satisfies Schemas['SubtitleTrackDto'][];

const subtitleContentData =
  'WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello there.\n\n00:00:03.000 --> 00:00:04.500\nGeneral Kenobi.\n';

const subtitleSidecarData =
  '1\n00:00:01,000 --> 00:00:02,000\nHello there.\n\n2\n00:00:03,000 --> 00:00:04,500\nGeneral Kenobi.\n';

const playbackCapabilitiesData = {
  localPlayEnabled: false,
} satisfies Schemas['PlaybackCapabilitiesDto'];

const rescanLibraryData = {
  changed: 1,
  unchanged: 1,
  failed: 0,
  failedMovieIds: [],
} satisfies Schemas['RescanLibraryResponse'];

const rescanMovieData = {
  movieId: 1,
  changed: true,
  oldFormat: 'avi',
  newFormat: 'mkv',
} satisfies Schemas['RescanMovieResponse'];

const listImportCandidatesData = {
  newFolders: [],
} satisfies Schemas['ImportCandidatesResponse'];

const importLibraryFoldersData = {
  imported: 0,
  skipped: 0,
  retried: 0,
  failures: [],
} satisfies Schemas['ImportLibraryResponse'];

export const browseMovies = Object.freeze(browseMoviesData);
export const movieDetails = Object.freeze(movieDetailsData);
export const movieDetailsNoTrailer = Object.freeze(movieDetailsNoTrailerData);
export const movieDetailsNullMetadata = Object.freeze(movieDetailsNullMetadataData);
export const recommendations = Object.freeze(recommendationsData);
export const relatedMovies = Object.freeze(relatedMoviesData);
export const forYouRecommendations = Object.freeze(forYouRecommendationsData);
export const genres = Object.freeze(genresData);
export const collections = Object.freeze(collectionsData);
export const hostileCollections = Object.freeze(hostileCollectionsData);
export const overlongCollections = Object.freeze(overlongCollectionsData);
export const people = Object.freeze(peopleData);
export const directors = Object.freeze(directorsData);
export const movieThumbnail = movieThumbnailData;
export const movieStream = movieStreamData;
export const subtitleTracks = Object.freeze(subtitleTracksData);

const subtitleLanguagesData = {
  defaultLanguage: 'pt-BR',
  languages: [
    { code: 'pt-BR', englishName: 'Portuguese (BR)', nativeName: 'Português (BR)' },
    { code: 'en', englishName: 'English', nativeName: 'English' },
  ],
} satisfies Schemas['SubtitleLanguagesResponse'];

export const subtitleLanguages = Object.freeze(subtitleLanguagesData);

const subtitleSearchData = {
  candidates: [
    {
      candidateId: 'f8a2c1d4e5b60718f8a2c1d4e5b60718',
      language: 'en',
      releaseName: 'Heat.1995.1080p.BluRay.x264-GROUP',
      matchPercent: 100,
      downloadCount: 42,
    },
  ],
  quota: { remainingDownloads: 17, resetAtUtc: '2026-03-04T05:06:07+00:00' },
  downloadAvailability: { enabled: true, reason: '' },
} satisfies Schemas['SubtitleSearchResponse'];

export const subtitleSearch = Object.freeze(subtitleSearchData);

const subtitleDownloadData = {
  track: {
    id: 'track-en-1080p',
    label: 'English',
    language: 'en',
    isDefault: false,
  },
  quota: { remainingDownloads: 16, resetAtUtc: '2026-03-04T05:06:07+00:00' },
} satisfies Schemas['SubtitleDownloadResponse'];

export const subtitleDownload = Object.freeze(subtitleDownloadData);
export const subtitleContent = subtitleContentData;
export const subtitleSidecar = subtitleSidecarData;
export const playbackCapabilities = Object.freeze(playbackCapabilitiesData);
export const rescanLibrary = Object.freeze(rescanLibraryData);
export const rescanMovie = Object.freeze(rescanMovieData);
export const listImportCandidates = Object.freeze(listImportCandidatesData);
export const importLibraryFolders = Object.freeze(importLibraryFoldersData);

const versionData = {
  release: null,
  commit: 'unknown',
} satisfies Schemas['VersionDto'];

export const version = Object.freeze(versionData);

export const success = Object.freeze({
  BrowseMovies: browseMovies,
  GetMovieDetails: movieDetails,
  GetRecommendations: recommendations,
  GetForYouRecommendations: forYouRecommendations,
  GetOutsideLibraryRecommendations: outsideLibraryRecommendations,
  GetGenres: genres,
  GetCollections: collections,
  GetPeople: people,
  GetDirectors: directors,
  GetMovieThumbnail: movieThumbnail,
  StreamMovie: movieStream,
  ListMovieSubtitleTracks: subtitleTracks,
  ListSubtitleLanguages: subtitleLanguages,
  SearchMovieSubtitles: subtitleSearch,
  DownloadMovieSubtitle: subtitleDownload,
  GetMovieSubtitleContent: subtitleContent,
  GetMovieSubtitleSidecar: subtitleSidecar,
  GetPlaybackCapabilities: playbackCapabilities,
  RescanLibrary: rescanLibrary,
  RescanMovie: rescanMovie,
  ListImportCandidates: listImportCandidates,
  ImportLibraryFolders: importLibraryFolders,
  GetVersion: version,
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
