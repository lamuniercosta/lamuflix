import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import type { components, paths } from '../api/types';
import App from '../App';
import { retryAcceptedHandler, retryConflictHandler, retryNotFoundHandler } from '../test/handlers';
import { server } from '../test/server';
import { browseMoviesQueryKey, browseRefetchInterval, GENERIC_RETRY_ERROR } from './libraryQueries';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

const lostFilm: MovieSummary = {
  id: 3,
  title: 'Lost Film',
  posterUrl: null,
  releaseYear: null,
  runtime: null,
  imdbRating: null,
  enrichmentStatus: 'NotFound',
};

const brokenCut: MovieSummary = {
  id: 4,
  title: 'Broken Cut',
  posterUrl: null,
  releaseYear: null,
  runtime: null,
  imdbRating: null,
  enrichmentStatus: 'Failed',
};

const dune: MovieSummary = {
  id: 1,
  title: 'Dune',
  posterUrl: null,
  releaseYear: null,
  runtime: null,
  imdbRating: null,
  enrichmentStatus: 'Enriched',
};

const arrival: MovieSummary = {
  id: 2,
  title: 'Arrival',
  posterUrl: null,
  releaseYear: null,
  runtime: null,
  imdbRating: null,
  enrichmentStatus: 'Pending',
};

function browseBody(items: MovieSummary[]): BrowseMoviesResponse {
  return { items, totalCount: items.length };
}

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return { client, ...render(<App />, { wrapper: Wrapper }) };
}

function useRetryableBrowse() {
  server.use(
    http.get('/api/movies', () =>
      HttpResponse.json(browseBody([dune, arrival, lostFilm, brokenCut]), { status: 200 }),
    ),
  );
}

describe('LibraryView retry', () => {
  it('shows Retry only for NotFound and Failed rows with row-specific names', async () => {
    useRetryableBrowse();
    renderApp();

    expect(await screen.findByRole('button', { name: 'Retry Lost Film' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry Broken Cut' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry Dune' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry Arrival' })).not.toBeInTheDocument();
  });

  it('sets the retried row to Pending on 202 so polling re-arms', async () => {
    useRetryableBrowse();
    server.use(retryAcceptedHandler);
    const { client } = renderApp();

    fireEvent.click(await screen.findByRole('button', { name: 'Retry Lost Film' }));

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'Retry Lost Film' })).not.toBeInTheDocument();
    });

    const row = screen
      .getAllByRole('listitem')
      .find((item) => item.textContent?.includes('Lost Film'));
    expect(row).toHaveTextContent('Pending');
    expect(
      browseRefetchInterval({
        state: { data: client.getQueryData(browseMoviesQueryKey) },
      }),
    ).toBe(2000);
  });

  it('shows a described inline row alert on 404 and invalidates browse', async () => {
    let browseCalls = 0;
    server.use(
      http.get('/api/movies', () => {
        browseCalls += 1;
        return HttpResponse.json(browseBody([dune, arrival, lostFilm, brokenCut]), { status: 200 });
      }),
      retryNotFoundHandler,
    );
    renderApp();

    fireEvent.click(await screen.findByRole('button', { name: 'Retry Lost Film' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_RETRY_ERROR);
    const retry = screen.getByRole('button', { name: 'Retry Lost Film' });
    expect(retry).toHaveAttribute('aria-describedby', alert.id);
    await waitFor(() => {
      expect(browseCalls).toBeGreaterThan(1);
    });
  });

  it('shows a described inline row alert on 409 and invalidates browse', async () => {
    let browseCalls = 0;
    server.use(
      http.get('/api/movies', () => {
        browseCalls += 1;
        return HttpResponse.json(browseBody([dune, arrival, lostFilm, brokenCut]), { status: 200 });
      }),
      retryConflictHandler,
    );
    renderApp();

    fireEvent.click(await screen.findByRole('button', { name: 'Retry Broken Cut' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_RETRY_ERROR);
    expect(screen.getByRole('button', { name: 'Retry Broken Cut' })).toHaveAttribute(
      'aria-describedby',
      alert.id,
    );
    await waitFor(() => {
      expect(browseCalls).toBeGreaterThan(1);
    });
  });
});
