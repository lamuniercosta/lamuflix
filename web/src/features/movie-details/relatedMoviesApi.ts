import { useQuery } from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';

export type RecommendedMovie = components['schemas']['RecommendedMovieDto'];
export type RecommendationMovieId = number | string;

export class RelatedMoviesError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`Related movies request failed with status ${status}`);
    this.status = status;
  }
}

export function toRecommendationMovieId(
  movieId: RecommendationMovieId | null | undefined,
): number | null {
  if (movieId === null || movieId === undefined) return null;
  const numeric = typeof movieId === 'number' ? movieId : Number(movieId);
  if (!Number.isInteger(numeric) || numeric <= 0) return null;
  return numeric;
}

export function relatedMoviesQueryKey(movieId: RecommendationMovieId | null | undefined) {
  const validId = toRecommendationMovieId(movieId);
  return ['relatedMovies', validId] as const;
}

export function useRelatedMovies(movieId: RecommendationMovieId | null | undefined) {
  const validId = toRecommendationMovieId(movieId);
  return useQuery<RecommendedMovie[], RelatedMoviesError>({
    queryKey: relatedMoviesQueryKey(validId),
    enabled: validId !== null,
    retry: false,
    queryFn: async ({ signal }) => {
      if (validId === null) {
        throw new RelatedMoviesError(0);
      }
      const { data, error, response } = await client.GET('/api/movies/{id}/recommendations', {
        params: { path: { id: validId } },
        signal,
      });
      if (error || data === undefined) {
        throw new RelatedMoviesError(response.status);
      }
      return data;
    },
  });
}
