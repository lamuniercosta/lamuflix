import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { createElement, type ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import {
  OutsideLibraryError,
  outsideLibraryQueryKey,
  useOutsideLibraryRecommendations,
} from './outsideLibraryApi';
import type { OutsideLibraryCard } from './outsideLibraryApi';

const card: OutsideLibraryCard = {
  tmdbId: 945961,
  title: 'Outside Library Movie',
  releaseYear: 1995,
  posterPath: null,
  reason: 'Recommended for Heat and Collateral',
};

function renderDiscovery(existingClient?: QueryClient) {
  const queryClient =
    existingClient ?? new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  }
  return {
    ...renderHook(() => useOutsideLibraryRecommendations(), { wrapper: Wrapper }),
    queryClient,
  };
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe('useOutsideLibraryRecommendations', () => {
  it('fetches the separate not-in-library route with the generated client', async () => {
    // arrange
    let path: string | null = null;
    server.use(
      http.get('*/api/recommendations/not-in-library', ({ request }) => {
        path = new URL(request.url).pathname;
        return HttpResponse.json([card]);
      }),
    );

    // act
    const { result } = renderDiscovery();
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(path).toBe('/api/recommendations/not-in-library');
    expect(result.current.data).toEqual([card]);
  });

  it('accepts the TMDb-disabled API 200 [] empty response', async () => {
    // arrange
    server.use(http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([])));

    // act
    const { result } = renderDiscovery();
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(result.current.data).toEqual([]);
  });

  it('accepts the blank-ApiKey API 200 [] empty response', async () => {
    // arrange
    server.use(http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([])));

    // act
    const { result } = renderDiscovery();
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(result.current.data).toEqual([]);
  });

  it('does not retry a failed discovery request and exposes its status', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/not-in-library', () => {
        calls += 1;
        return HttpResponse.json(problemDetails(503), { status: 503 });
      }),
    );

    // act
    const { result } = renderDiscovery();
    await waitFor(() => expect(result.current.isError).toBe(true));

    // assert
    await waitFor(() => expect(result.current.failureCount).toBe(1));
    expect(calls).toBe(1);
    expect(result.current.error).toBeInstanceOf(OutsideLibraryError);
    expect(result.current.error?.status).toBe(503);
  });

  it('keeps a query identity independent of the For You grid', async () => {
    // arrange
    server.use(http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([card])));
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    queryClient.setQueryData(['forYou', 10], [{ id: 7, title: 'The Batman' }]);

    // act
    const { result } = renderDiscovery(queryClient);
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(outsideLibraryQueryKey).toEqual(['outsideLibraryRecommendations']);
    expect(queryClient.getQueryData(['outsideLibraryRecommendations'])).toEqual([card]);
    expect(queryClient.getQueryData(['forYou', 10])).toEqual([{ id: 7, title: 'The Batman' }]);
  });
});
