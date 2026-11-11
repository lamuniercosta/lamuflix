import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { client } from '../api/client';
import type { components, paths } from '../api/types';

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];

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

export const GENERIC_RETRY_ERROR = 'Something went wrong. Try again.';
export const GENERIC_BULK_IMPORT_ERROR = 'Something went wrong. Try again.';

export const BULK_IMPORT_REASON_TEXT: Record<string, string> = {
  'folder-not-found': 'That folder could not be found. It may have been removed.',
  'no-video': 'No playable video was found in that folder.',
  'invalid-folder': 'That folder path is not valid.',
  'import-failed': 'That folder could not be imported.',
};

export type ImportCandidates = components['schemas']['ImportCandidatesResponse'];
export type BulkImportSummary = components['schemas']['ImportLibraryResponse'];

export class RetryRowError extends Error {
  readonly kind = 'row' as const;
}

export function invalidateBrowseMovies(queryClient: QueryClient): void {
  void queryClient.invalidateQueries({ queryKey: ['browseMovies'] }).catch(() => {});
}

export function invalidateImportSources(queryClient: QueryClient): void {
  void queryClient.invalidateQueries({ queryKey: ['importCandidates'] }).catch(() => {});
  void queryClient.invalidateQueries({ queryKey: ['failedEnrichments'] }).catch(() => {});
}

export function markRowPending(
  current: BrowseMoviesResponse | undefined,
  id: number | string,
): BrowseMoviesResponse | undefined {
  if (!current) {
    return current;
  }

  return {
    ...current,
    items: current.items.map((item) =>
      String(item.id) === String(id) ? { ...item, enrichmentStatus: 'Pending' } : item,
    ),
  };
}

export interface BrowseMoviesQueryError extends Error {
  status?: number;
  invalidFilter?: boolean;
}

export function useBrowseMoviesQuery(inWatchlist = false, filters: Record<string, unknown> = {}) {
  const merged = {
    ...standingBrowseQuery,
    ...filters,
  };
  const { sort, direction, page, pageSize, ...extras } = merged;
  const browseParams = {
    ...extras,
    sort,
    direction,
    page,
    pageSize,
    ...(inWatchlist ? { InWatchlist: 'true' as const } : {}),
  };

  return useQuery({
    queryKey: ['browseMovies', browseParams],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/movies', {
        params: {
          query: browseParams as unknown as NonNullable<
            paths['/api/movies']['get']['parameters']['query']
          >,
        },
        signal,
      });
      if (error || !data) {
        const status = response?.status ?? 0;
        const failure = new Error(
          error?.detail ?? `Browse failed (${status})`,
        ) as BrowseMoviesQueryError;
        failure.status = status;
        if (status === 422) {
          failure.invalidFilter = true;
        }
        throw failure;
      }

      return data;
    },
    retry: false,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
  });
}

export function useImportCandidatesQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['importCandidates'],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/movies/import/candidates', {
        signal,
      });
      if (error || !data) {
        throw new Error(error?.detail ?? `Candidates failed (${response?.status ?? 0})`);
      }
      return data;
    },
    enabled,
    retry: false,
    refetchOnWindowFocus: false,
  });
}

export function useFailedEnrichmentsQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['failedEnrichments'],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/movies', {
        params: {
          query: {
            ...standingBrowseQuery,
            Statuses: ['Failed'],
          } as unknown as NonNullable<paths['/api/movies']['get']['parameters']['query']>,
        },
        signal,
      });
      if (error || !data) {
        throw new Error(error?.detail ?? `Failed movies failed (${response?.status ?? 0})`);
      }
      return data;
    },
    enabled,
    retry: false,
    refetchOnWindowFocus: false,
  });
}

export function useImportLibraryFoldersMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: { folders: string[]; retryFailed: boolean }) => {
      const { data, error, response } = await client.POST('/api/movies/import/library', {
        body: { folders: input.folders, retryFailed: input.retryFailed },
      });
      if (error || !data) {
        throw new Error(error?.detail ?? `Bulk import failed (${response?.status ?? 0})`);
      }
      return data;
    },
    onSuccess: () => {
      invalidateBrowseMovies(queryClient);
      invalidateImportSources(queryClient);
    },
  });
}

export function useRetryEnrichmentMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: number | string) => {
      let response: Response;

      try {
        const result = await client.POST('/api/movies/{id}/enrichment', {
          params: { path: { id } },
          parseAs: 'text',
        });
        response = result.response;
      } catch {
        throw new RetryRowError(GENERIC_RETRY_ERROR);
      }

      if (response.status === 202) {
        return { id };
      }

      throw new RetryRowError(GENERIC_RETRY_ERROR);
    },
    onSuccess: ({ id }) => {
      queryClient.setQueriesData<BrowseMoviesResponse>({ queryKey: ['browseMovies'] }, (current) =>
        markRowPending(current, id),
      );
      invalidateBrowseMovies(queryClient);
    },
    onError: () => {
      void queryClient.invalidateQueries({ queryKey: browseMoviesQueryKey });
    },
  });
}
