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
import { __resetWatchlistStore } from './watchlistStore';

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
  review: 'none',
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
  __resetWatchlistStore();
});

afterEach(() => {
  while (observers.length > 0) {
    observers.pop()?.();
  }
  __resetMovieFeedbackStore();
  __resetWatchlistStore();
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

  it('falls back through the thumbnail to the placeholder when images fail to load', () => {
    renderCard(<MovieCard movie={movie} />);

    fireEvent.error(screen.getByAltText('Heat'));
    expect(screen.getByAltText('Heat')).toHaveAttribute('src', '/api/movies/1/thumbnail');

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

  it('navigates to details with player intent from Play without posting the launch contract', async () => {
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/play', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const onOpenMovie = vi.fn();
    renderCard(<MovieCard movie={movie} onOpenMovie={onOpenMovie} />);

    fireEvent.click(screen.getByRole('button', { name: 'Play Heat' }));

    expect(onOpenMovie).toHaveBeenCalledTimes(1);
    expect(onOpenMovie).toHaveBeenCalledWith(1, { playerIntent: true });
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(posts).toBe(0);
  });

  it('navigates with player intent even without a host callback and posts nothing', async () => {
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/play', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    window.history.replaceState(null, '', '/');
    renderCard(<MovieCard movie={movie} />);

    fireEvent.click(screen.getByRole('button', { name: 'Play Heat' }));

    expect(window.location.pathname).toBe('/movies/1');
    expect(window.history.state).toEqual({ libraryReturnUrl: '/', playerIntent: true });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(posts).toBe(0);
  });
});

describe('MovieCard feedback actions', () => {
  it('renders the shared four-icon row on a Library card', () => {
    // arrange / act
    const { container } = renderCard(<MovieCard movie={movie} />);

    // assert
    expect(screen.getByRole('button', { name: 'Mark Watched, Heat' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Like, Heat' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Dislike, Heat' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Add to Watchlist, Heat' })).toBeEnabled();
    const row = container.querySelector('.movie-feedback-actions') as HTMLElement;
    expect(row.querySelectorAll('button')).toHaveLength(4);
  });

  it('renders no bulky Watched or Not interested text buttons on the card', () => {
    // arrange / act
    const { container } = renderCard(<MovieCard movie={movie} />);

    // assert
    expect(container.textContent).not.toContain('Not Interested');
    expect(container.querySelector('.movie-card-watch')).toBeNull();
  });

  it('marks Watched optimistically before the response and keeps the review', async () => {
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
    renderCard(<MovieCard movie={{ ...movie, review: 'liked' }} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Heat' }));

    // assert
    expect(screen.getByRole('button', { name: 'Unmark Watched, Heat' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Clear liked review, Heat' })).toBeDisabled();
    await waitFor(() => expect(posts).toBe(1));

    gate.resolve();

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Clear liked review, Heat' })).not.toHaveAttribute(
        'aria-busy',
      ),
    );
    expect(screen.getByRole('button', { name: 'Unmark Watched, Heat' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Clear liked review, Heat' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('switches the review thumbs without changing Watched on the card', async () => {
    // arrange
    const bodies: string[] = [];
    server.use(
      http.put('*/api/movies/:id/review', async ({ request }) => {
        bodies.push(((await request.json()) as { review: string }).review);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={{ ...movie, isWatched: true }} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Dislike, Heat' }));

    // assert
    expect(screen.getByRole('button', { name: 'Clear disliked review, Heat' })).toBeDisabled();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Clear disliked review, Heat' })).toBeEnabled(),
    );
    expect(screen.getByRole('button', { name: 'Unmark Watched, Heat' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(bodies).toEqual(['disliked']);
  });

  it('un-marks Watched on the marked control and keeps the review', async () => {
    // arrange
    let deletes = 0;
    let puts = 0;
    server.use(
      http.delete('*/api/movies/:id/watched', () => {
        deletes += 1;
        return new HttpResponse(null, { status: 204 });
      }),
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={{ ...movie, isWatched: true, review: 'disliked' }} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Unmark Watched, Heat' }));

    // assert
    expect(screen.getByRole('button', { name: 'Mark Watched, Heat' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Clear disliked review, Heat' })).toBeDisabled();
    await waitFor(() => expect(deletes).toBe(1));
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Mark Watched, Heat' })).toBeEnabled(),
    );
    expect(screen.getByRole('button', { name: 'Clear disliked review, Heat' })).toBeEnabled();
    expect(puts).toBe(0);
  });

  it('keeps native keyboard activation available across the icon row', async () => {
    // arrange
    let puts = 0;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={movie} />);
    const like = screen.getByRole('button', { name: 'Like, Heat' });

    // act
    like.focus();

    // assert
    expect(like).toHaveAttribute('type', 'button');
    expect(document.activeElement).toBe(like);
    expect(fireEvent.keyDown(like, { key: 'Enter' })).toBe(true);
    expect(fireEvent.keyDown(like, { key: ' ' })).toBe(true);

    fireEvent.click(like);

    await waitFor(() => expect(puts).toBe(1));
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
    expect(screen.getByRole('button', { name: 'Unmark Watched, Heat' })).toBeVisible();
    expect(
      screen.queryByRole('button', { name: 'Retry saving feedback, Heat' }),
    ).not.toBeInTheDocument();

    refreshFails = false;
    fireEvent.click(screen.getByRole('button', { name: 'Retry refresh, Heat' }));

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Unmark Watched, Heat' })).toBeVisible();
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
    let puts = 0;
    const onOpenMovie = vi.fn();
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderCard(<MovieCard movie={movie} onOpenMovie={onOpenMovie} />);

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Like, Heat' }));

    // assert
    await waitFor(() => expect(puts).toBe(1));
    expect(onOpenMovie).not.toHaveBeenCalled();
  });
});
