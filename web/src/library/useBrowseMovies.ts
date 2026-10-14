import { useQuery } from '@tanstack/react-query';
import type { UseQueryOptions, UseQueryResult } from '@tanstack/react-query';
import { client } from '../api/client';
import type { components } from '../api/types';
import { toApiParams } from './libraryUrl';
import type { LibraryUrlState } from './libraryUrl';

type ProblemDetails = components['schemas']['ProblemDetails'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type BrowseMoviesQuery = Record<string, unknown>;

export class BrowseMoviesError extends Error {
  status: number;
  problem?: ProblemDetails;
  response?: Response;

  constructor(status: number, problem?: ProblemDetails, response?: Response) {
    super(problem?.detail ?? (status ? `HTTP ${status}` : 'Network error'));
    this.name = 'BrowseMoviesError';
    this.status = status;
    this.problem = problem;
    this.response = response;
  }

  get invalidFilter(): boolean {
    return this.status === 422;
  }
}

function isLibraryUrlState(value: unknown): value is Partial<LibraryUrlState> {
  if (!value || typeof value !== 'object') return false;
  const v = value as Record<string, unknown>;
  return (
    Array.isArray(v.genreIds) ||
    typeof v.runtimeIncludeUnknown === 'boolean' ||
    typeof v.page === 'number' ||
    'statuses' in v
  );
}

function extractParams(input: unknown): BrowseMoviesQuery {
  if (!input || typeof input !== 'object') return {} as BrowseMoviesQuery;

  const record = input as Record<string, unknown>;
  const candidate =
    record.params ??
    record.state ??
    record.filters ??
    record.normalizedState ??
    record.query ??
    record.apiParams ??
    record.generatedParams;

  const source = candidate && typeof candidate === 'object' ? candidate : record;

  if (isLibraryUrlState(source)) {
    return toApiParams(source as Partial<LibraryUrlState>) as unknown as BrowseMoviesQuery;
  }

  return { ...source } as unknown as BrowseMoviesQuery;
}

export function classifyBrowseError(error: unknown): BrowseMoviesError {
  if (error instanceof BrowseMoviesError) return error;
  if (error && typeof error === 'object') {
    const status =
      (error as { status?: number }).status ??
      (error as { response?: { status?: number } }).response?.status ??
      0;
    const problem = (error as { problem?: ProblemDetails }).problem ?? (error as ProblemDetails);
    return new BrowseMoviesError(status, problem);
  }
  return new BrowseMoviesError(0);
}

export function useBrowseMovies(
  input?: unknown,
  options?: Omit<UseQueryOptions<BrowseMoviesResponse, BrowseMoviesError>, 'queryKey' | 'queryFn'>,
): UseQueryResult<BrowseMoviesResponse, BrowseMoviesError> {
  const params = extractParams(input);

  return useQuery<BrowseMoviesResponse, BrowseMoviesError>({
    queryKey: ['browseMovies', params],
    queryFn: async ({ signal }) => {
      const apiClient = client as unknown as {
        GET: (path: string, init?: unknown) => Promise<unknown>;
      };
      const result = (await apiClient.GET('/api/movies', {
        params: { query: params },
        signal,
      })) as {
        data?: BrowseMoviesResponse;
        error?: ProblemDetails;
        response?: Response;
      };
      const data = result.data;
      const error = result.error;
      const response = result.response;

      const status = (response as Response | undefined)?.status ?? 0;
      if (response && !response.ok) {
        throw new BrowseMoviesError(
          status,
          error as ProblemDetails | undefined,
          response as Response,
        );
      }
      if (error) {
        const status = response?.status ?? 0;
        throw new BrowseMoviesError(status, error as ProblemDetails, response);
      }
      if (!data) {
        throw new BrowseMoviesError(response?.status ?? 0, undefined, response);
      }
      return data;
    },
    retry: false,
    ...options,
  });
}

export default useBrowseMovies;
