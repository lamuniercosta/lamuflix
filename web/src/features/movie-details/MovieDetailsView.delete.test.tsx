import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { browseMoviesQueryKey, standingBrowseQuery } from '../../library/libraryQueries';
import { movieDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import MovieDetailsView from './MovieDetailsView';

const watchlistFilteredKey = [
  'browseMovies',
  { ...standingBrowseQuery, InWatchlist: 'true' },
] as const;

function renderView() {
  const libraryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const rendered = render(
    <QueryClientProvider client={libraryClient}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
  return { libraryClient, ...rendered };
}

async function openAndConfirmRemoval() {
  fireEvent.click(screen.getByRole('button', { name: 'Remove from library' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Remove' }));
}

afterEach(() => {
  window.history.replaceState({}, '', '/movies/1');
});

describe('MovieDetailsView destructive removal', () => {
  it('confirms removal, invalidates the details/browse/watchlist caches, and returns to the library', async () => {
    let deleteCalls = 0;
    let getCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => {
        getCalls += 1;
        return HttpResponse.json(movieDetails);
      }),
      http.delete('*/api/movies/:id', () => {
        deleteCalls += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { libraryClient } = renderView();
    libraryClient.setQueryData(browseMoviesQueryKey, { items: [], totalCount: 0 });
    libraryClient.setQueryData(watchlistFilteredKey, { items: [], totalCount: 0 });

    await screen.findByRole('heading', { name: 'Dune' });
    await openAndConfirmRemoval();

    await waitFor(() => expect(deleteCalls).toBe(1));
    await waitFor(() => expect(window.location.pathname).toBe('/'));
    await waitFor(() =>
      expect(libraryClient.getQueryState(browseMoviesQueryKey)?.isInvalidated).toBe(true),
    );
    expect(libraryClient.getQueryState(watchlistFilteredKey)?.isInvalidated).toBe(true);
    await waitFor(() => expect(getCalls).toBeGreaterThan(1));
  });

  it('sends no delete request when the confirmation is cancelled', async () => {
    let deleteCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetails)),
      http.delete('*/api/movies/:id', () => {
        deleteCalls += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Remove from library' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(deleteCalls).toBe(0);
    expect(window.location.pathname).toBe('/movies/1');
  });

  it('sends only one delete while the request is in flight', async () => {
    let deleteCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetails)),
      http.delete('*/api/movies/:id', async () => {
        deleteCalls += 1;
        await new Promise((resolve) => setTimeout(resolve, 100));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Remove from library' }));
    const confirm = await screen.findByRole('button', { name: 'Remove' });
    fireEvent.click(confirm);
    fireEvent.click(confirm);

    await waitFor(() => expect(window.location.pathname).toBe('/'));
    expect(deleteCalls).toBe(1);
  });

  it('shows an error and a retry for a failure other than 404, and stays on details', async () => {
    let deleteCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetails)),
      http.delete('*/api/movies/:id', () => {
        deleteCalls += 1;
        return deleteCalls === 1
          ? new HttpResponse(null, { status: 500 })
          : new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    await openAndConfirmRemoval();

    expect(await screen.findByText('Could not remove this movie.')).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Could not remove this movie.');
    expect(window.location.pathname).toBe('/movies/1');

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(deleteCalls).toBe(2));
    await waitFor(() => expect(window.location.pathname).toBe('/'));
  });

  it('treats a DELETE 404 as confirmed absence with no error, no retry, and invalidated caches', async () => {
    let getCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => {
        getCalls += 1;
        return HttpResponse.json(movieDetails);
      }),
      http.delete('*/api/movies/:id', () => new HttpResponse(null, { status: 404 })),
    );
    const { libraryClient } = renderView();
    libraryClient.setQueryData(browseMoviesQueryKey, { items: [], totalCount: 0 });

    await screen.findByRole('heading', { name: 'Dune' });
    await openAndConfirmRemoval();

    await waitFor(() => expect(window.location.pathname).toBe('/'));
    expect(screen.queryByText('Could not remove this movie.')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    await waitFor(() =>
      expect(libraryClient.getQueryState(browseMoviesQueryKey)?.isInvalidated).toBe(true),
    );
    await waitFor(() => expect(getCalls).toBeGreaterThan(1));
  });
});
