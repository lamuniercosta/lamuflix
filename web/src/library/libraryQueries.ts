import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { client } from '../api/client';
import type { components, paths } from '../api/types';

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type MovieSummary = components['schemas']['MovieSummaryResponse'];

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

export const GENERIC_IMPORT_ERROR = 'Something went wrong. Try again.';
export const GENERIC_RETRY_ERROR = 'Something went wrong. Try again.';

export class ImportFieldError extends Error {
  readonly kind = 'field' as const;
}

export class ImportGenericError extends Error {
  readonly kind = 'generic' as const;
}

export class RetryRowError extends Error {
  readonly kind = 'row' as const;
}

export function lastFolderSegment(folderPath: string): string {
  const trimmed = folderPath.replace(/[\\/]+$/, '');
  const parts = trimmed.split(/[\\/]/).filter(Boolean);
  return parts.at(-1) ?? trimmed;
}

export function movieIdFromLocation(location: string | null): string | null {
  if (!location) {
    return null;
  }

  const match = location.match(/\/api\/movies\/([^/?#]+)/);
  return match?.[1] ?? null;
}

function folderPathProblemMessage(error: unknown): string | undefined {
  if (!error || typeof error !== 'object' || !('errors' in error)) {
    return undefined;
  }

  const errors = (error as { errors?: Record<string, unknown> }).errors;
  const folderPath = errors?.folderPath;
  if (Array.isArray(folderPath) && typeof folderPath[0] === 'string') {
    return folderPath[0];
  }

  return undefined;
}

let importPlaceholderPending = false;
let importPlaceholderToken = 0;
let onImportPlaceholderReconciled: (() => void) | null = null;

export function armImportPlaceholder(onReconciled: () => void): void {
  importPlaceholderToken += 1;
  importPlaceholderPending = true;
  onImportPlaceholderReconciled = onReconciled;
}

export function isImportPlaceholderPending(): boolean {
  return importPlaceholderPending;
}

export function clearImportPlaceholder(): void {
  importPlaceholderPending = false;
  importPlaceholderToken = 0;
  onImportPlaceholderReconciled = null;
}

function reconcileImportPlaceholder(): void {
  if (!importPlaceholderPending) {
    return;
  }

  importPlaceholderPending = false;
  const notify = onImportPlaceholderReconciled;
  onImportPlaceholderReconciled = null;
  notify?.();
}

export function browseRefetchInterval(query: {
  state: { data?: BrowseMoviesResponse };
}): number | false {
  if (importPlaceholderPending) {
    return 2000;
  }

  const items = query.state.data?.items ?? [];
  return items.some((item) => item.enrichmentStatus === 'Pending') ? 2000 : false;
}

export function pendingRow(id: string, title: string): MovieSummary {
  return {
    id,
    title,
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Pending',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
  };
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

export function useBrowseMoviesQuery(
  placeholderPending = false,
  inWatchlist = false,
  filters: Record<string, unknown> = {},
) {
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
      const tokenAtStart = importPlaceholderToken;
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

      if (tokenAtStart !== 0 && tokenAtStart === importPlaceholderToken) {
        reconcileImportPlaceholder();
      }
      return data;
    },
    retry: false,
    placeholderData: keepPreviousData,
    refetchOnWindowFocus: false,
    refetchInterval: placeholderPending
      ? (query: { state: { data?: BrowseMoviesResponse } }) => browseRefetchInterval(query)
      : browseRefetchInterval,
    refetchIntervalInBackground: false,
  });
}

export function useImportMovieMutation() {
  return useMutation({
    mutationFn: async (folderPath: string) => {
      let error: unknown;
      let response: Response;

      try {
        const result = await client.POST('/api/movies/import', {
          body: { folderPath },
          parseAs: 'text',
        });
        error = result.error;
        response = result.response;
      } catch {
        throw new ImportGenericError(GENERIC_IMPORT_ERROR);
      }

      if (response.status === 202) {
        const id = movieIdFromLocation(response.headers.get('Location'));
        if (id == null) {
          throw new ImportGenericError(GENERIC_IMPORT_ERROR);
        }

        return { id, title: lastFolderSegment(folderPath) };
      }

      if (response.status === 422) {
        throw new ImportFieldError(folderPathProblemMessage(error) ?? GENERIC_IMPORT_ERROR);
      }

      throw new ImportGenericError(GENERIC_IMPORT_ERROR);
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
    },
    onError: () => {
      void queryClient.invalidateQueries({ queryKey: browseMoviesQueryKey });
    },
  });
}
