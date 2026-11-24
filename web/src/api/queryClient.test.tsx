import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClientProvider, useMutation, useQuery } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { server } from '../test/server';
import ApiFailureToastHost from '../ui/ApiFailureToastHost';
import { createApiFailureToastQueue, type ApiFailureToastQueue } from '../ui/apiFailureToastQueue';
import { client } from './client';
import { createQueryClient } from './queryClient';
import { markApiTransportFailure } from './reportApiFailure';

const here = dirname(fileURLToPath(import.meta.url));
const mainSource = readFileSync(join(here, '..', 'main.tsx'), 'utf8');

function problemResponse(status: number, body: Record<string, unknown>) {
  return HttpResponse.json(body, {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  });
}

async function apiQuery() {
  let result;
  try {
    result = await client.GET('/api/genres');
  } catch (error) {
    throw markApiTransportFailure(error);
  }
  if (result.error !== undefined || result.response.status >= 400) {
    throw result;
  }
  return result.data;
}

async function apiMutation() {
  let result;
  try {
    result = await client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 7 } } });
  } catch (error) {
    throw markApiTransportFailure(error);
  }
  if (result.error !== undefined || result.response.status >= 400) {
    throw result;
  }
  return result.data;
}

function Harness() {
  const query = useQuery({ queryKey: ['api-failure'], queryFn: apiQuery, enabled: false });
  const mutation = useMutation({ mutationFn: apiMutation });
  const programmingQuery = useQuery({
    queryKey: ['programming-query'],
    queryFn: () => {
      throw new TypeError('programming query failed');
    },
    enabled: false,
  });
  const programmingMutation = useMutation({
    mutationFn: async () => {
      throw 'programming mutation failed';
    },
  });

  return (
    <>
      <button type="button" onClick={() => void query.refetch()}>
        Run query
      </button>
      <button type="button" onClick={() => mutation.mutate()}>
        Run mutation
      </button>
      <button type="button" onClick={() => void programmingQuery.refetch()}>
        Throw query
      </button>
      <button type="button" onClick={() => programmingMutation.mutate()}>
        Throw mutation
      </button>
    </>
  );
}

function toasts(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>('.lamu-toast'));
}

async function settle(): Promise<void> {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
}

let queue: ApiFailureToastQueue;
let queryClient: ReturnType<typeof createQueryClient>;
let publish: ReturnType<typeof vi.spyOn>;
let unsubscribe: (() => void) | undefined;

beforeEach(() => {
  queue = createApiFailureToastQueue();
  publish = vi.spyOn(queue, 'publish');
  queryClient = createQueryClient(queue);
  unsubscribe = queryClient.getQueryCache().subscribe(() => {});
  render(
    <QueryClientProvider client={queryClient}>
      <ApiFailureToastHost queue={queue} />
      <Harness />
    </QueryClientProvider>,
  );
});

afterEach(() => {
  unsubscribe?.();
  unsubscribe = undefined;
  queue.reset();
  vi.restoreAllMocks();
});

