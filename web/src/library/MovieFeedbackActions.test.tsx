import {
  QueryClient,
  QueryClientProvider,
  QueryObserver,
  type QueryKey,
} from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
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
import { MovieFeedbackActions } from './MovieFeedbackActions';

const WATCHED_IDLE = 'Mark Watched, Heat';
const WATCHED_MARKED = 'Watched, Heat';
const NOT_INTERESTED_IDLE = 'Not Interested, Heat';
const NOT_INTERESTED_MARKED = 'Not Interested (marked), Heat';
const RETRY_SAVE = 'Retry saving feedback, Heat';
const RETRY_REFRESH = 'Retry refresh, Heat';

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
  isNotInterested?: boolean;
  clients?: MovieFeedbackClients;
}

function renderActions(client: QueryClient, options: ActionsOptions = {}) {
  return render(
    <QueryClientProvider client={client}>
      <MovieFeedbackActions
        movieId={1}
        title="Heat"
        isWatched={options.isWatched ?? false}
        isNotInterested={options.isNotInterested ?? false}
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
});

afterEach(() => {
  while (observers.length > 0) {
    observers.pop()?.();
  }
  __resetMovieFeedbackStore();
});

describe('MovieFeedbackActions labels and accessible names', () => {
  it('renders the idle labels with the movie title in the accessible names', () => {
    // arrange / act
    renderActions(makeQueryClient());

    // assert
    const watched = screen.getByRole('button', { name: WATCHED_IDLE });
    const notInterested = screen.getByRole('button', { name: NOT_INTERESTED_IDLE });
    expect(watched).toHaveTextContent('Mark Watched');
    expect(notInterested).toHaveTextContent('Not Interested');
    expect(watched).toBeEnabled();
    expect(notInterested).toBeEnabled();
  });

  it('renders the marked Watched label and disables only that control', () => {
    // arrange / act
    renderActions(makeQueryClient(), { isWatched: true });

    // assert
    expect(screen.getByRole('button', { name: WATCHED_MARKED })).toHaveTextContent('Watched');
    expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeDisabled();
    expect(screen.getByRole('button', { name: NOT_INTERESTED_IDLE })).toBeEnabled();
  });

  it('renders the marked Not Interested label and disables only that control', () => {
    // arrange / act
    renderActions(makeQueryClient(), { isNotInterested: true });

    // assert
    expect(screen.getByRole('button', { name: NOT_INTERESTED_MARKED })).toHaveTextContent(
      'Not Interested (marked)',
    );
    expect(screen.getByRole('button', { name: NOT_INTERESTED_MARKED })).toBeDisabled();
    expect(screen.getByRole('button', { name: WATCHED_IDLE })).toBeEnabled();
  });

  it('does not dispatch when an already-marked control is activated', () => {
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
    renderActions(makeQueryClient(), { isWatched: true, isNotInterested: true });

    // act
    fireEvent.click(screen.getByRole('button', { name: WATCHED_MARKED }));
    fireEvent.click(screen.getByRole('button', { name: NOT_INTERESTED_MARKED }));

    // assert
    expect(posts).toBe(0);
  });
});

