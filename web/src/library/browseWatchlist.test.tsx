import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import App from '../App';
import { server } from '../test/server';
import { __resetWatchlistStore } from './watchlistStore';

function watchlistParam(search: string): string | null {
  for (const [key, value] of new URLSearchParams(search)) {
    if (key.toLowerCase() === 'inwatchlist') return value;
  }
  return null;
}

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

function captureBrowse() {
  const requests: string[] = [];
  server.use(
    http.get('/api/movies', ({ request }) => {
      requests.push(new URL(request.url).search);
      return HttpResponse.json({ items: [], totalCount: 0 }, { status: 200 });
    }),
  );
  return requests;
}

beforeEach(() => {
  __resetWatchlistStore();
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('browse watchlist filter translation', () => {
  it.each([[''], ['?watchlist=false'], ['?watchlist=1'], ['?watchlist=']])(
    'sends no inWatchlist filter for URL "%s"',
    async (search) => {
      window.history.replaceState({}, '', `/${search}`);
      const requests = captureBrowse();

      renderApp();

      await waitFor(() => expect(requests.length).toBeGreaterThan(0));
      expect(watchlistParam(requests[0])).toBeNull();
    },
  );

  it('sends inWatchlist=true only when the URL is watchlist=true', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    const requests = captureBrowse();

    renderApp();

    await waitFor(() => expect(requests.length).toBeGreaterThan(0));
    expect(watchlistParam(requests[0])).toBe('true');
  });

  it('combines the watchlist filter with the other filters using AND', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    const requests = captureBrowse();

    renderApp();

    await waitFor(() => expect(requests.length).toBeGreaterThan(0));
    const params = new URLSearchParams(requests[0]);
    expect(watchlistParam(requests[0])).toBe('true');
    expect(params.get('sort')).toBe('Title');
    expect(params.get('direction')).toBe('Ascending');
    expect(params.get('page')).toBe('1');
    expect(params.get('pageSize')).toBe('20');
  });

  it('drops the filter from the request when the toggle turns it off', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    const requests = captureBrowse();

    renderApp();

    await waitFor(() => expect(requests.length).toBe(1));
    expect(watchlistParam(requests[0])).toBe('true');

    fireEvent.click(screen.getByRole('button', { name: 'Watchlist' }));

    await waitFor(() => expect(requests.length).toBe(2));
    expect(watchlistParam(requests[1])).toBeNull();
  });
});
