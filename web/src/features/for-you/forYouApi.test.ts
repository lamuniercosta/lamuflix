import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { createElement, type ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { forYouRecommendations } from '../../mocks/fixtures';
import { server } from '../../test/server';
import { forYouQueryKey, useForYou } from './forYouApi';

function renderForYou(existingClient?: QueryClient) {
  const queryClient =
    existingClient ?? new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  }
  return { ...renderHook(() => useForYou(), { wrapper: Wrapper }), queryClient };
}

function forYouHandler(body: object, status = 200) {
  return http.get('*/api/recommendations/for-you', () => HttpResponse.json(body, { status }));
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe('useForYou', () => {
  it('requests the for-you route with take 10', async () => {
    // arrange
    let take: string | null = null;
    server.use(
      http.get('*/api/recommendations/for-you', ({ request }) => {
        take = new URL(request.url).searchParams.get('take');
        return HttpResponse.json(forYouRecommendations);
      }),
    );

    // act
    const { result } = renderForYou();
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(take).toBe('10');
  });

  it('stores the result under the [forYou, 10] key on the shared QueryClient', async () => {
    // arrange
    server.use(forYouHandler(forYouRecommendations));

    // act
    const { result, queryClient } = renderForYou();
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(forYouQueryKey).toEqual(['forYou', 10]);
    expect(queryClient.getQueryData(['forYou', 10])).toEqual(forYouRecommendations);
    expect(
      queryClient.getQueryCache().find({ queryKey: ['forYou', 10], exact: true }),
    ).toBeDefined();
  });

  it('forwards the query cancellation signal to the request', async () => {
    // arrange
    const requestSignals: AbortSignal[] = [];
    const fetch = globalThis.fetch;
    vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
      const request = input instanceof Request ? input : new Request(input);
      requestSignals.push(request.signal);
      return fetch(request);
    });
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      http.get('*/api/recommendations/for-you', async () => {
        await gate;
        return HttpResponse.json(forYouRecommendations);
      }),
    );

    // act
    const { queryClient, unmount } = renderForYou();
    await waitFor(() => expect(requestSignals).toHaveLength(1));
    void queryClient.cancelQueries({ queryKey: forYouQueryKey });

    // assert
    await waitFor(() => expect(requestSignals[0].aborted).toBe(true));
    release();
    unmount();
  });

  it('keeps a query identity distinct from the browse and related-movies keys', async () => {
    // arrange
    server.use(forYouHandler(forYouRecommendations));
    const browseRow = forYouRecommendations[0];
    const seeded = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    seeded.setQueryData(['browseMovies', { page: 1 }], {
      items: [browseRow],
      totalCount: 1,
    });
    seeded.setQueryData(['relatedMovies', 7], [browseRow]);

    // act
    const { result } = renderForYou(seeded);
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // assert
    expect(seeded.getQueryData(['browseMovies', { page: 1 }])).toEqual({
      items: [browseRow],
      totalCount: 1,
    });
    expect(seeded.getQueryData(['relatedMovies', 7])).toEqual([browseRow]);
    expect(seeded.getQueryData(['forYou', 10])).toEqual(forYouRecommendations);
    expect(seeded.getQueryData(['forYou', 10])).not.toEqual([browseRow]);
  });

  it('does not retry a failed for-you request', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/for-you', () => {
        calls += 1;
        return HttpResponse.json({ title: 'Server error', status: 500 }, { status: 500 });
      }),
    );

    // act
    const { result } = renderForYou();
    await waitFor(() => expect(result.current.isError).toBe(true));

    // assert
    await waitFor(() => expect(result.current.failureCount).toBe(1));
    expect(calls).toBe(1);
  });
});
