import {
  QueryClient,
  QueryClientProvider,
  QueryObserver,
  type QueryKey,
} from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { act } from 'react';
import { HttpResponse, http } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { server } from '../test/server';
import {
  MOVIE_FEEDBACK_REFRESH_ERROR,
  MOVIE_FEEDBACK_SAVE_ERROR,
  __resetMovieFeedbackStore,
  type MovieFeedbackClients,
} from './movieFeedbackStore';
import { __resetWatchlistStore } from './watchlistStore';
import { MovieFeedbackActions } from './MovieFeedbackActions';

const WATCHED_IDLE = 'Mark Watched, Heat';
const WATCHED_MARKED = 'Unmark Watched, Heat';
const LIKE_IDLE = 'Like, Heat';
const LIKE_MARKED = 'Clear liked review, Heat';
const DISLIKE_IDLE = 'Dislike, Heat';
const DISLIKE_MARKED = 'Clear disliked review, Heat';
const WATCHLIST_IDLE = 'Add to Watchlist, Heat';
const WATCHLIST_MARKED = 'Remove from Watchlist, Heat';
const RETRY_SAVE = 'Retry saving feedback, Heat';
const RETRY_REFRESH = 'Retry refresh, Heat';

const here = dirname(fileURLToPath(import.meta.url));
const actionsCss = readFileSync(join(here, 'MovieFeedbackActions.css'), 'utf8');

