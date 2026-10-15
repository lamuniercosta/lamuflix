import { useQuery } from '@tanstack/react-query';
import { client } from '../api/client';
import type { paths } from '../api/types';

export const standingBrowseQuery = {
  sort: 'Title',
  direction: 'Ascending',
  page: 1,
  pageSize: 20,
} as const satisfies Pick<
  paths['/api/movies']['get']['parameters']['query'],
  'sort' | 'direction' | 'page' | 'pageSize'
>;

export const browseMoviesQueryKey = ['browseMovies', standingBrowseQuery] as const;

export function useBrowseMoviesQuery() {
  return useQuery({
    queryKey: browseMoviesQueryKey,
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/movies', {
        params: { query: { ...standingBrowseQuery } },
        signal,
      });
      if (error || !data) {
        throw new Error(error?.detail ?? `Browse failed (${response.status})`);
      }
      return data;
    },
    retry: false,
    refetchOnWindowFocus: false,
  });
}
