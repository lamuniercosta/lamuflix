const TMDB_MOVIE_PAGE_BASE = 'https://www.themoviedb.org/movie/';
const TMDB_IMAGE_BASE_AND_SIZE = 'https://image.tmdb.org/t/p/w500/';
const SAFE_POSTER_SEGMENT = /^[A-Za-z0-9._-]+$/;

export function tmdbMoviePageUrl(tmdbId: number | string): string | null {
  const value = typeof tmdbId === 'string' ? tmdbId.trim() : tmdbId;
  if (typeof value === 'string' && !/^\d+$/.test(value)) {
    return null;
  }

  const id = Number(value);
  return Number.isSafeInteger(id) && id > 0 ? `${TMDB_MOVIE_PAGE_BASE}${id.toString()}` : null;
}

export function tmdbPosterUrl(posterPath: string | null | undefined): string | null {
  if (typeof posterPath !== 'string' || posterPath.length === 0) {
    return null;
  }

  const segment = posterPath.startsWith('/') ? posterPath.slice(1) : posterPath;
  if (
    segment.length === 0 ||
    !SAFE_POSTER_SEGMENT.test(segment) ||
    segment === '.' ||
    segment === '..'
  ) {
    return null;
  }

  return `${TMDB_IMAGE_BASE_AND_SIZE}${encodeURIComponent(segment)}`;
}
