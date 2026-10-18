import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import type { paths } from './api/types';
import App from './App';
import { retryAcceptedHandler, standingBrowseQuery } from './test/handlers';
import { server } from './test/server';

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

function detailsBackLink() {
  const link = screen
    .getAllByRole('link', { name: 'Library' })
    .find((element) => !element.closest('nav'));
  if (!link) throw new Error('Details back link not found');
  return link;
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
  cleanup();
});

describe('App library host', () => {
  it('shows the Import folder button, a Pending badge without an Enriched badge, and a polite live region', async () => {
    renderApp();

    expect(screen.getByRole('button', { name: 'Import folder' })).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Arrival' })).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).queryByText('Enriched'),
    ).not.toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getByText('Pending'),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Retry / })).not.toBeInTheDocument();
    expect(screen.getByRole('list', { name: 'Movies' })).toHaveAttribute('aria-live', 'polite');
    expect(document.activeElement).not.toBe(screen.getByRole('list', { name: 'Movies' }));
  });

  it('shows Retry only for NotFound and Failed rows with row-specific names', async () => {
    server.use(
      http.get('/api/movies', () =>
        HttpResponse.json(
          {
            items: [
              {
                id: 1,
                title: 'Dune',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'Enriched',
                isInWatchlist: false,
                isWatched: false,
                isNotInterested: false,
              },
              {
                id: 2,
                title: 'Arrival',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'Pending',
                isInWatchlist: false,
                isWatched: false,
                isNotInterested: false,
              },
              {
                id: 3,
                title: 'Lost Film',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'NotFound',
                isInWatchlist: false,
                isWatched: false,
                isNotInterested: false,
              },
              {
                id: 4,
                title: 'Broken Cut',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'Failed',
                isInWatchlist: false,
                isWatched: false,
                isNotInterested: false,
              },
            ],
            totalCount: 4,
          },
          { status: 200 },
        ),
      ),
    );

    renderApp();

    expect(await screen.findByRole('heading', { name: 'Lost Film' })).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getByText('Not found'),
    ).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getByText('Failed'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry Lost Film' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry Broken Cut' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry Dune' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry Arrival' })).not.toBeInTheDocument();
  });

  it('shows the empty-library copy when browse returns no items', async () => {
    server.use(
      http.get('/api/movies', () =>
        HttpResponse.json({ items: [], totalCount: 0 }, { status: 200 }),
      ),
    );

    renderApp();

    expect(
      await screen.findByText('No movies yet. Import a folder to get started.'),
    ).toBeInTheDocument();
  });

  it('sends the A6 standing first-page browse query', async () => {
    let search = '';
    server.use(
      http.get('/api/movies', ({ request }) => {
        search = new URL(request.url).search;
        return HttpResponse.json({ items: [], totalCount: 0 }, { status: 200 });
      }),
    );

    renderApp();

    expect(
      await screen.findByText('No movies yet. Import a folder to get started.'),
    ).toBeInTheDocument();
    expect(search).toBe(
      `?sort=${standingBrowseQuery.sort}&direction=${standingBrowseQuery.direction}&page=${standingBrowseQuery.page}&pageSize=${standingBrowseQuery.pageSize}`,
    );
  });

  it('opens the import dialog with title, folder path focus, actions, and focus trap', async () => {
    renderApp();
    const importFolder = screen.getByRole('button', { name: 'Import folder' });
    fireEvent.click(importFolder);

    const dialog = screen.getByRole('dialog', { name: 'Import a movie folder' });
    const folderPath = screen.getByLabelText('Folder path');
    expect(folderPath).toHaveFocus();
    expect(screen.getByRole('button', { name: 'Import' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();

    fireEvent.keyDown(dialog, { key: 'Tab', shiftKey: true });
    expect(screen.getByRole('button', { name: 'Import' })).toHaveFocus();
    fireEvent.keyDown(dialog, { key: 'Tab' });
    expect(folderPath).toHaveFocus();
  });

  it('returns focus to Import folder on Escape, Cancel, and successful import', async () => {
    renderApp();
    const importFolder = screen.getByRole('button', { name: 'Import folder' });

    fireEvent.click(importFolder);
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(importFolder).toHaveFocus();

    fireEvent.click(importFolder);
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(importFolder).toHaveFocus();

    fireEvent.click(importFolder);
    fireEvent.change(screen.getByLabelText('Folder path'), {
      target: { value: 'F:/library/NewArrival' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Import' }));

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'NewArrival' })).toBeInTheDocument();
    });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(importFolder).toHaveFocus();
  });
});

