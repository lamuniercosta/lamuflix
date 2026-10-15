import { focusManager, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { components, paths } from '../api/types';
import App from '../App';
import { server } from '../test/server';
import { browseRefetchInterval } from './libraryQueries';

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

      expect(await screen.findByText('Arrival')).toBeInTheDocument();
      expect(screen.getByText('Pending')).toBeInTheDocument();
      expect(calls).toBe(1);

      await waitFor(
        () => {
          expect(screen.getByText('Enriched')).toBeInTheDocument();
        },
        { timeout: 4000 },
      );
      expect(screen.queryByText('Pending')).not.toBeInTheDocument();
      expect(calls).toBe(2);

      const list = screen.getByRole('list');
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
    expect(await screen.findByText('Dune')).toBeInTheDocument();
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
    expect(await screen.findByText('Pending')).toBeInTheDocument();
    expect(calls).toBe(1);

    focusManager.setFocused(false);
    await vi.advanceTimersByTimeAsync(4000);
    expect(calls).toBe(1);
  });
});
