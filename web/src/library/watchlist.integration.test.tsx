import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { components } from '../api/types';
import App from '../App';
import { server } from '../test/server';
import { WATCHLIST_TOGGLE_ERROR, __resetWatchlistStore } from './watchlistStore';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type MovieDetails = components['schemas']['MovieDetailsResponse'];

function summary(overrides: Partial<MovieSummary> = {}): MovieSummary {
  return {
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
    ...overrides,
  };
}

function details(overrides: Partial<MovieDetails> = {}): MovieDetails {
  return {
    id: 1,
    title: 'Dune',
    path: '/media/dune.mkv',
    format: 'mkv',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    metadata: null,
    ...overrides,
  };
}

function deferred() {
  let resolve!: () => void;
  const promise = new Promise<void>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

function browseBody(items: MovieSummary[]) {
  return { items, totalCount: items.length };
}

function goto(path: string) {
  window.history.replaceState({}, '', path);
}

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
  __resetWatchlistStore();
});

beforeEach(() => {
  goto('/');
});

describe('watchlist shared state across card and hero', () => {
  it('shows a card toggle in the delivered details hero after navigation', async () => {
    server.use(
      http.get('*/api/movies', () => HttpResponse.json(browseBody([summary()]))),
      http.get('*/api/movies/:id', () => HttpResponse.json(details())),
      http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    const card = renderApp();
    fireEvent.click(
      await within(screen.getByRole('region', { name: 'Results' })).findByRole(
        'button',
        { name: 'Add to watchlist' },
        { timeout: 5000 },
      ),
    );
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
        'aria-busy',
        'false',
      ),
    );
    card.unmount();

    goto('/movies/1');
    renderApp();
    expect(
      await screen.findByRole('heading', { name: 'Dune' }, { timeout: 5000 }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('keeps different movies isolated while a toggle is pending', async () => {
    const gate = deferred();
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.json(browseBody([summary({ id: 1 }), summary({ id: 2, title: 'Arrival' })])),
      ),
      http.post('*/api/movies/:id/watchlist', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderApp();
    const buttons = await within(screen.getByRole('region', { name: 'Results' })).findAllByRole(
      'button',
      { name: 'Add to watchlist' },
    );
    expect(buttons).toHaveLength(2);

    fireEvent.click(buttons[0]);
    expect(buttons[0]).toHaveAttribute('aria-pressed', 'true');
    expect(buttons[0]).toHaveAttribute('aria-busy', 'true');
    expect(buttons[1]).toHaveAttribute('aria-pressed', 'false');
    expect(buttons[1]).toHaveAttribute('aria-busy', 'false');

    gate.resolve();
    await waitFor(() => expect(buttons[0]).toHaveAttribute('aria-busy', 'false'));
  });
});

describe('watchlist delayed settlement across unmount and navigation', () => {
  it('retains a delayed 204 on the mounted hero surface', async () => {
    const gate = deferred();
    server.use(
      http.get('*/api/movies', () => HttpResponse.json(browseBody([summary()]))),
      http.get('*/api/movies/:id', () => HttpResponse.json(details())),
      http.post('*/api/movies/:id/watchlist', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const card = renderApp();
    fireEvent.click(
      await within(screen.getByRole('region', { name: 'Results' })).findByRole('button', {
        name: 'Add to watchlist',
      }),
    );
    expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
      'aria-busy',
      'true',
    );
    card.unmount();

    goto('/movies/1');
    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });
    expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );

    gate.resolve();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
        'aria-busy',
        'false',
      ),
    );
    expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('rolls back a delayed failure with the per-movie error on the mounted hero surface', async () => {
    const gate = deferred();
    server.use(
      http.get('*/api/movies', () => HttpResponse.json(browseBody([summary()]))),
      http.get('*/api/movies/:id', () => HttpResponse.json(details())),
      http.post('*/api/movies/:id/watchlist', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 500 });
      }),
    );

    const card = renderApp();
    fireEvent.click(
      await within(screen.getByRole('region', { name: 'Results' })).findByRole('button', {
        name: 'Add to watchlist',
      }),
    );
    card.unmount();

    goto('/movies/1');
    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });

    gate.resolve();
    const button = await screen.findByRole('button', { name: 'Add to watchlist' });
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    expect(button).toHaveAttribute('aria-pressed', 'false');
    expect(await screen.findByText(WATCHLIST_TOGGLE_ERROR)).toBeInTheDocument();
  });
});

