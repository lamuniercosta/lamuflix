import type { paths } from '../../api/types';

type MovieStreamPath = keyof paths & '/api/movies/{id}/stream';

const MOVIE_STREAM_PATH: MovieStreamPath = '/api/movies/{id}/stream';

export function movieStreamUrl(movieId: number | string): string {
  return MOVIE_STREAM_PATH.replace('{id}', String(movieId));
}
