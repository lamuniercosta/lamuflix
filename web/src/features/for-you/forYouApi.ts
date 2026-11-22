import { useQuery } from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';

export type ForYouRecommendation = components['schemas']['RecommendedMovieDto'];

export type ForYouSlice = components['schemas']['ForYouRecommendationsPageDto'];

export interface ForYouSliceRequest {
  take: number;
  offset: number;
  exclude?: number[];
  signal?: AbortSignal;
}

export const FOR_YOU_TAKE = 10;

export const forYouQueryKey = ['forYou', FOR_YOU_TAKE] as const;

export class ForYouError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`For You request failed with status ${status}`);
    this.status = status;
  }
}

export async function fetchForYouSlice({
  take,
  offset,
  exclude = [],
  signal,
}: ForYouSliceRequest): Promise<ForYouSlice> {
  const { data, error, response } = await client.GET('/api/recommendations/for-you', {
    params: { query: { take, offset, exclude } },
    signal,
  });
  if (error || data === undefined) {
    throw new ForYouError(response.status);
  }
  return { items: data.items, hasMore: data.hasMore };
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
      return data.items;
    },
  });
}
