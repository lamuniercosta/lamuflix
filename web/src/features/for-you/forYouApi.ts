import { useQuery } from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';

export type ForYouRecommendation = components['schemas']['RecommendedMovieDto'];

export const FOR_YOU_TAKE = 10;

export const forYouQueryKey = ['forYou', FOR_YOU_TAKE] as const;

export class ForYouError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`For You request failed with status ${status}`);
    this.status = status;
  }
}

export function useForYou() {
  return useQuery<ForYouRecommendation[], ForYouError>({
    queryKey: forYouQueryKey,
    retry: false,
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/recommendations/for-you', {
        params: { query: { take: FOR_YOU_TAKE } },
        signal,
      });
      if (error || data === undefined) {
        throw new ForYouError(response.status);
      }
      return data;
    },
  });
}
