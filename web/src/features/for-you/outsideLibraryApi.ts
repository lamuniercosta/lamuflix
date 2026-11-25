import { useQuery } from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';

export type OutsideLibraryCard = components['schemas']['OutsideLibraryRecommendation'];

export const outsideLibraryQueryKey = ['outsideLibraryRecommendations'] as const;

export class OutsideLibraryError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`Outside-library recommendation request failed with status ${status}`);
    this.status = status;
  }
}

export async function fetchOutsideLibraryRecommendations({
  signal,
}: { signal?: AbortSignal } = {}): Promise<OutsideLibraryCard[]> {
  const { data, error, response } = await client.GET('/api/recommendations/not-in-library', {
    signal,
  });
  const status = response.status;
  if (error || data === undefined) {
    throw new OutsideLibraryError(status);
  }
  return data;
}

export function useOutsideLibraryRecommendations() {
  return useQuery<OutsideLibraryCard[], OutsideLibraryError>({
    queryKey: outsideLibraryQueryKey,
    retry: false,
    queryFn: async ({ signal }) => fetchOutsideLibraryRecommendations({ signal }),
  });
}