describe('watchlist bookmark accessibility in delivered hosts', () => {
  it('renders a native button with a decorative bookmark and retains focus through success', async () => {
    server.use(
      http.get('*/api/movies', () => HttpResponse.json(browseBody([summary()]))),
      http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    renderApp();
    const button = await within(screen.getByRole('region', { name: 'Results' })).findByRole(
      'button',
      { name: 'Add to watchlist' },
    );

    expect(button.tagName).toBe('BUTTON');
    expect(button).toHaveAttribute('type', 'button');
    expect(button.querySelector('svg')).toHaveAttribute('aria-hidden', 'true');

    button.focus();
    expect(button).toHaveFocus();

    fireEvent.click(button);
    expect(button).toHaveFocus();
    expect(button).toHaveAttribute('aria-pressed', 'true');
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    expect(button).toHaveFocus();
  });

  it('announces the inline error adjacent to the failed button', async () => {
    server.use(
      http.get('*/api/movies', () => HttpResponse.json(browseBody([summary()]))),
      http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 404 })),
    );

    renderApp();
    const button = await within(screen.getByRole('region', { name: 'Results' })).findByRole(
      'button',
      { name: 'Add to watchlist' },
    );
    button.focus();
    fireEvent.click(button);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(WATCHLIST_TOGGLE_ERROR);
    expect(button).toHaveAttribute('aria-describedby', alert.id);
    expect(button).toHaveFocus();
  });
});

function rowFor(title: string): HTMLElement {
  const row = screen.getByRole('heading', { name: title }).closest('[role="listitem"]');
  if (!row) throw new Error(`No row for ${title}`);
  return row as HTMLElement;
}

describe('watchlist filter accessibility in the delivered header', () => {
  it('renders a native, focusable button that toggles the filter', async () => {
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.json(browseBody([summary({ isInWatchlist: true })])),
      ),
    );

    renderApp();
    const toggle = await screen.findByRole('button', { name: 'Watchlist' });

    expect(toggle.tagName).toBe('BUTTON');
    expect(toggle).toHaveAttribute('type', 'button');
    expect(toggle).toHaveAttribute('aria-pressed', 'false');

    toggle.focus();
    expect(toggle).toHaveFocus();

    fireEvent.click(toggle);
    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    expect(new URLSearchParams(window.location.search).get('watchlist')).toBe('true');
  });

  it('restores the filter from a shared ?watchlist=true link', async () => {
    const requests: string[] = [];
    server.use(
      http.get('*/api/movies', ({ request }) => {
        requests.push(request.url);
        return HttpResponse.json(browseBody([summary({ isInWatchlist: true })]));
      }),
    );

    goto('/?watchlist=true');
    renderApp();

    expect(await screen.findByRole('button', { name: 'Watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    await waitFor(() => expect(requests.length).toBeGreaterThan(0));
    const params = new URL(requests[0]).searchParams;
    const watchlist = [...params.entries()].find(([key]) => key.toLowerCase() === 'inwatchlist');
    expect(watchlist?.[1]).toBe('true');
  });

  it('restores the filter state on Back navigation via popstate', async () => {
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.json(browseBody([summary({ isInWatchlist: true })])),
      ),
    );

    goto('/');
    renderApp();
    const toggle = await screen.findByRole('button', { name: 'Watchlist' });
    expect(toggle).toHaveAttribute('aria-pressed', 'false');

    fireEvent.click(toggle);
    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    expect(new URLSearchParams(window.location.search).get('watchlist')).toBe('true');

    goto('/');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await waitFor(() => expect(toggle).toHaveAttribute('aria-pressed', 'false'));
  });

  it('moves focus to the grid region after a successful filter-active removal', async () => {
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.json(
          browseBody([
            summary({ id: 1, title: 'Dune', isInWatchlist: true }),
            summary({ id: 2, title: 'Arrival', isInWatchlist: true }),
          ]),
        ),
      ),
      http.delete('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    goto('/?watchlist=true');
    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });
    const remove = within(rowFor('Dune')).getByRole('button', { name: 'Remove from watchlist' });
    remove.focus();
    fireEvent.click(remove);

    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Dune' })).not.toBeInTheDocument(),
    );
    expect(document.activeElement).toBe(screen.getByRole('list', { name: 'Movies' }));
    expect(document.activeElement).not.toBe(document.body);
  });
});
