import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { components } from '../api/types';
import App from '../App';
import { server } from '../test/server';
import LibraryView from './LibraryView';
import { pendingRow } from './libraryQueries';
import { WATCHLIST_TOGGLE_ERROR, __resetWatchlistStore } from './watchlistStore';

type MovieSummary = components['schemas']['MovieSummaryResponse'];

function summary(overrides: Partial<MovieSummary> = {}): MovieSummary {
  return {
    id: 1,
    title: 'Dune',
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Enriched',
    isInWatchlist: true,
    isWatched: false,
    isNotInterested: false,
    ...overrides,
  };
}

function browseBody(items: MovieSummary[]) {
  return { items, totalCount: items.length };
}

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

function rowFor(title: string): HTMLElement {
  const row = screen.getByRole('heading', { name: title }).closest('[role="listitem"]');
  if (!row) throw new Error(`No row for ${title}`);
  return row as HTMLElement;
}

beforeEach(() => {
  __resetWatchlistStore();
  window.history.replaceState({}, '', '/');
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('watchlist filter empty state', () => {
  it('shows "Your watchlist is empty." when the filter is on with no matches', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    server.use(http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })));

    renderApp();

    expect(await screen.findByText('Your watchlist is empty.')).toBeInTheDocument();
    expect(
      screen.queryByText('No movies yet. Import a folder to get started.'),
    ).not.toBeInTheDocument();
  });

  it('keeps the delivered empty copy when the filter is off', async () => {
    server.use(http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })));

    renderApp();

    expect(
      await screen.findByText('No movies yet. Import a folder to get started.'),
    ).toBeInTheDocument();
    expect(screen.queryByText('Your watchlist is empty.')).not.toBeInTheDocument();
  });
});

describe('watchlist filter-active removal reconciliation', () => {
  it('removes the card after a successful removal', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    server.use(
      http.get('/api/movies', () =>
        HttpResponse.json(
          browseBody([summary({ id: 1, title: 'Dune' }), summary({ id: 2, title: 'Arrival' })]),
          { status: 200 },
        ),
      ),
      http.delete('/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(within(rowFor('Dune')).getByRole('button', { name: 'Remove from watchlist' }));

    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Dune' })).not.toBeInTheDocument(),
    );
    expect(screen.getByRole('heading', { name: 'Arrival' })).toBeInTheDocument();
  });

  it('shows the watchlist empty copy after the last watchlisted card is removed', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    server.use(
      http.get('/api/movies', () =>
        HttpResponse.json(browseBody([summary({ id: 1, title: 'Dune' })]), { status: 200 }),
      ),
      http.delete('/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(within(rowFor('Dune')).getByRole('button', { name: 'Remove from watchlist' }));

    expect(await screen.findByText('Your watchlist is empty.')).toBeInTheDocument();
    expect(
      screen.queryByText('No movies yet. Import a folder to get started.'),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole('list', { name: 'Movies' })).not.toBeInTheDocument();
  });

  it('keeps the card and rolls back with the inline error when removal fails', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    server.use(
      http.get('/api/movies', () =>
        HttpResponse.json(
          browseBody([summary({ id: 1, title: 'Dune' }), summary({ id: 2, title: 'Arrival' })]),
          { status: 200 },
        ),
      ),
      http.delete('/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 500 })),
    );

    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });
    const remove = within(rowFor('Dune')).getByRole('button', { name: 'Remove from watchlist' });
    fireEvent.click(remove);

    await waitFor(() => expect(remove).toHaveAttribute('aria-busy', 'false'));
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(
      within(rowFor('Dune')).getByRole('button', { name: 'Remove from watchlist' }),
    ).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByText(WATCHLIST_TOGGLE_ERROR)).toBeInTheDocument();
  });
});

describe('watchlist filter exit reconciliation', () => {
  it('restores hidden cards when the watchlist filter turns off', async () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    server.use(
      http.get('/api/movies', () =>
        HttpResponse.json(browseBody([summary({ id: 1, title: 'Dune' })]), { status: 200 }),
      ),
      http.delete('/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    function LocalWrapper({ children }: { children: ReactNode }) {
      return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    }
    const view = render(<LibraryView leadingItem={null} inWatchlist={true} />, {
      wrapper: LocalWrapper,
    });

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(within(rowFor('Dune')).getByRole('button', { name: 'Remove from watchlist' }));
    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: 'Dune' })).not.toBeInTheDocument();
    });

    view.rerender(
      <QueryClientProvider client={client}>
        <LibraryView leadingItem={null} inWatchlist={false} />
      </QueryClientProvider>,
    );

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
  });
});

describe('watchlist filter empty state with a hidden leading item', () => {
  it('shows the watchlist empty copy for a non-watchlisted import placeholder', async () => {
    server.use(http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    render(
      <QueryClientProvider client={client}>
        <LibraryView leadingItem={pendingRow('99', 'Imported')} inWatchlist />
      </QueryClientProvider>,
    );

    expect(await screen.findByText('Your watchlist is empty.')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Imported' })).not.toBeInTheDocument();
  });
});
