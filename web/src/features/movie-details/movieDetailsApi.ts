import { useMutation, useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { client } from '../../api/client';
import type { components } from '../../api/types';

type MovieMetadataResponse = components['schemas']['MovieMetadataResponse'];
type MovieDetailsResponse = components['schemas']['MovieDetailsResponse'];

const movieMetadataSchema = z.object({
  title: z.string(),
  synopsis: z.string().nullable(),
  releaseYear: z.union([z.number(), z.string()]).nullable(),
  runtime: z.union([z.number(), z.string()]).nullable(),
  imdbRating: z.union([z.number(), z.string()]).nullable(),
  imdbId: z.string().nullable(),
  posterUrl: z.string().nullable(),
  rottenTomatoesRating: z.union([z.number(), z.string()]).nullable(),
  metaScore: z.union([z.number(), z.string()]).nullable(),
  directors: z.array(z.string()),
  actors: z.array(z.string()),
}) satisfies z.ZodType<MovieMetadataResponse>;

const movieDetailsSchema = z.object({
  id: z.union([z.number(), z.string()]),
  title: z.string(),
  path: z.string(),
  format: z.string(),
  isInWatchlist: z.boolean(),
  metadata: movieMetadataSchema.nullable(),
}) satisfies z.ZodType<MovieDetailsResponse>;

export type MovieDetails = MovieDetailsResponse;
export type MovieMetadata = MovieMetadataResponse;

export class MovieDetailsError extends Error {
  status: number;
  constructor(status: number) {
    super(`Movie details request failed with status ${status}`);
    this.status = status;
  }
}

function toNumber(value: number | string | null): number | null {
  if (value === null) return null;
  const numeric = Number(value);
  return Number.isFinite(numeric) ? numeric : null;
}

export function formatRuntime(runtime: number | string | null): string {
  const minutes = toNumber(runtime);
  if (minutes === null) return '';
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  return `${hours}h ${remainder}m`;
}

export function useMovieDetails(movieId: number | string) {
  return useQuery<MovieDetails, MovieDetailsError>({
    queryKey: ['movieDetails', movieId],
    queryFn: async () => {
      const { data, error, response } = await client.GET('/api/movies/{id}', {
        params: { path: { id: Number(movieId) } },
      });
      if (response.status === 404) {
        throw new MovieDetailsError(404);
      }
      if (error || data === undefined) {
        throw new MovieDetailsError(response.status || 500);
      }
      try {
        return movieDetailsSchema.parse(data);
      } catch {
        throw new MovieDetailsError(response.status || 500);
      }
    },
    retry: false,
  });
}

/**
 * Deletes a movie through the generated client. A 204 (removed) and a 404 (already absent) are both
 * treated as a settled outcome; any other failure rejects with `MovieDetailsError`.
 */
export function useDeleteMovie(movieId: number | string) {
  return useMutation<number, MovieDetailsError, void>({
    mutationFn: async () => {
      const { response } = await client.DELETE('/api/movies/{id}', {
        params: { path: { id: Number(movieId) } },
      });
      if (response.status === 204 || response.status === 404) {
        return response.status;
      }
      throw new MovieDetailsError(response.status);
    },
  });
}

export function usePlayMovie(movieId: number | string) {
  return useMutation<number, MovieDetailsError, void>({
    mutationFn: async () => {
      const { response } = await client.POST('/api/movies/{id}/play', {
        params: { path: { id: Number(movieId) } },
      });
      if (!response.ok) {
        throw new MovieDetailsError(response.status);
      }
      return response.status;
    },
  });
}
