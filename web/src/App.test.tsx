import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, render, screen } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import type { paths } from './api/types';
import App from './App';
import { standingBrowseQuery } from './test/handlers';
import { server } from './test/server';

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
  cleanup();
});

describe('App library host', () => {
  it('shows the Import folder button, browse rows with badges, and a polite live region', async () => {
    renderApp();

    expect(screen.getByRole('button', { name: 'Import folder' })).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByText('Arrival')).toBeInTheDocument();
    expect(screen.getByText('Enriched')).toBeInTheDocument();
    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Retry / })).not.toBeInTheDocument();
    expect(screen.getByRole('list')).toHaveAttribute('aria-live', 'polite');
    expect(document.activeElement).not.toBe(screen.getByRole('list'));
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
              },
              {
                id: 2,
                title: 'Arrival',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'Pending',
              },
              {
                id: 3,
                title: 'Lost Film',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'NotFound',
              },
              {
                id: 4,
                title: 'Broken Cut',
                posterUrl: null,
                releaseYear: null,
                runtime: null,
                imdbRating: null,
                enrichmentStatus: 'Failed',
              },
            ],
            totalCount: 4,
          },
          { status: 200 },
        ),
      ),
    );

    renderApp();

    expect(await screen.findByText('Lost Film')).toBeInTheDocument();
    expect(screen.getByText('Not found')).toBeInTheDocument();
    expect(screen.getByText('Failed')).toBeInTheDocument();
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