function makeQueryClient(): QueryClient {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

function deferred<T = void>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

interface ActionsOptions {
  isWatched?: boolean;
  review?: string;
  isInWatchlist?: boolean;
  clients?: MovieFeedbackClients;
}

function renderActions(client: QueryClient, options: ActionsOptions = {}) {
  return render(
    <QueryClientProvider client={client}>
      <MovieFeedbackActions
        movieId={1}
        title="Heat"
        isWatched={options.isWatched ?? false}
        review={options.review ?? 'none'}
        isInWatchlist={options.isInWatchlist ?? false}
        clients={options.clients}
      />
    </QueryClientProvider>,
  );
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

describe('MovieFeedbackActions four-icon row', () => {
  it('renders exactly four icon controls in the Q9 order with exact accessible names', () => {
    // arrange / act
    const { container } = renderActions(makeQueryClient());

    // assert
    const row = container.querySelector('.movie-feedback-actions');
    expect(row).not.toBeNull();
    const icons = (row as HTMLElement).querySelectorAll('button.movie-feedback-icon');
    expect(icons).toHaveLength(4);
    expect(Array.from(icons).map((icon) => icon.getAttribute('aria-label'))).toEqual([
      WATCHED_IDLE,
      LIKE_IDLE,
      DISLIKE_IDLE,
      WATCHLIST_IDLE,
    ]);
  });

  it('renders no bulky text feedback buttons', () => {
    // arrange / act
    const { container } = renderActions(makeQueryClient());

    // assert
    expect(container.textContent).not.toContain('Not interested');
    expect(container.textContent).not.toContain('Not Interested');
    const row = container.querySelector('.movie-feedback-actions') as HTMLElement;
    expect(row.querySelectorAll('.lamu-action-quiet')).toHaveLength(0);
  });

  it('marks pressed state independently for each icon from the supplied review and flags', () => {
    // arrange / act
    renderActions(makeQueryClient(), { isWatched: true, review: 'liked', isInWatchlist: true });

    // assert
    expect(screen.getByRole('button', { name: WATCHED_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: LIKE_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: DISLIKE_IDLE })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
    expect(screen.getByRole('button', { name: WATCHLIST_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('treats an unrecognised review value as None with both thumbs inactive', () => {
    // arrange / act
    renderActions(makeQueryClient(), { review: 'unexpected' });

    // assert
    expect(screen.getByRole('button', { name: LIKE_IDLE })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
    expect(screen.getByRole('button', { name: DISLIKE_IDLE })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
  });
});

describe('MovieFeedbackActions tooltips', () => {
  it('renders a tooltip per icon whose text equals the accessible name', () => {
    // arrange / act
    const { container } = renderActions(makeQueryClient());

    // assert
    const actions = Array.from(
      container.querySelectorAll('.movie-feedback-action'),
    ) as HTMLElement[];
    expect(actions).toHaveLength(4);
    for (const action of actions) {
      const button = action.querySelector('button') as HTMLElement;
      const tooltip = action.querySelector('.movie-feedback-tooltip') as HTMLElement;
      expect(tooltip).not.toBeNull();
      expect(tooltip.textContent).toBe(button.getAttribute('aria-label'));
      expect(tooltip).toHaveAttribute('aria-hidden', 'true');
    }
    expect(
      actions.map((action) => action.querySelector('.movie-feedback-tooltip')?.textContent),
    ).toEqual([WATCHED_IDLE, LIKE_IDLE, DISLIKE_IDLE, WATCHLIST_IDLE]);
  });

  it('reveals the tooltip on pointer hover and on keyboard focus', () => {
    // arrange / act
    const hoverRule = /\.movie-feedback-action:hover\s+\.movie-feedback-tooltip/;
    const focusRule = /\.movie-feedback-action:focus-within\s+\.movie-feedback-tooltip/;

    // assert
    expect(actionsCss).toMatch(hoverRule);
    expect(actionsCss).toMatch(focusRule);
  });

  it('keeps exact Q9 casing without a text transform', () => {
    // arrange / act
    const transform = /text-transform/;

    // assert
    expect(actionsCss).not.toMatch(transform);
  });

  it('keeps the watchlist tooltip in step with an optimistic toggle', async () => {
    // arrange
    server.use(
      http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );
    const { container } = renderActions(makeQueryClient());

    // act
    fireEvent.click(screen.getByRole('button', { name: WATCHLIST_IDLE }));

    // assert
    const button = screen.getByRole('button', { name: WATCHLIST_MARKED });
    expect(button).toHaveAttribute('aria-pressed', 'true');
    const tooltips = Array.from(container.querySelectorAll('.movie-feedback-tooltip')).map(
      (tooltip) => tooltip.textContent,
    );
    expect(tooltips).toEqual([WATCHED_IDLE, LIKE_IDLE, DISLIKE_IDLE, WATCHLIST_MARKED]);
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
  });
});

describe('MovieFeedbackActions thumbs mutual exclusion', () => {
  it('keeps only Disliked pressed after activating Like and then Dislike', async () => {
    // arrange
    const reviewBodies: Array<{ review: string }> = [];
    server.use(
      http.put('*/api/movies/:id/review', async ({ request }) => {
        reviewBodies.push((await request.json()) as { review: string });
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderActions(makeQueryClient());

    // act
    fireEvent.click(screen.getByRole('button', { name: LIKE_IDLE }));
    await waitFor(() => expect(screen.getByRole('button', { name: LIKE_MARKED })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: DISLIKE_IDLE }));
    await waitFor(() => expect(screen.getByRole('button', { name: DISLIKE_MARKED })).toBeEnabled());

    // assert
    expect(screen.getByRole('button', { name: DISLIKE_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: LIKE_IDLE })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
    expect(reviewBodies).toEqual([{ review: 'liked' }, { review: 'disliked' }]);
  });

  it('clears the active thumb to None without touching Watched or Watchlist', async () => {
    // arrange
    const bodies: string[] = [];
    server.use(
      http.put('*/api/movies/:id/review', async ({ request }) => {
        bodies.push(((await request.json()) as { review: string }).review);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderActions(makeQueryClient(), { isWatched: true, review: 'liked', isInWatchlist: true });

    // act
    fireEvent.click(screen.getByRole('button', { name: LIKE_MARKED }));

    // assert
    await waitFor(() => expect(screen.getByRole('button', { name: LIKE_IDLE })).toBeEnabled());
    expect(screen.getByRole('button', { name: LIKE_IDLE })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
    expect(screen.getByRole('button', { name: WATCHED_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: WATCHLIST_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(bodies).toEqual(['none']);
  });

  it('never changes the thumbs when Watched is toggled', async () => {
    // arrange
    server.use(
      http.post('*/api/movies/:id/watched', () => new HttpResponse(null, { status: 204 })),
    );
    renderActions(makeQueryClient(), { review: 'disliked' });

    // act
    fireEvent.click(screen.getByRole('button', { name: WATCHED_IDLE }));

    // assert
    await waitFor(() => expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeEnabled());
    expect(screen.getByRole('button', { name: DISLIKE_MARKED })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });
});

describe('MovieFeedbackActions keyboard and focus', () => {
  it('keeps native Enter and Space activation available across the icon row', async () => {
    // arrange
    let reviewCalls = 0;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        reviewCalls += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderActions(makeQueryClient());
    const like = screen.getByRole('button', { name: LIKE_IDLE });

    // act
    like.focus();

    // assert
    expect(like.tagName).toBe('BUTTON');
    expect(like).toHaveAttribute('type', 'button');
    expect(document.activeElement).toBe(like);
    expect(fireEvent.keyDown(like, { key: 'Enter' })).toBe(true);
    expect(fireEvent.keyDown(like, { key: ' ' })).toBe(true);

    fireEvent.click(like);

    await waitFor(() => expect(reviewCalls).toBe(1));
  });

  it('moves focus onto the icons and leaves the shared focus treatment intact', () => {
    // arrange
    renderActions(makeQueryClient());
    const like = screen.getByRole('button', { name: LIKE_IDLE });
    const dislike = screen.getByRole('button', { name: DISLIKE_IDLE });

    // act
    like.focus();

    // assert
    expect(document.activeElement).toBe(like);
    expect(like.style.outline).toBe('');

    dislike.focus();

    expect(document.activeElement).toBe(dislike);
    expect(dislike.style.outline).toBe('');
    expect(actionsCss).toMatch(/\.movie-feedback-icon:focus-visible/);
  });
});

describe('MovieFeedbackActions pending and error states', () => {
  it('announces the pending state and disables every icon for that movie', async () => {
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
    renderActions(makeQueryClient());

    // act
    fireEvent.click(screen.getByRole('button', { name: WATCHED_IDLE }));

    // assert
    const pendingWatched = screen.getByRole('button', { name: WATCHED_MARKED });
    expect(pendingWatched).toBeDisabled();
    expect(pendingWatched).toHaveAttribute('aria-busy', 'true');
    expect(screen.getByRole('button', { name: LIKE_IDLE })).toBeDisabled();
    expect(screen.getByRole('button', { name: DISLIKE_IDLE })).toBeDisabled();
    expect(screen.getByRole('button', { name: WATCHLIST_IDLE })).toBeDisabled();
    expect(screen.getByRole('status')).toHaveTextContent('Saving feedback for Heat');

    gate.resolve();

    await waitFor(() => expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeVisible());
    expect(screen.getByRole('status')).toHaveTextContent('');
    expect(posts).toBe(1);
  });

  it('rolls the review back provisionally and retries once from the error block', async () => {
    // arrange
    let calls = 0;
    const retryGate = deferred();
    server.use(
      http.put('*/api/movies/:id/review', async () => {
        calls += 1;
        if (calls === 1) {
          return new HttpResponse(null, { status: 500 });
        }
        await retryGate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderActions(makeQueryClient());

    // act
    fireEvent.click(screen.getByRole('button', { name: LIKE_IDLE }));

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_SAVE_ERROR);
    expect(screen.getByRole('button', { name: LIKE_IDLE })).toBeEnabled();
    expect(screen.getByRole('button', { name: LIKE_IDLE })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
    const retry = screen.getByRole('button', { name: RETRY_SAVE });
    const before = calls;

    act(() => {
      retry.click();
      retry.click();
    });

    await waitFor(() => expect(calls).toBe(before + 1));

    retryGate.resolve();

    await waitFor(() => expect(screen.getByRole('button', { name: LIKE_MARKED })).toBeVisible());
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(calls).toBe(before + 1);
  });

  it('issues no retry while reconciliation is still active', async () => {
    // arrange
    let calls = 0;
    let unblockRefresh!: () => void;
    const refreshGate = new Promise<unknown>((resolve) => {
      unblockRefresh = () => resolve([]);
    });
    server.use(
      http.put('*/api/movies/:id/review', () => {
        calls += 1;
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const client = makeQueryClient();
    const clients: MovieFeedbackClients = { libraryClient: client, detailsClient: client };
    let fetches = 0;
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      return fetches === 1 ? Promise.resolve([]) : refreshGate;
    });
    renderActions(client, { clients });
    await waitFor(() => expect(fetches).toBe(1));

    // act
    fireEvent.click(screen.getByRole('button', { name: LIKE_IDLE }));
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_SAVE_ERROR);

    // assert
    const retry = screen.getByRole('button', { name: RETRY_SAVE });
    const before = calls;
    fireEvent.click(retry);
    expect(calls).toBe(before);

    unblockRefresh();

    await waitFor(() =>
      expect(screen.getByRole('button', { name: LIKE_IDLE })).toBeInTheDocument(),
    );
    expect(calls).toBe(before);
  });

  it('renders a refresh-only error that keeps the confirmed review and retries without another PUT', async () => {
    // arrange
    let calls = 0;
    let fetches = 0;
    let refreshFails = true;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        calls += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    const clients: MovieFeedbackClients = { libraryClient: client, detailsClient: client };
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      if (fetches === 1 || !refreshFails) {
        return Promise.resolve([]);
      }
      return Promise.reject(new Error('refresh failed'));
    });
    renderActions(client, { clients });
    await waitFor(() => expect(fetches).toBe(1));

    // act
    fireEvent.click(screen.getByRole('button', { name: LIKE_IDLE }));

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_REFRESH_ERROR);
    expect(screen.getByRole('button', { name: LIKE_MARKED })).toBeVisible();
    expect(screen.queryByRole('button', { name: RETRY_SAVE })).not.toBeInTheDocument();

    refreshFails = false;
    fireEvent.click(screen.getByRole('button', { name: RETRY_REFRESH }));

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: LIKE_MARKED })).toBeVisible();
    expect(fetches).toBe(3);
    expect(calls).toBe(1);
  });
});

describe('MovieFeedbackActions navigation isolation', () => {
  it('keeps activation isolated from parent-card navigation', async () => {
    // arrange
    let puts = 0;
    const onOpenMovie = vi.fn();
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    render(
      <QueryClientProvider client={client}>
        <article>
          <a
            href="/movies/1"
            onClick={(event) => {
              event.preventDefault();
              onOpenMovie();
            }}
          >
            Heat
          </a>
          <MovieFeedbackActions
            movieId={1}
            title="Heat"
            isWatched={false}
            review="none"
            isInWatchlist={false}
          />
        </article>
      </QueryClientProvider>,
    );
    const like = screen.getByRole('button', { name: LIKE_IDLE });

    // act
    fireEvent.click(like);

    // assert
    expect(screen.getByRole('link', { name: 'Heat' })).not.toContainElement(like);
    await waitFor(() => expect(puts).toBe(1));
    expect(onOpenMovie).not.toHaveBeenCalled();
  });

  it('keeps the row above the stretched title overlay', () => {
    // arrange / act
    const zIndex = actionsCss.match(/z-index:\s*(\d+)/);

    // assert
    expect(zIndex).not.toBeNull();
    expect(Number(zIndex![1])).toBeGreaterThan(1);
  });

  it('renders the watchlist icon only once inside the shared row', () => {
    // arrange / act
    renderActions(makeQueryClient(), { isInWatchlist: true });

    // assert
    const watchlistButtons = screen.getAllByRole('button', { name: WATCHLIST_MARKED });
    expect(watchlistButtons).toHaveLength(1);
    const row = watchlistButtons[0].closest('.movie-feedback-actions') as HTMLElement;
    expect(row).not.toBeNull();
    expect(within(row).getAllByRole('button')).toHaveLength(4);
  });
});
