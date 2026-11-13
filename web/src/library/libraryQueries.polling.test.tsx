import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { components, paths } from '../api/types';
import App from '../App';
import { retryAcceptedHandler } from '../test/handlers';
import { server } from '../test/server';
import {
  useEnrichmentStatus,
  type EnrichmentConnectionFactory,
  type EnrichmentHubConnection,
  type EnrichmentStatusEvent,
} from './useEnrichmentStatus';

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
  review: 'none',
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
  review: 'none',
};

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
    review: 'none',
  };
}

function lostFilmRow(enrichmentStatus: MovieSummary['enrichmentStatus']): MovieSummary {
  return {
    id: 3,
    title: 'Lost Film',
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus,
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    review: 'none',
  };
}

function browseBody(items: MovieSummary[]): BrowseMoviesResponse {
  return { items, totalCount: items.length };
}

type EventHandler = (...args: unknown[]) => void;

function createControllableHub() {
  const handlers = new Map<string, EventHandler>();
  const connection: EnrichmentHubConnection = {
    start: async () => {},
    stop: async () => {},
    on: (eventName, handler) => {
      handlers.set(eventName, handler);
    },
    off: (eventName) => {
      handlers.delete(eventName);
    },
    onreconnected: () => {},
    onclose: () => {},
  };
  const factory: EnrichmentConnectionFactory = () => connection;
  return {
    factory,
    size: () => handlers.size,
    keys: () => [...handlers.keys()],
    publish(payload: EnrichmentStatusEvent) {
      handlers.get('EnrichmentStatusChanged')?.(payload);
    },
  };
}

function HubHarness({ factory }: { factory: EnrichmentConnectionFactory }) {
  const enrichmentStatus = useEnrichmentStatus(factory);
  return <App enrichmentStatus={enrichmentStatus} />;
}

function renderApp(factory?: EnrichmentConnectionFactory) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(factory ? <HubHarness factory={factory} /> : <App />, { wrapper: Wrapper });
}

function moviesList() {
  return screen.getByRole('list', { name: 'Movies' });
}

function rowText(title: string): string {
  const row = within(moviesList())
    .getAllByRole('listitem')
    .find((item) => item.textContent?.includes(title));
  return row?.textContent ?? '';
}

