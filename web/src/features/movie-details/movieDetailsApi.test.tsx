import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails } from '../../mocks/fixtures';
import { MovieDetailsError, useMovieDetails } from './movieDetailsApi';

function useDetails(body: object) {
  server.use(http.get('*/api/movies/:id', () => HttpResponse.json(body)));
}

function renderDetailsHook() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return renderHook(() => useMovieDetails(1), {
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    ),
  });
}

const enriched = {
  ...movieDetails,
  metadata: {
    ...movieDetails.metadata!,
    writers: ['Frank Herbert', 'Jon Spaihts'],
    genres: ['Science fiction', 'Adventure'],
    collectionName: 'Dune Saga',
  },
};

describe('movieDetailsApi About field contract', () => {
  it('accepts and exposes stored writers, genres and a collection name', async () => {
    useDetails(enriched);

    const { result } = renderDetailsHook();

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.metadata?.writers).toEqual(['Frank Herbert', 'Jon Spaihts']);
    expect(result.current.data?.metadata?.genres).toEqual(['Science fiction', 'Adventure']);
    expect(result.current.data?.metadata?.collectionName).toBe('Dune Saga');
  });

  it('accepts empty arrays and an explicit null collection name', async () => {
    useDetails({
      ...enriched,
      metadata: { ...enriched.metadata, writers: [], genres: [], collectionName: null },
    });

    const { result } = renderDetailsHook();

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.metadata?.writers).toEqual([]);
    expect(result.current.data?.metadata?.genres).toEqual([]);
    expect(result.current.data?.metadata?.collectionName).toBeNull();
  });

  it('fails closed through the details error path when a new required key is absent', async () => {
    const metadata = { ...enriched.metadata } as Record<string, unknown>;
    delete metadata.writers;
    useDetails({ ...enriched, metadata });

    const { result } = renderDetailsHook();

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error).toBeInstanceOf(MovieDetailsError);
    expect(result.current.data).toBeUndefined();
  });

  it.each([
    ['writers', 'Frank Herbert'],
    ['genres', 'Drama'],
    ['collectionName', 42],
  ])('fails closed through the details error path for malformed %s', async (field, value) => {
    useDetails({
      ...enriched,
      metadata: { ...enriched.metadata, [field]: value },
    });

    const { result } = renderDetailsHook();

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error).toBeInstanceOf(MovieDetailsError);
    expect(result.current.data).toBeUndefined();
  });
});
