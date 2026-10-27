import { useCallback, useEffect, useSyncExternalStore } from 'react';
import {
  useMutation,
  useQuery,
  useQueryClient,
  type QueryCache,
  type QueryClient,
} from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';
import { markRowPending } from '../../library/libraryQueries';
import type { EnrichmentStatusSubscription } from '../../library/useEnrichmentStatus';

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type MovieSummary = components['schemas']['MovieSummaryResponse'];

export type EnrichmentStatus = MovieSummary['enrichmentStatus'];
export type ActorId = number | string;

export const MORE_WITH_PAGE_SIZE = 20;

export function useGenresLookup() {
  return useQuery({
    queryKey: ['detailsGenres'],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/genres', { signal });
      const status = response.status;
      if (error || !data) {
        throw new Error(`Genres request failed (${status})`);
      }
      return data;
    },
  });
}

export function usePeopleLookup() {
  return useQuery({
    queryKey: ['detailsPeople'],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/people', { signal });
      if (error || !data) {
        throw new Error(`People request failed (${response.status})`);
      }
      return data;
    },
  });
}

export function useMoreWithActors(actorIds: readonly ActorId[], enabled: boolean) {
  const ids = [...new Set(actorIds.map(String))].sort();
  return useQuery({
    queryKey: ['moreWith', ids],
    enabled: enabled && ids.length > 0,
    queryFn: async ({ signal }) => {
      const { data, error, response } = await client.GET('/api/movies', {
        params: {
          query: {
            ActorIds: ids,
            sort: 'Title',
            direction: 'Ascending',
            page: 1,
            pageSize: MORE_WITH_PAGE_SIZE,
          },
        },
        signal,
      });
      if (error || !data) {
        throw new Error(`More-with request failed (${response.status})`);
      }
      return data.items;
    },
  });
}

export class EnrichmentRequestError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`Enrichment request failed with status ${status}`);
    this.status = status;
  }
}

async function postEnrichment(movieId: ActorId): Promise<number> {
  try {
    const { response } = await client.POST('/api/movies/{id}/enrichment', {
      params: { path: { id: Number(movieId) } },
      parseAs: 'text',
    });
    return response.status;
  } catch {
    return 0;
  }
}

export function useRequestEnrichment(movieId: ActorId, libraryClient: QueryClient) {
  const detailsClient = useQueryClient();
  return useMutation<{ id: ActorId }, EnrichmentRequestError, void>({
    mutationFn: async () => {
      const status = await postEnrichment(movieId);
      if (status !== 202) {
        throw new EnrichmentRequestError(status);
      }
      return { id: movieId };
    },
    onSuccess: ({ id }) => {
      libraryClient.setQueriesData<BrowseMoviesResponse>(
        { queryKey: ['browseMovies'] },
        (current) => markRowPending(current, id),
      );
      void detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] });
    },
  });
}

export class MatchImdbIdError extends Error {
  readonly status: number;
  readonly detail: string | null;

  constructor(status: number, detail: string | null = null) {
    super(`IMDb match request failed with status ${status}`);
    this.status = status;
    this.detail = detail;
  }
}

type ImdbMatchOutcome = { status: number; detail: string | null };

/** Reads the RFC 7807 `detail` from an already-parsed error body; anything else yields null. */
function problemDetailText(body: unknown): string | null {
  if (typeof body !== 'object' || body === null) return null;
  const { detail } = body as { detail?: unknown };
  return typeof detail === 'string' && detail.trim() !== '' ? detail : null;
}

async function putImdbMatch(movieId: ActorId, imdbId: string): Promise<ImdbMatchOutcome> {
  try {
    const { response, error } = await client.PUT('/api/movies/{id}/imdb', {
      params: { path: { id: Number(movieId) } },
      body: { imdbId },
      parseAs: 'text',
    });
    const detail = response.status === 409 ? problemDetailText(error) : null;
    return { status: response.status, detail };
  } catch {
    return { status: 0, detail: null };
  }
}

export function useMatchImdbId(movieId: ActorId, libraryClient: QueryClient) {
  const detailsClient = useQueryClient();
  return useMutation<{ id: ActorId }, MatchImdbIdError, string>({
    mutationFn: async (imdbId) => {
      const { status, detail } = await putImdbMatch(movieId, imdbId);
      if (status !== 202) {
        throw new MatchImdbIdError(status, detail);
      }
      return { id: movieId };
    },
    onSuccess: ({ id }) => {
      libraryClient.setQueriesData<BrowseMoviesResponse>(
        { queryKey: ['browseMovies'] },
        (current) => markRowPending(current, id),
      );
      void detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] });
    },
  });
}

export function readLibraryEnrichmentStatus(
  cache: QueryCache,
  movieId: ActorId,
): EnrichmentStatus | null {
  for (const query of cache.findAll({ queryKey: ['browseMovies'] })) {
    const page = query.state.data as BrowseMoviesResponse | undefined;
    const match = page?.items.find((item) => String(item.id) === String(movieId));
    if (match) {
      return match.enrichmentStatus;
    }
  }
  return null;
}

export function useLibraryEnrichmentStatus(
  libraryClient: QueryClient,
  movieId: ActorId,
): EnrichmentStatus | null {
  const cache = libraryClient.getQueryCache();
  const subscribe = useCallback((onChange: () => void) => cache.subscribe(onChange), [cache]);
  const snapshot = useCallback(() => readLibraryEnrichmentStatus(cache, movieId), [cache, movieId]);
  return useSyncExternalStore(subscribe, snapshot, snapshot);
}

/** One-way bridge: root hub notifications reconcile the mounted details query without a second connection. */
export function useDetailsEnrichmentBridge(
  subscription: EnrichmentStatusSubscription | undefined,
  detailsClient: QueryClient,
  movieId: ActorId,
): void {
  useEffect(() => {
    if (!subscription) {
      return undefined;
    }
    return subscription.subscribe((notification) => {
      if (notification.kind === 'event' && String(notification.payload.id) !== String(movieId)) {
        return;
      }
      void detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] }).catch(() => {});
    });
  }, [subscription, detailsClient, movieId]);
}