describe('MovieFeedbackActions keyboard and focus', () => {
  it('keeps native Enter and Space activation available on both controls', async () => {
    // arrange
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderActions(makeQueryClient());
    const watched = screen.getByRole('button', { name: WATCHED_IDLE });
    const notInterested = screen.getByRole('button', { name: NOT_INTERESTED_IDLE });

    // act
    watched.focus();

    // assert
    expect(watched.tagName).toBe('BUTTON');
    expect(watched).toHaveAttribute('type', 'button');
    expect(notInterested).toHaveAttribute('type', 'button');
    expect(watched).not.toHaveAttribute('tabindex', '-1');
    expect(document.activeElement).toBe(watched);
    // jsdom does not synthesize activation from keys; proving no key handler
    // swallows Enter/Space and that the resulting click path dispatches once.
    expect(fireEvent.keyDown(watched, { key: 'Enter' })).toBe(true);
    expect(fireEvent.keyDown(watched, { key: ' ' })).toBe(true);
    expect(
      watched.compareDocumentPosition(notInterested) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();

    fireEvent.click(watched);

    await waitFor(() => expect(posts).toBe(1));
  });

  it('moves focus onto the controls and leaves the shared focus treatment intact', () => {
    // arrange
    renderActions(makeQueryClient());
    const watched = screen.getByRole('button', { name: WATCHED_IDLE });
    const notInterested = screen.getByRole('button', { name: NOT_INTERESTED_IDLE });

    // act
    watched.focus();

    // assert
    expect(document.activeElement).toBe(watched);
    expect(watched.style.outline).toBe('');

    notInterested.focus();

    expect(document.activeElement).toBe(notInterested);
    expect(notInterested.style.outline).toBe('');
  });
});

describe('MovieFeedbackActions pending and error states', () => {
  it('announces the pending state and disables both actions for that movie', async () => {
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
    const pendingNotInterested = screen.getByRole('button', { name: NOT_INTERESTED_IDLE });
    expect(pendingWatched).toBeDisabled();
    expect(pendingWatched).toHaveAttribute('aria-busy', 'true');
    expect(pendingNotInterested).toBeDisabled();
    expect(pendingNotInterested).toHaveAttribute('aria-busy', 'true');
    expect(screen.getByRole('status')).toHaveTextContent('Saving feedback for Heat');
    // The optimistic mark drives the settled label; pending never swaps to a saving label.
    expect(pendingWatched).toHaveTextContent('Watched');

    gate.resolve();

    await waitFor(() => expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeVisible());
    expect(screen.getByRole('status')).toHaveTextContent('');
    expect(posts).toBe(1);
  });

  it('renders an accessible mutation error with an explicit retry that dispatches once', async () => {
    // arrange
    let posts = 0;
    const retryGate = deferred();
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        posts += 1;
        if (posts === 1) {
          return new HttpResponse(null, { status: 500 });
        }
        await retryGate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderActions(makeQueryClient());

    // act
    fireEvent.click(screen.getByRole('button', { name: WATCHED_IDLE }));

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_SAVE_ERROR);
    expect(screen.getByRole('button', { name: WATCHED_IDLE })).toBeEnabled();
    const retry = screen.getByRole('button', { name: RETRY_SAVE });
    const beforeRetry = posts;

    // One duplicate activation before the retry re-render dispatches at most one POST.
    act(() => {
      retry.click();
      retry.click();
    });

    await waitFor(() => expect(posts).toBe(beforeRetry + 1));

    retryGate.resolve();

    await waitFor(() => expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeVisible());
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(posts).toBe(beforeRetry + 1);
  });

  it('issues no POST from retry while reconciliation is still active', async () => {
    // arrange
    let posts = 0;
    let unblockRefresh!: () => void;
    const refreshGate = new Promise<unknown>((resolve) => {
      unblockRefresh = () => resolve([]);
    });
    let fetches = 0;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const client = makeQueryClient();
    const clients: MovieFeedbackClients = { libraryClient: client, detailsClient: client };
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      return fetches === 1 ? Promise.resolve([]) : refreshGate;
    });
    renderActions(client, { clients });
    await waitFor(() => expect(fetches).toBe(1));

    // act
    fireEvent.click(screen.getByRole('button', { name: WATCHED_IDLE }));
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_SAVE_ERROR);

    // assert
    const retry = screen.getByRole('button', { name: RETRY_SAVE });
    expect(retry).toBeEnabled();
    const beforeRetry = posts;
    fireEvent.click(retry);
    expect(posts).toBe(beforeRetry);

    unblockRefresh();

    await waitFor(() =>
      expect(screen.getByRole('button', { name: WATCHED_IDLE })).toBeInTheDocument(),
    );
    expect(posts).toBe(beforeRetry);
  });

  it('renders a refresh-only error that keeps the confirmed mark and retries without another POST', async () => {
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
    fireEvent.click(screen.getByRole('button', { name: WATCHED_IDLE }));

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent(MOVIE_FEEDBACK_REFRESH_ERROR);
    expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeVisible();
    expect(screen.queryByRole('button', { name: RETRY_SAVE })).not.toBeInTheDocument();

    refreshFails = false;
    fireEvent.click(screen.getByRole('button', { name: RETRY_REFRESH }));

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: WATCHED_MARKED })).toBeVisible();
    expect(fetches).toBe(3);
    expect(posts).toBe(1);
  });
});

describe('MovieFeedbackActions navigation isolation', () => {
  it('keeps activation isolated from parent-card navigation', async () => {
    // arrange
    let posts = 0;
    const onOpenMovie = vi.fn();
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
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
            isNotInterested={false}
          />
        </article>
      </QueryClientProvider>,
    );
    const watched = screen.getByRole('button', { name: WATCHED_IDLE });

    // act
    fireEvent.click(watched);

    // assert
    expect(screen.getByRole('link', { name: 'Heat' })).not.toContainElement(watched);
    await waitFor(() => expect(posts).toBe(1));
    expect(onOpenMovie).not.toHaveBeenCalled();
  });
});
