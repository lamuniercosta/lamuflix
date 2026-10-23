import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import {
  QueryClient,
  QueryClientProvider,
  QueryObserver,
  type QueryKey,
} from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { ReactElement } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { server } from '../test/server';
import MovieCard from './MovieCard';
import { __resetMovieFeedbackStore, MOVIE_FEEDBACK_REFRESH_ERROR } from './movieFeedbackStore';

const movie = {
  id: 1,
  title: 'Heat',
  posterUrl: '/posters/heat.jpg',
  releaseYear: 1995,
  runtime: 170,
  imdbRating: 8.3,
  enrichmentStatus: 'Enriched',
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
} as const;

function renderCard(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return {
    ...render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>),
    client,
  };
}

function deferred<T = void>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

const observers: Array<() => void> = [];

function watch(
  queryClient: QueryClient,
  queryKey: QueryKey,
  queryFn: () => Promise<unknown>,
): void {
  const observer = new QueryObserver(queryClient, { queryKey, queryFn, retry: false });
  observers.push(observer.subscribe(() => undefined));
}

beforeEach(() => {
  __resetMovieFeedbackStore();
});

afterEach(() => {
  while (observers.length > 0) {
    observers.pop()?.();
  }
  __resetMovieFeedbackStore();
});

describe('MovieCard', () => {
  it('renders the poster, title, rating and year with no badge or Details link', () => {
    renderCard(<MovieCard movie={movie} />);

    expect(screen.getByRole('heading', { level: 3, name: 'Heat' })).toBeInTheDocument();
    expect(screen.getByAltText('Heat')).toHaveAttribute('src', '/posters/heat.jpg');
    expect(screen.getByText('1995')).toBeInTheDocument();
    expect(screen.getByText('8.3')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Play Heat' })).toBeInTheDocument();
    expect(screen.queryByText('Enriched')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Details' })).not.toBeInTheDocument();
    expect(screen.queryByText('170 min')).not.toBeInTheDocument();
  });

  it('renders a titled placeholder and dashes for missing values', () => {
    renderCard(
      <MovieCard
        movie={{
          ...movie,
          posterUrl: null,
          releaseYear: null,
          runtime: null,
          imdbRating: null,
          enrichmentStatus: 'Pending',
        }}
      />,
    );

    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.queryByAltText('Heat')).not.toBeInTheDocument();
    expect(screen.getAllByText('—')).toHaveLength(2);
    expect(screen.getByText('Pending')).toBeInTheDocument();
  });

  it('replaces the poster with the placeholder when the image fails to load', () => {
    renderCard(<MovieCard movie={movie} />);

    fireEvent.error(screen.getByAltText('Heat'));

    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.queryByAltText('Heat')).not.toBeInTheDocument();
  });

  it('exposes the IMDb rating as an accessible label', () => {
    const { container } = renderCard(<MovieCard movie={movie} />);
    expect(container.querySelector('[aria-label="IMDb rating 8.3"]')).toBeInTheDocument();
  });

  it.each(['NotFound', 'Failed'] as const)('offers Retry for enrichment status %s', (status) => {
    renderCard(<MovieCard movie={{ ...movie, enrichmentStatus: status }} />);
    expect(screen.getByRole('button', { name: 'Retry Heat' })).toBeInTheDocument();
  });

  it('shows the forbidden playback message inline on the card without a dialog', async () => {
    server.use(http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 403 })));
    renderCard(<MovieCard movie={movie} />);

    fireEvent.click(screen.getByRole('button', { name: 'Play Heat' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Playback is not available for this title.',
    );
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});

describe('MovieCard feedback actions', () => {
  it('renders both feedback actions on a Library card', () => {
    // arrange / act
    renderCard(<MovieCard movie={movie} />);

    // assert
    expect(screen.getByRole('button', { name: 'Mark Watched, Heat' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Not Interested, Heat' })).toBeEnabled();
    expect(screen.queryByText('Watched')).not.toBeInTheDocument();
  });

  it('marks Watched optimistically before the response and keeps the other flag', async () => {
    // arrange
    const gate = deferred();
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        posts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={{ ...movie, isNotInterested: true }} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Heat' }));

    // assert
    expect(screen.getByRole('button', { name: 'Watched, Heat' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Not Interested (marked), Heat' })).toBeDisabled();
    await waitFor(() => expect(posts).toBe(1));

    gate.resolve();

    await waitFor(() =>
      expect(
        screen.getByRole('button', { name: 'Not Interested (marked), Heat' }),
      ).not.toHaveAttribute('aria-busy'),
    );
    expect(screen.getByRole('button', { name: 'Watched, Heat' })).toBeDisabled();
  });

  it('does not dispatch when an already-marked action is activated', () => {
    // arrange
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
      http.post('*/api/movies/:id/not-interested', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={{ ...movie, isWatched: true, isNotInterested: true }} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Watched, Heat' }));
    fireEvent.click(screen.getByRole('button', { name: 'Not Interested (marked), Heat' }));

    // assert
    expect(posts).toBe(0);
  });

  it('keeps native keyboard activation available for both actions', async () => {
    // arrange
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/not-interested', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={movie} />);
    const notInterested = screen.getByRole('button', { name: 'Not Interested, Heat' });

    // act
    notInterested.focus();

    // assert
    expect(notInterested).toHaveAttribute('type', 'button');
    expect(document.activeElement).toBe(notInterested);
    expect(fireEvent.keyDown(notInterested, { key: 'Enter' })).toBe(true);
    expect(fireEvent.keyDown(notInterested, { key: ' ' })).toBe(true);

    fireEvent.click(notInterested);

    await waitFor(() => expect(posts).toBe(1));
  });

  it('keeps the confirmed mark and retries only the refresh after a refresh failure', async () => {
    // arrange
    let posts = 0;
    let fetches = 0;
    let refreshFails = true;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { client } = renderCard(<MovieCard movie={movie} />);
    watch(client, ['browseMovies', { page: 1 }], () => {
      fetches += 1;
      if (fetches === 1 || !refreshFails) {
        return Promise.resolve({ items: [], totalCount: 0 });
      }
      return Promise.reject(new Error('refresh failed'));
    });
    await waitFor(() => expect(fetches).toBe(1));

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Heat' }));

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_REFRESH_ERROR);
    expect(screen.getByRole('button', { name: 'Watched, Heat' })).toBeVisible();
    expect(
      screen.queryByRole('button', { name: 'Retry saving feedback, Heat' }),
    ).not.toBeInTheDocument();

    refreshFails = false;
    fireEvent.click(screen.getByRole('button', { name: 'Retry refresh, Heat' }));

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Watched, Heat' })).toBeVisible();
    expect(fetches).toBe(3);
    expect(posts).toBe(1);
  });

  it('renders no fabricated reasons on ordinary Library data', () => {
    // arrange / act
    const { container } = renderCard(<MovieCard movie={movie} />);

    // assert
    expect(container.querySelector('.related-movie-reason')).toBeNull();
  });

  it('keeps feedback activation isolated from parent-card navigation', async () => {
    // arrange
    let posts = 0;
    const onOpenMovie = vi.fn();
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={movie} onOpenMovie={onOpenMovie} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Heat' }));

    // assert
    await waitFor(() => expect(posts).toBe(1));
    expect(onOpenMovie).not.toHaveBeenCalled();
  });
});
