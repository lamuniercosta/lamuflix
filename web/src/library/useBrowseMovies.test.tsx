import { http, HttpResponse } from 'msw';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { ReactNode } from 'react';
import { server } from '../test/server';
import useBrowseMovies, { BrowseMoviesError } from './useBrowseMovies';

function wrapper({ children }: { children: ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
}

function problemResponse(status: number) {
  return new HttpResponse(
    JSON.stringify({
      type: 'about:blank',
      title: 'Problem',
      status,
      detail: `status ${status}`,
      instance: null,
    }),
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  );
}

describe('useBrowseMovies', () => {
  it('returns typed data on success', async () => {
    const { result } = renderHook(() => useBrowseMovies({}), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data?.items).toHaveLength(2);
  });

  it('classifies a 422 as an invalid filter without retry', async () => {
    server.use(http.get('*/api/movies', () => problemResponse(422)));

    const { result } = renderHook(() => useBrowseMovies({}), { wrapper });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(result.current.error).toBeInstanceOf(BrowseMoviesError);
    expect(result.current.error?.status).toBe(422);
    expect(result.current.error?.invalidFilter).toBe(true);
  });

  it('classifies a 400 as a recoverable error', async () => {
    server.use(http.get('*/api/movies', () => problemResponse(400)));

    const { result } = renderHook(() => useBrowseMovies({}), { wrapper });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(result.current.error?.status).toBe(400);
    expect(result.current.error?.invalidFilter).toBe(false);
  });

  it('retains content on a same-key refetch', async () => {
    const { result, rerender } = renderHook(() => useBrowseMovies({}), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    const first = result.current.data;

    rerender();
    expect(result.current.data).toBe(first);
  });
});