describe('App movie details routing', () => {
  it('mounts MovieDetailsView with the parsed id on /movies/<id>', async () => {
    window.history.replaceState({}, '', '/movies/1');

    renderApp();

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument();
  });

  it('renders the library screen on paths outside /movies/<id>', async () => {
    window.history.replaceState({}, '', '/archive');

    renderApp();

    expect(screen.getByRole('button', { name: 'Import folder' })).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
  });
});

describe('App layout shell', () => {
  it('shares the wordmark, navigation, import action, and search on library and watchlist', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    expect(screen.getByRole('link', { name: 'LamuFlix' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Library' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Watchlist' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Import folder' })).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Search titles')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Watchlist' }));
    expect(new URLSearchParams(window.location.search).get('watchlist')).toBe('true');
    expect(screen.getByRole('button', { name: 'Watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('link', { name: 'LamuFlix' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Import folder' })).toBeInTheDocument();
  });

  it('reflects a shared ?watchlist=true link in the shell toggle', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    renderApp();
    await screen.findByText('Your watchlist is empty.');

    expect(screen.getByRole('button', { name: 'Watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('falls back to the library for malformed, non-positive, and non-integer detail paths', async () => {
    for (const path of ['/movies/abc', '/movies/0', '/movies/-4', '/movies/1.5']) {
      window.history.replaceState({}, '', path);
      const view = renderApp();
      expect(await screen.findByText('Dune', {}, { timeout: 5000 })).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Play' })).not.toBeInTheDocument();
      view.unmount();
      cleanup();
      window.history.replaceState({}, '', '/');
    }
  });

  it('opens a movie client-side and restores the exact originating query from the back link', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending&sort=Title&direction=Ascending');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.click(screen.getByRole('link', { name: 'Dune' }));
    expect(await screen.findByRole('button', { name: 'Play' })).toBeInTheDocument();
    expect(window.location.pathname).toBe('/movies/1');

    fireEvent.click(screen.getByRole('link', { name: 'Library (Pending)' }));
    expect(window.location.pathname).toBe('/');
    expect(window.location.search).toBe('?statuses=Pending&sort=Title&direction=Ascending');
    expect(await screen.findByRole('link', { name: 'Dune' })).toBeInTheDocument();
  });

  it('sends a direct detail route back link to the default library without valid return state', async () => {
    window.history.replaceState(null, '', '/movies/1');
    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();

    fireEvent.click(detailsBackLink());
    expect(window.location.pathname).toBe('/');
    expect(await screen.findByRole('link', { name: 'Dune' })).toBeInTheDocument();
  });

  it('rejects untrusted return state and falls back to the default library', async () => {
    window.history.replaceState(
      { libraryReturnUrl: 'https://example.com/?text=x' },
      '',
      '/movies/1',
    );
    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();

    fireEvent.click(detailsBackLink());
    expect(window.location.pathname).toBe('/');
    expect(window.location.search).toBe('');
  });

  it('updates the mounted route on browser Back and Forward', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.click(screen.getByRole('link', { name: 'Dune' }));
    expect(await screen.findByRole('button', { name: 'Play' })).toBeInTheDocument();

    window.history.replaceState({}, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate'));
    expect(await screen.findByRole('link', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Play' })).not.toBeInTheDocument();
  });

  it('keeps details search local until Enter, then preserves filters and replaces text', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.click(screen.getByRole('link', { name: 'Dune' }));
    expect(await screen.findByRole('button', { name: 'Play' })).toBeInTheDocument();

    const search = screen.getByPlaceholderText('Search titles');
    fireEvent.change(search, { target: { value: 'arrival' } });
    expect(window.location.pathname).toBe('/movies/1');

    const form = search.closest('form');
    expect(form).not.toBeNull();
    fireEvent.submit(form!);
    await waitFor(() => {
      expect(window.location.pathname).toBe('/');
    });
    const params = new URLSearchParams(window.location.search);
    expect(params.get('text')).toBe('arrival');
    expect(params.getAll('statuses')).toContain('Pending');
    expect(params.get('page') ?? '1').toBe('1');
  });

  it('cancels a pending shell search when browser history changes route', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.change(screen.getByPlaceholderText('Search titles'), {
      target: { value: 'dune' },
    });
    window.history.replaceState({}, '', '/?statuses=Pending');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await new Promise((resolve) => setTimeout(resolve, 600));
    const params = new URLSearchParams(window.location.search);
    expect(params.get('text')).toBeNull();
    expect(params.getAll('statuses')).toContain('Pending');
  });

  it('resyncs the details back link when only history state changes', async () => {
    window.history.replaceState({ libraryReturnUrl: '/?text=a' }, '', '/movies/1');
    renderApp();
    expect(await screen.findByRole('button', { name: 'Play' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Library ("a")' })).toHaveAttribute('href', '/?text=a');

    window.history.replaceState({ libraryReturnUrl: '/?text=b' }, '', '/movies/1');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(screen.getByRole('link', { name: 'Library ("b")' })).toHaveAttribute(
        'href',
        '/?text=b',
      );
    });
  });

  it('toggles a card bookmark optimistically from the shared shell', async () => {
    server.use(
      http.post('/api/movies/{id}/watchlist', () => new HttpResponse(null, { status: 204 })),
    );
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.click(screen.getAllByRole('button', { name: 'Add to watchlist' })[0]);
    await waitFor(() => {
      expect(
        screen.getAllByRole('button', { name: 'Remove from watchlist' })[0],
      ).toBeInTheDocument();
    });
  });

  it(
    'retries enrichment from the shared shell and marks the row Pending',
    { timeout: 15000 },
    async () => {
      let calls = 0;
      server.use(
        http.get('/api/movies', () => {
          calls += 1;
          return HttpResponse.json(
            {
              items: [
                {
                  id: 3,
                  title: 'Lost Film',
                  posterUrl: null,
                  releaseYear: null,
                  runtime: null,
                  imdbRating: null,
                  enrichmentStatus: calls > 1 ? 'Pending' : 'NotFound',
                  isInWatchlist: false,
                  isWatched: false,
                  isNotInterested: false,
                },
              ],
              totalCount: 1,
            },
            { status: 200 },
          );
        }),
        retryAcceptedHandler,
      );
      window.history.replaceState({}, '', '/');
      renderApp();
      await screen.findByRole('heading', { name: 'Lost Film' }, { timeout: 5000 });

      fireEvent.click(screen.getByRole('button', { name: 'Retry Lost Film' }));
      await waitFor(
        () => {
          expect(screen.queryByRole('button', { name: 'Retry Lost Film' })).not.toBeInTheDocument();
        },
        { timeout: 5000 },
      );
    },
  );

  it('shows the inline Playback unavailable alert for a 403 after client-side details navigation', async () => {
    server.use(http.post('/api/movies/{id}/play', () => new HttpResponse(null, { status: 403 })));
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.click(screen.getByRole('link', { name: 'Dune' }));
    expect(await screen.findByRole('button', { name: 'Play' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Play' }));
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Playback unavailable');
    expect(alert).toHaveTextContent('Local playback is disabled on this server.');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('debounces shell search without discarding other active filters', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending');
    renderApp();
    await screen.findByText('Dune', {}, { timeout: 5000 });

    fireEvent.change(screen.getByPlaceholderText('Search titles'), {
      target: { value: 'dune' },
    });
    await waitFor(
      () => {
        const params = new URLSearchParams(window.location.search);
        expect(params.get('text')).toBe('dune');
      },
      { timeout: 3000 },
    );
    expect(new URLSearchParams(window.location.search).getAll('statuses')).toContain('Pending');
  });
});
