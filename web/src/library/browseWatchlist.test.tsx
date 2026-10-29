import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import App from '../App';
import { directors as directorFixtures } from '../mocks/fixtures';
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

describe('director filter hosts', () => {
  it('restores a director selection from the URL and forwards it on the library host', async () => {
    window.history.replaceState({}, '', '/?directorId=2');
    const requests = captureBrowse();

    renderApp();

    await screen.findByText('Christopher Nolan');
    await waitFor(() => expect(requests.length).toBeGreaterThan(0));
    expect(new URLSearchParams(requests[0]).get('directorId')).toBe('2');
    expect(watchlistParam(requests[0])).toBeNull();
  });

  it('restores a director selection and AND-composes it on the watchlist host', async () => {
    window.history.replaceState({}, '', '/?watchlist=true&directorId=2');
    const requests = captureBrowse();

    renderApp();

    await screen.findByText('Christopher Nolan');
    await waitFor(() => expect(requests.length).toBeGreaterThan(0));
    const params = new URLSearchParams(requests[0]);
    expect(params.get('directorId')).toBe('2');
    expect(watchlistParam(requests[0])).toBe('true');
  });

  it('restores a new director selection on back/forward navigation', async () => {
    window.history.replaceState({}, '', '/?directorId=2');
    const requests = captureBrowse();

    renderApp();
    await screen.findByText('Christopher Nolan');

    window.history.replaceState({}, '', '/?directorId=1');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await screen.findByText('Denis Villeneuve');
    await waitFor(() =>
      expect(requests.some((search) => new URLSearchParams(search).get('directorId') === '1')).toBe(
        true,
      ),
    );
  });

  it('resets page to 1 and preserves other filters when the director changes', async () => {
    window.history.replaceState({}, '', '/?directorId=2&genreIds=1&page=3');
    const requests = captureBrowse();

    renderApp();
    await screen.findByText('Christopher Nolan');
    await waitFor(() => expect(requests.length).toBe(1));

    fireEvent.change(screen.getByRole('combobox', { name: 'Director' }), {
      target: { value: 'Denis' },
    });
    fireEvent.click(screen.getByRole('option', { name: /Denis Villeneuve/ }));

    await waitFor(() => expect(requests.length).toBeGreaterThan(1));
    const next = new URLSearchParams(requests.at(-1)!);
    expect(next.get('directorId')).toBe('1');
    expect(next.get('genreIds')).toBe('1');
    expect(next.get('page')).toBe('1');
  });

  it('shows a local loading state without blocking other filters', async () => {
    server.use(
      http.get('/api/directors', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100));
        return HttpResponse.json(directorFixtures, { status: 200 });
      }),
    );

    renderApp();

    expect(await screen.findByText('Loading directors')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear all/i })).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('Loading directors')).not.toBeInTheDocument());
  });

  it('keeps the selection clearable and retries a failed director fetch', async () => {
    let attempts = 0;
    server.use(
      http.get('/api/directors', () => {
        attempts += 1;
        if (attempts === 1) {
          return HttpResponse.json({}, { status: 500 });
        }
        return HttpResponse.json(directorFixtures, { status: 200 });
      }),
    );
    window.history.replaceState({}, '', '/?directorId=2');

    renderApp();

    expect(await screen.findByText('Could not load directors')).toBeInTheDocument();
    expect(screen.getByText('Director 2')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear all/i })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => expect(attempts).toBeGreaterThan(1));
    await waitFor(() =>
      expect(screen.queryByText('Could not load directors')).not.toBeInTheDocument(),
    );
    expect(screen.getByText('Christopher Nolan')).toBeInTheDocument();
  });
});
