import { QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { useEffect, type ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { server } from '../test/server';
import ApiFailureToastHost from '../ui/ApiFailureToastHost';
import { createApiFailureToastQueue, type ApiFailureToastQueue } from '../ui/apiFailureToastQueue';
import { createQueryClient } from './queryClient';
import {
  bulkImportErrorMessage,
  BulkImportError,
  GENERIC_RETRY_ERROR,
  RetryRowError,
  useBrowseMoviesQuery,
  useImportLibraryFoldersMutation,
  useRetryEnrichmentMutation,
  type BrowseMoviesQueryError,
} from '../library/libraryQueries';

function problemResponse(status: number, body: Record<string, unknown>) {
  return HttpResponse.json(body, {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  });
}

function BrowseProbe({ onError }: { onError: (error: BrowseMoviesQueryError) => void }) {
  const query = useBrowseMoviesQuery();
  const { error } = query;
  useEffect(() => {
    if (error) {
      onError(error as BrowseMoviesQueryError);
    }
  }, [error, onError]);
  return null;
}

function RetryProbe({ onError }: { onError: (error: Error) => void }) {
  const mutation = useRetryEnrichmentMutation();
  return (
    <button
      type="button"
      onClick={() => {
        mutation.mutate(7, { onError });
      }}
    >
      Retry seven
    </button>
  );
}

function BulkProbe({ onError }: { onError: (error: Error) => void }) {
  const mutation = useImportLibraryFoldersMutation();
  return (
    <button
      type="button"
      onClick={() => {
        mutation.mutate({ folders: ['/media'], retryFailed: false }, { onError });
      }}
    >
      Import library
    </button>
  );
}

let queue: ApiFailureToastQueue;
let queryClient: ReturnType<typeof createQueryClient>;
let publish: ReturnType<typeof vi.spyOn>;

function renderHarness(children: ReactNode) {
  return render(
    <QueryClientProvider client={queryClient}>
      <ApiFailureToastHost queue={queue} />
      {children}
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  queue = createApiFailureToastQueue();
  publish = vi.spyOn(queue, 'publish');
  queryClient = createQueryClient(queue);
});

afterEach(() => {
  queue.reset();
  vi.restoreAllMocks();
});

function toasts(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>('.lamu-toast'));
}

describe('library production failure reporting', () => {
  it('reports a browse 503 with response diagnostics while keeping the inline status', async () => {
    const seen: BrowseMoviesQueryError[] = [];
    server.use(
      http.get('*/api/movies', () =>
        problemResponse(503, {
          status: 503,
          title: 'Service degraded',
          reason: 'retry later',
          traceId: 'trace-503',
        }),
      ),
    );
    renderHarness(
      <BrowseProbe
        onError={(error) => {
          seen.push(error);
        }}
      />,
    );

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()).toHaveLength(1);
    expect(toasts()[0]).toHaveTextContent('Service degraded');
    expect(toasts()[0]).toHaveTextContent('Status: 503');
    expect(toasts()[0]).toHaveTextContent('retry later');
    expect(toasts()[0]).toHaveTextContent('trace-503');
    await waitFor(() => expect(seen.length).toBeGreaterThan(0));
    expect(seen[0].status).toBe(503);
  });

  it('reports a rejected browse fetch as a network error', async () => {
    server.use(http.get('*/api/movies', () => HttpResponse.error()));
    renderHarness(<BrowseProbe onError={() => {}} />);

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()).toHaveLength(1);
    expect(toasts()[0]).toHaveTextContent('Network error');
  });

  it('does not report HTTP 200 invalid JSON as a network error', async () => {
    const seen: unknown[] = [];
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.text('<html>proxy login</html>', {
          status: 200,
          headers: { 'Content-Type': 'text/html' },
        }),
      ),
    );
    renderHarness(
      <BrowseProbe
        onError={(error) => {
          seen.push(error);
        }}
      />,
    );

    await waitFor(() => expect(seen.length).toBeGreaterThan(0));
    expect(seen[0]).toBeInstanceOf(SyntaxError);
    expect(toasts()).toHaveLength(0);
    expect(publish).not.toHaveBeenCalled();
  });

  it('does not report an HTTP 200 body-decoding TypeError as a network error', async () => {
    const seen: unknown[] = [];
    const decodeError = new TypeError('terminated', { cause: { code: 'Z_DATA_ERROR' } });
    const response = new Response('{}', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
    vi.spyOn(response, 'json').mockRejectedValue(decodeError);
    vi.spyOn(response, 'text').mockRejectedValue(decodeError);
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(response);
    try {
      renderHarness(
        <BrowseProbe
          onError={(error) => {
            seen.push(error);
          }}
        />,
      );

      await waitFor(() => expect(seen.length).toBeGreaterThan(0));
      expect(seen[0]).toBe(decodeError);
      expect(toasts()).toHaveLength(0);
      expect(publish).not.toHaveBeenCalled();
    } finally {
      fetchSpy.mockRestore();
    }
  });

  it('keeps the invalid-filter marker on a browse 422 alongside the toast', async () => {
    const seen: BrowseMoviesQueryError[] = [];
    server.use(
      http.get('*/api/movies', () =>
        problemResponse(422, { status: 422, title: 'Invalid filter', traceId: 'trace-422' }),
      ),
    );
    renderHarness(
      <BrowseProbe
        onError={(error) => {
          seen.push(error);
        }}
      />,
    );

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()[0]).toHaveTextContent('Status: 422');
    await waitFor(() => expect(seen.length).toBeGreaterThan(0));
    expect(seen[0].status).toBe(422);
    expect(seen[0].invalidFilter).toBe(true);
  });

  it('reports a retry 404 while the row keeps its retry error', async () => {
    const seen: RetryRowError[] = [];
    server.use(
      http.post('*/api/movies/:id/enrichment', () =>
        problemResponse(404, { status: 404, title: 'Movie missing' }),
      ),
    );
    renderHarness(
      <RetryProbe
        onError={(error) => {
          seen.push(error as RetryRowError);
        }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Retry seven' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()[0]).toHaveTextContent('Status: 404');
    await waitFor(() => expect(seen.length).toBe(1));
    expect(seen[0]).toBeInstanceOf(RetryRowError);
    expect(seen[0].message).toBe(GENERIC_RETRY_ERROR);
  });

  it('reports a rejected retry fetch as a network error without losing the row error', async () => {
    const seen: RetryRowError[] = [];
    server.use(http.post('*/api/movies/:id/enrichment', () => HttpResponse.error()));
    renderHarness(
      <RetryProbe
        onError={(error) => {
          seen.push(error as RetryRowError);
        }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Retry seven' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()[0]).toHaveTextContent('Network error');
    await waitFor(() => expect(seen.length).toBe(1));
    expect(seen[0]).toBeInstanceOf(RetryRowError);
  });

  it('reports a bulk import 422 while keeping the inline bulk message', async () => {
    const seen: BulkImportError[] = [];
    server.use(
      http.post('*/api/movies/import/library', () =>
        problemResponse(422, {
          status: 422,
          title: 'Unprocessable',
          detail: 'Select at least one folder.',
        }),
      ),
    );
    renderHarness(
      <BulkProbe
        onError={(error) => {
          seen.push(error as BulkImportError);
        }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Import library' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    expect(toasts()[0]).toHaveTextContent('Status: 422');
    await waitFor(() => expect(seen.length).toBe(1));
    expect(seen[0]).toBeInstanceOf(BulkImportError);
    expect(bulkImportErrorMessage(seen[0])).toBe('Select at least one folder.');
  });
});