describe('configured QueryClient cache reporting', () => {
  it('reports a forced HTTP query failure once with response diagnostics', async () => {
    server.use(
      http.get('*/api/genres', () =>
        problemResponse(503, {
          status: 429,
          title: 'Service degraded',
          reason: 'retry later',
          traceId: 'trace-503',
          detail: 'internal detail must stay hidden',
        }),
      ),
    );

    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()).toHaveLength(1);
    expect(toasts()[0]).toHaveTextContent('Service degraded');
    expect(toasts()[0]).toHaveTextContent('Status: 503');
    expect(toasts()[0]).toHaveTextContent('retry later');
    expect(toasts()[0]).toHaveTextContent('trace-503');
    expect(document.body.textContent).not.toContain('internal detail must stay hidden');
  });

  it('reports a forced HTTP mutation failure once with an unavailable trace id', async () => {
    server.use(
      http.post('*/api/movies/:id/watchlist', () =>
        problemResponse(404, { status: 404, title: 'Movie missing', detail: 'hidden path' }),
      ),
    );

    fireEvent.click(screen.getByRole('button', { name: 'Run mutation' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()).toHaveLength(1);
    expect(toasts()[0]).toHaveTextContent('Movie missing');
    expect(toasts()[0]).toHaveTextContent('Status: 404');
    expect(toasts()[0]).toHaveTextContent('Trace ID: Unavailable');
    expect(document.body.textContent).not.toContain('hidden path');
  });

  it('reports a rejected fetch query once as a network error', async () => {
    server.use(http.get('*/api/genres', () => HttpResponse.error()));

    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()).toHaveLength(1);
    expect(toasts()[0]).toHaveTextContent('Network error');
    expect(toasts()[0]).toHaveTextContent('Status: Unavailable');
    expect(toasts()[0]).toHaveTextContent('Trace ID: Unavailable');
    expect(document.body.textContent).not.toContain('Failed to fetch');
  });

  it('reports a rejected fetch mutation once as a network error', async () => {
    server.use(http.post('*/api/movies/:id/watchlist', () => HttpResponse.error()));

    fireEvent.click(screen.getByRole('button', { name: 'Run mutation' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()).toHaveLength(1);
    expect(toasts()[0]).toHaveTextContent('Network error');
  });

  it('ignores unmarked programming throws from both caches', async () => {
    fireEvent.click(screen.getByRole('button', { name: 'Throw query' }));
    fireEvent.click(screen.getByRole('button', { name: 'Throw mutation' }));

    await settle();

    expect(publish).not.toHaveBeenCalled();
    expect(toasts()).toEqual([]);
    expect(document.body.textContent).not.toContain('programming query failed');
    expect(document.body.textContent).not.toContain('programming mutation failed');
  });

  it('keeps one publish per failure when a local cache error subscriber is present', async () => {
    server.use(
      http.get('*/api/genres', () =>
        problemResponse(500, { status: 500, title: 'Broken', traceId: 'trace-local' }),
      ),
    );
    const localErrors = new Set<unknown>();
    const localUnsubscribe = queryClient.getQueryCache().subscribe((event) => {
      if ('query' in event && event.query.state.status === 'error') {
        localErrors.add(event.query);
      }
    });

    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(localErrors.size).toBe(1);
    localUnsubscribe();
  });

  it('caps concurrent cache failures at three visible toasts without losing reports', async () => {
    const evidence = (reason: string) => ({
      response: new Response(null, { status: 500 }),
      error: { status: 500, title: 'Burst', reason, traceId: reason },
    });
    const failures = [
      queryClient.fetchQuery({
        queryKey: ['burst-query-1'],
        queryFn: async () => {
          throw evidence('burst-one');
        },
      }),
      queryClient.fetchQuery({
        queryKey: ['burst-query-2'],
        queryFn: async () => {
          throw evidence('burst-two');
        },
      }),
      ...[3, 4].map((index) =>
        queryClient
          .getMutationCache()
          .build(queryClient, {
            mutationFn: async () => {
              throw evidence(`burst-${index}`);
            },
          })
          .execute(undefined),
      ),
    ];

    await Promise.allSettled(failures);
    await settle();

    expect(publish).toHaveBeenCalledTimes(4);
    expect(toasts()).toHaveLength(3);
    expect(screen.getByRole('status').children).toHaveLength(3);
  });

  it('does not report an intentionally cancelled query', async () => {
    const pending = queryClient.fetchQuery({
      queryKey: ['cancelled-query'],
      queryFn: ({ signal }) =>
        new Promise<never>((_resolve, reject) => {
          signal.addEventListener('abort', () => {
            reject(new DOMException('Aborted', 'AbortError'));
          });
        }),
    });

    await queryClient.cancelQueries({ queryKey: ['cancelled-query'] });
    await pending.catch(() => undefined);
    await settle();

    expect(publish).not.toHaveBeenCalled();
    expect(toasts()).toEqual([]);
  });

  it('uses the configured factory with retry disabled in the app entry module', () => {
    expect(queryClient.getDefaultOptions().queries?.retry).toBe(false);
    expect(mainSource).toContain('createQueryClient()');
    expect(mainSource).not.toContain('new QueryClient(');
  });
});
