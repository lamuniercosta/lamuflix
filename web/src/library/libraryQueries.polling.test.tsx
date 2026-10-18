import { focusManager, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { components, paths } from '../api/types';
import App from '../App';
import { server } from '../test/server';
import { browseRefetchInterval, clearImportPlaceholder } from './libraryQueries';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

const pendingArrival: MovieSummary = {
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
};

const enrichedArrival: MovieSummary = {
  ...pendingArrival,
  enrichmentStatus: 'Enriched',
};

const duneEnriched: MovieSummary = {
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
};

function browseBody(items: MovieSummary[]): BrowseMoviesResponse {
  return { items, totalCount: items.length };
}

function renderApp() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

describe('browseRefetchInterval', () => {
  it('returns 2000 while any browse item is Pending', () => {
    expect(
      browseRefetchInterval({
        state: { data: browseBody([duneEnriched, pendingArrival]) },
      }),
    ).toBe(2000);
  });

  it('returns false when no browse item is Pending', () => {
    expect(
      browseRefetchInterval({
        state: { data: browseBody([duneEnriched, enrichedArrival]) },
      }),
    ).toBe(false);
  });
});

describe('browse query polling', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['setInterval'] });
    focusManager.setFocused(true);
  });

  afterEach(() => {
    focusManager.setFocused(true);
    vi.useRealTimers();
  });

  it(
    'refetches every 2s while Pending, reconciles without stealing focus, then stops',
    { timeout: 15_000 },
    async () => {
      vi.useRealTimers();
      let calls = 0;
      server.use(
        http.get('/api/movies', () => {
          calls += 1;
          if (calls === 1) {
            return HttpResponse.json(browseBody([pendingArrival]), { status: 200 });
          }
          return HttpResponse.json(browseBody([enrichedArrival]), { status: 200 });
        }),
      );

      renderApp();
      const importFolder = screen.getByRole('button', { name: 'Import folder' });
      importFolder.focus();

      expect(await screen.findByRole('heading', { name: 'Arrival' })).toBeInTheDocument();
      expect(
        within(screen.getByRole('list', { name: 'Movies' })).getByText('Pending'),
      ).toBeInTheDocument();
      expect(calls).toBe(1);

      await waitFor(
        () => {
          expect(
            within(screen.getByRole('list', { name: 'Movies' })).queryByText('Pending'),
          ).not.toBeInTheDocument();
        },
        { timeout: 4000 },
      );
      expect(
        within(screen.getByRole('list', { name: 'Movies' })).queryByText('Pending'),
      ).not.toBeInTheDocument();
      expect(calls).toBe(2);

      const list = screen.getByRole('list', { name: 'Movies' });
      expect(list).toHaveAttribute('aria-live', 'polite');
      expect(importFolder).toHaveFocus();
      expect(document.activeElement).not.toBe(list);

      await new Promise((resolve) => {
        setTimeout(resolve, 4000);
      });
      expect(calls).toBe(2);
    },
  );

  it('does not poll when no Pending row is in view', async () => {
    let calls = 0;
    server.use(
      http.get('/api/movies', () => {
        calls += 1;
        return HttpResponse.json(browseBody([duneEnriched]), { status: 200 });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(calls).toBe(1);

    await vi.advanceTimersByTimeAsync(4000);
    expect(calls).toBe(1);
  });

  it('keeps background refetch off while a Pending row is in view', async () => {
    let calls = 0;
    server.use(
      http.get('/api/movies', () => {
        calls += 1;
        return HttpResponse.json(browseBody([pendingArrival]), { status: 200 });
      }),
    );

    renderApp();
    await waitFor(() =>
      expect(
        within(screen.getByRole('list', { name: 'Movies' })).queryByText('Pending'),
      ).toBeInTheDocument(),
    );
    expect(calls).toBe(1);

    focusManager.setFocused(false);
    await vi.advanceTimersByTimeAsync(4000);
    expect(calls).toBe(1);
  });
});

async function importFolder(folderPath: string) {
  fireEvent.click(screen.getByRole('button', { name: 'Import folder' }));
  fireEvent.change(screen.getByLabelText('Folder path'), { target: { value: folderPath } });
  fireEvent.click(screen.getByRole('button', { name: 'Import' }));
  await waitFor(() => {
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
}

function enrichedRow(id: number, title: string): MovieSummary {
  return {
    id,
    title,
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
  };
}

describe('optimistic import placeholder', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['setInterval'] });
    focusManager.setFocused(true);
  });

  afterEach(() => {
    clearImportPlaceholder();
    focusManager.setFocused(true);
    vi.useRealTimers();
  });

  it('replaces an on-page placeholder with the server row', async () => {
    let calls = 0;
    server.use(
      http.get('/api/movies', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json(browseBody([duneEnriched]), { status: 200 });
        }
        return HttpResponse.json(browseBody([enrichedRow(99, 'Server Cut'), duneEnriched]), {
          status: 200,
        });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    await importFolder('F:/library/NewArrival');
    expect(screen.getByRole('heading', { name: 'NewArrival' })).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getByText('Pending'),
    ).toBeInTheDocument();

    await vi.advanceTimersByTimeAsync(2000);
    expect(await screen.findByRole('heading', { name: 'Server Cut' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'NewArrival' })).not.toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).queryByText('Pending'),
    ).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
  });

  it('drops an off-page placeholder and disarms polling', { timeout: 15_000 }, async () => {
    vi.useRealTimers();
    let calls = 0;
    server.use(
      http.get('/api/movies', () => {
        calls += 1;
        return HttpResponse.json(browseBody([duneEnriched]), { status: 200 });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    await importFolder('F:/library/NewArrival');
    expect(screen.getByRole('heading', { name: 'NewArrival' })).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByRole('heading', { name: 'NewArrival' })).not.toBeInTheDocument();
      },
      { timeout: 4000 },
    );
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    const callsAfterDrop = calls;
    expect(callsAfterDrop).toBeGreaterThan(1);

    await new Promise((resolve) => {
      setTimeout(resolve, 4000);
    });
    expect(calls).toBe(callsAfterDrop);
  });

  it('keeps polling armed until the first post-202 refetch', async () => {
    let calls = 0;
    let releaseRefetch: (() => void) | undefined;
    server.use(
      http.get('/api/movies', async () => {
        calls += 1;
        if (calls > 1) {
          await new Promise<void>((resolve) => {
            releaseRefetch = resolve;
          });
        }
        return HttpResponse.json(browseBody([duneEnriched]), { status: 200 });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(calls).toBe(1);

    await importFolder('F:/library/NewArrival');
    expect(screen.getByRole('heading', { name: 'NewArrival' })).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getByText('Pending'),
    ).toBeInTheDocument();
    expect(calls).toBe(1);

    await vi.advanceTimersByTimeAsync(2000);
    await waitFor(() => {
      expect(calls).toBe(2);
    });
    expect(screen.getByRole('heading', { name: 'NewArrival' })).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getByText('Pending'),
    ).toBeInTheDocument();

    releaseRefetch?.();
    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: 'NewArrival' })).not.toBeInTheDocument();
    });
    expect(calls).toBe(2);
  });

  it('replaces an earlier placeholder when another folder is imported', async () => {
    let imported = 0;
    server.use(
      http.get('/api/movies', () => HttpResponse.json(browseBody([duneEnriched]), { status: 200 })),
      http.post('/api/movies/import', () => {
        imported += 1;
        return new HttpResponse(null, {
          status: 202,
          headers: { Location: `/api/movies/${100 + imported}`, 'Content-Length': '0' },
        });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    await importFolder('F:/library/FirstCut');
    expect(screen.getByRole('heading', { name: 'FirstCut' })).toBeInTheDocument();

    await importFolder('F:/library/SecondCut');
    expect(screen.getByRole('heading', { name: 'SecondCut' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'FirstCut' })).not.toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Movies' })).getAllByText('Pending'),
    ).toHaveLength(1);
  });
});