async function bulkImport(folderPath: string) {
  fireEvent.click(screen.getByRole('button', { name: 'Bulk import' }));
  const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
  fireEvent.click(await within(dialog).findByLabelText(folderPath));
  fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));
  await waitFor(() => {
    expect(within(dialog).getByRole('status')).toBeInTheDocument();
  });
  fireEvent.keyDown(dialog, { key: 'Escape' });
  await waitFor(() => {
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
}

describe('browse query without polling', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not issue interval-driven browse requests for a Pending row', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    let calls = 0;
    server.use(
      http.get('/api/movies', () => {
        calls += 1;
        return HttpResponse.json(browseBody([pendingArrival]), { status: 200 });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Arrival' })).toBeInTheDocument();
    expect(within(moviesList()).getByText('Pending')).toBeInTheDocument();
    expect(calls).toBe(1);

    await vi.advanceTimersByTimeAsync(10_000);
    expect(calls).toBe(1);
    expect(moviesList()).toHaveAttribute('aria-live', 'polite');
  });

  it('refreshes the grid from the bulk import mutation without waiting for a timer', async () => {
    let calls = 0;
    let releaseSecond: (() => void) | undefined;
    server.use(
      http.get('/api/movies/import/candidates', () =>
        HttpResponse.json({ newFolders: ['NewArrival'] }, { status: 200 }),
      ),
      http.post('/api/movies/import/library', () =>
        HttpResponse.json({ imported: 1, skipped: 0, retried: 0, failures: [] }, { status: 200 }),
      ),
      http.get('/api/movies', async ({ request }) => {
        const status = new URL(request.url).searchParams.getAll('Statuses');
        if (status.includes('Failed')) {
          return HttpResponse.json(browseBody([]), { status: 200 });
        }
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json(browseBody([duneEnriched]), { status: 200 });
        }
        await new Promise<void>((resolve) => {
          releaseSecond = resolve;
        });
        return HttpResponse.json(browseBody([enrichedRow(99, 'Server Cut'), duneEnriched]), {
          status: 200,
        });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    await bulkImport('NewArrival');

    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(moviesList()).toHaveAttribute('aria-live', 'polite');
    await waitFor(() => {
      expect(calls).toBe(2);
    });

    releaseSecond?.();
    expect(await screen.findByRole('heading', { name: 'Server Cut' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'NewArrival' })).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
  });

  it('refreshes the grid once the import-triggered refetch settles', async () => {
    let calls = 0;
    server.use(
      http.get('/api/movies/import/candidates', () =>
        HttpResponse.json({ newFolders: ['NewArrival'] }, { status: 200 }),
      ),
      http.post('/api/movies/import/library', () =>
        HttpResponse.json({ imported: 1, skipped: 0, retried: 0, failures: [] }, { status: 200 }),
      ),
      http.get('/api/movies', ({ request }) => {
        const status = new URL(request.url).searchParams.getAll('Statuses');
        if (status.includes('Failed')) {
          return HttpResponse.json(browseBody([]), { status: 200 });
        }
        calls += 1;
        return HttpResponse.json(browseBody([duneEnriched]), { status: 200 });
      }),
    );

    renderApp();
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    await bulkImport('NewArrival');

    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(calls).toBeGreaterThan(1);
  });
});

describe('event and mutation reconciliation without polling', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  function holdBrowseAfterFirst() {
    let calls = 0;
    let hold = false;
    let inflight = 0;
    const releases: Array<() => void> = [];
    let serverStatus: MovieSummary['enrichmentStatus'] = 'NotFound';

    server.use(
      http.get('/api/movies', async () => {
        calls += 1;
        inflight += 1;
        try {
          if (hold) {
            await new Promise<void>((resolve) => {
              releases.push(resolve);
            });
          }
          return HttpResponse.json(browseBody([lostFilmRow(serverStatus)]), { status: 200 });
        } finally {
          inflight -= 1;
        }
      }),
    );

    return {
      setHold(value: boolean) {
        hold = value;
      },
      setServerStatus(status: MovieSummary['enrichmentStatus']) {
        serverStatus = status;
      },
      releaseAll() {
        while (releases.length > 0) {
          releases.shift()?.();
        }
      },
      calls: () => calls,
      inflight: () => inflight,
    };
  }

  async function settle(browse: ReturnType<typeof holdBrowseAfterFirst>) {
    browse.setHold(false);
    let previous = -1;
    let stableRuns = 0;
    for (let attempt = 0; attempt < 40; attempt += 1) {
      browse.releaseAll();
      await new Promise((resolve) => setTimeout(resolve, 10));
      browse.releaseAll();
      if (browse.calls() === previous && browse.inflight() === 0) {
        stableRuns += 1;
        if (stableRuns >= 2) {
          break;
        }
      } else {
        stableRuns = 0;
      }
      previous = browse.calls();
    }
    browse.releaseAll();
    return browse.calls();
  }

  async function expectNoIntervalRequests(browse: ReturnType<typeof holdBrowseAfterFirst>) {
    const settledCalls = await settle(browse);
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    await vi.advanceTimersByTimeAsync(10_000);
    expect(browse.calls()).toBe(settledCalls);
    vi.useRealTimers();
  }

  it('converges when the event arrives before the retry mutation response', async () => {
    const browse = holdBrowseAfterFirst();
    const hub = createControllableHub();
    let releaseRetry: (() => void) | undefined;
    server.use(
      http.post('/api/movies/{id}/enrichment', async () => {
        await new Promise<void>((resolve) => {
          releaseRetry = resolve;
        });
        return new HttpResponse(null, {
          status: 202,
          headers: { 'Content-Length': '0' },
        });
      }),
    );

    renderApp(hub.factory);
    fireEvent.click(await screen.findByRole('button', { name: 'Retry Lost Film' }));
    browse.setHold(true);

    browse.setServerStatus('Enriched');
    hub.publish({ id: 3, enrichmentStatus: 'Enriched' });

    await waitFor(
      () => {
        expect(browse.calls()).toBeGreaterThan(1);
      },
      { timeout: 5000 },
    );
    releaseRetry?.();
    await settle(browse);

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'Retry Lost Film' })).not.toBeInTheDocument();
      expect(rowText('Lost Film')).not.toContain('Pending');
    });
    expect(rowText('Lost Film')).not.toContain('Pending');
    await expectNoIntervalRequests(browse);
  });

  it('converges when the retry mutation response arrives before the event', async () => {
    const browse = holdBrowseAfterFirst();
    const hub = createControllableHub();
    server.use(retryAcceptedHandler);

    renderApp(hub.factory);
    fireEvent.click(await screen.findByRole('button', { name: 'Retry Lost Film' }));
    browse.setHold(true);

    await waitFor(() => {
      expect(browse.calls()).toBeGreaterThan(1);
    });
    expect(rowText('Lost Film')).toContain('Pending');

    browse.setServerStatus('Enriched');
    hub.publish({ id: 3, enrichmentStatus: 'Enriched' });
    browse.releaseAll();

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'Retry Lost Film' })).not.toBeInTheDocument();
      expect(rowText('Lost Film')).not.toContain('Pending');
    });

    await settle(browse);
    expect(rowText('Lost Film')).not.toContain('Pending');
    await expectNoIntervalRequests(browse);
  });
});
