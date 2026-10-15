import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
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
    fireEvent.click(await screen.findByRole('button', { name: 'Add to watchlist' }));
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
        'aria-busy',
        'false',
      ),
    );
    card.unmount();

    goto('/movies/1');
    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
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
    const buttons = await screen.findAllByRole('button', { name: 'Add to watchlist' });
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
    fireEvent.click(await screen.findByRole('button', { name: 'Add to watchlist' }));
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
    fireEvent.click(await screen.findByRole('button', { name: 'Add to watchlist' }));
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
    const button = await screen.findByRole('button', { name: 'Add to watchlist' });

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
    const button = await screen.findByRole('button', { name: 'Add to watchlist' });
    button.focus();
    fireEvent.click(button);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(WATCHLIST_TOGGLE_ERROR);
    expect(button).toHaveAttribute('aria-describedby', alert.id);
    expect(button).toHaveFocus();
  });
});
