import { QueryClient, QueryObserver, type QueryKey } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { HttpResponse, http } from 'msw';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../test/server';
import {
  MOVIE_FEEDBACK_REFRESH_ERROR,
  MOVIE_FEEDBACK_SAVE_ERROR,
  useMovieFeedback,
  __resetMovieFeedbackStore,
  type MovieFeedbackClients,
  type MovieFeedbackFlags,
} from './movieFeedbackStore';

const BROWSE_KEY: QueryKey = ['browseMovies', { page: 1 }];

function deferred<T = void>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

function flags(isWatched = false, isNotInterested = false): MovieFeedbackFlags {
  return { isWatched, isNotInterested };
}

function clientsFor(queryClient: QueryClient): MovieFeedbackClients {
  return { libraryClient: queryClient, detailsClient: queryClient };
}

interface ProbeProps {
  movieId: number;
  instance?: string;
  initial: MovieFeedbackFlags;
  clients: MovieFeedbackClients;
}

function Probe({ movieId, instance = 'a', initial, clients }: ProbeProps) {
  const feedback = useMovieFeedback(movieId, initial, clients);
  const tag = `${movieId}-${instance}`;
  return (
    <div>
      <span data-testid={`flags-${tag}`}>
        {`${feedback.isWatched ? 'watched' : 'unwatched'}/${feedback.isNotInterested ? 'not-interested' : 'neutral'}`}
      </span>
      <span data-testid={`pending-${tag}`}>{feedback.pending ? 'pending' : 'settled'}</span>
      <span data-testid={`reconciling-${tag}`}>
        {feedback.reconciling ? 'reconciling' : 'done'}
      </span>
      <span data-testid={`blocked-${tag}`}>{feedback.blocked ? 'blocked' : 'open'}</span>
      <span data-testid={`error-${tag}`}>{feedback.error ?? ''}</span>
      <span data-testid={`refresh-error-${tag}`}>{feedback.refreshError ?? ''}</span>
      <button type="button" onClick={feedback.markWatched}>{`watched-${tag}`}</button>
      <button type="button" onClick={feedback.markNotInterested}>{`not-interested-${tag}`}</button>
      <button type="button" onClick={feedback.retry}>{`retry-${tag}`}</button>
      <button type="button" onClick={feedback.retryRefresh}>{`refresh-${tag}`}</button>
    </div>
  );
}

function read(tag: string, field: string): string {
  return screen.getByTestId(`${field}-${tag}`).textContent ?? '';
}

function click(label: string): void {
  fireEvent.click(screen.getByText(label));
}

const observers: Array<() => void> = [];

function watch(
  queryClient: QueryClient,
  queryKey: QueryKey,
  queryFn: () => Promise<unknown>,
): () => void {
  const observer = new QueryObserver(queryClient, { queryKey, queryFn, retry: false });
  const unsubscribe = observer.subscribe(() => undefined);
  observers.push(unsubscribe);
  return unsubscribe;
}

function makeQueryClient(): QueryClient {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

function renderProbes(client: QueryClient, probes: ProbeProps[]) {
  const result = render(
    <div>
      {probes.map((probe) => (
        <Probe
          key={`${probe.movieId}-${probe.instance ?? 'a'}`}
          {...probe}
          clients={clientsFor(client)}
        />
      ))}
    </div>,
  );
  return result;
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

describe('movieFeedbackStore optimistic flags', () => {
  it('updates only the selected flag before the response and preserves the other flag', async () => {
    // arrange
    const gate = deferred();
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, true), clients: clientsFor(client) },
    ]);

    // act
    click('watched-1-a');

    // assert
    expect(read('1-a', 'flags')).toBe('watched/not-interested');
    expect(read('1-a', 'pending')).toBe('pending');
    expect(read('1-a', 'blocked')).toBe('blocked');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('watched/not-interested');
  });

  it('does not dispatch when the movie is already marked', () => {
    // arrange
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(true, false), clients: clientsFor(client) },
    ]);

    // act
    click('watched-1-a');

    // assert
    expect(posts).toBe(0);
    expect(read('1-a', 'blocked')).toBe('open');
    expect(read('1-a', 'flags')).toBe('watched/neutral');
  });

  it('cannot be erased by a stale in-flight response', async () => {
    // arrange
    const gate = deferred();
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    const { rerender } = renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);
    click('watched-1-a');

    // act
    rerender(
      <div>
        <Probe movieId={1} initial={flags(false, false)} clients={clientsFor(client)} />
      </div>,
    );

    // assert
    expect(read('1-a', 'flags')).toBe('watched/neutral');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore per-movie exclusion', () => {
  it('blocks both actions for one movie across instances while another movie proceeds', async () => {
    // arrange
    const gate = deferred();
    let firstPosts = 0;
    let secondPosts = 0;
    server.use(
      http.post('*/api/movies/1/watched', async () => {
        firstPosts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
      http.post('*/api/movies/2/not-interested', () => {
        secondPosts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 1, instance: 'b', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 2, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
    ]);

    // act
    click('watched-1-a');
    await waitFor(() => expect(firstPosts).toBe(1));
    click('not-interested-1-b');
    click('not-interested-2-a');

    // assert
    await waitFor(() => expect(secondPosts).toBe(1));
    expect(firstPosts).toBe(1);
    expect(read('1-a', 'blocked')).toBe('blocked');
    expect(read('1-b', 'blocked')).toBe('blocked');
    expect(read('1-b', 'blocked')).toBe('blocked');
    expect(read('2-a', 'flags')).toBe('unwatched/not-interested');
    expect(read('2-a', 'blocked')).toBe('open');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });

  it('keeps both actions blocked after a settled POST while reconciliation is deferred', async () => {
    // arrange
    let fetches = 0;
    let unblockRefresh!: () => void;
    const refreshGate = new Promise<unknown>((resolve) => {
      unblockRefresh = () => resolve([]);
    });
    server.use(http.post('*/api/movies/1/watched', () => new HttpResponse(null, { status: 204 })));
    const client = makeQueryClient();
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      return fetches === 1 ? Promise.resolve([]) : refreshGate;
    });
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 1, instance: 'b', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 2, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches).toBe(1));

    // act
    click('watched-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'pending')).toBe('settled'));
    expect(read('1-a', 'reconciling')).toBe('reconciling');
    expect(read('1-a', 'blocked')).toBe('blocked');
    expect(read('1-b', 'blocked')).toBe('blocked');
    expect(read('2-a', 'blocked')).toBe('open');

    unblockRefresh();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(fetches).toBe(2);
  });

  it('dispatches at most one POST under duplicate activation', async () => {
    // arrange
    const gate = deferred();
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/not-interested', async () => {
        posts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 1, instance: 'b', initial: flags(false, false), clients: clientsFor(client) },
    ]);

    // act
    click('not-interested-1-a');
    await waitFor(() => expect(posts).toBe(1));
    click('not-interested-1-a');
    click('not-interested-1-b');

    // assert
    expect(posts).toBe(1);

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore unmount survival', () => {
  it('still blocks a remounted instance and reconciles once', async () => {
    // arrange
    const gate = deferred();
    let posts = 0;
    let refreshes = 0;
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        posts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    watch(client, ['relatedMovies', 1], () => {
      refreshes += 1;
      return Promise.resolve([]);
    });
    const first = renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(refreshes).toBe(1));

    // act
    click('watched-1-a');
    await waitFor(() => expect(posts).toBe(1));
    first.unmount();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);

    // assert
    expect(read('1-a', 'blocked')).toBe('blocked');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('watched/neutral');
    expect(posts).toBe(1);
    expect(refreshes).toBe(2);
  });

  it('exposes the mutation error and retry on the remounted instance', async () => {
    // arrange
    const gate = deferred();
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        posts += 1;
        if (posts === 1) {
          await gate.promise;
          return new HttpResponse(null, { status: 500 });
        }
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    const first = renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);
    click('watched-1-a');
    await waitFor(() => expect(posts).toBe(1));
    first.unmount();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);

    // act
    gate.resolve();

    // assert
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    expect(read('1-a', 'flags')).toBe('unwatched/neutral');

    click('retry-1-a');
    await waitFor(() => expect(posts).toBe(2));
    await waitFor(() => expect(read('1-a', 'flags')).toBe('watched/neutral'));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore failure handling', () => {
  it('rolls back only this operation and reconciles this movie while another movie proceeds', async () => {
    // arrange
    const gate = deferred();
    let firstRefreshes = 0;
    let secondRefreshes = 0;
    server.use(
      http.post('*/api/movies/1/watched', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 500 });
      }),
      http.post('*/api/movies/2/not-interested', () => new HttpResponse(null, { status: 204 })),
    );
    const client = makeQueryClient();
    watch(client, ['relatedMovies', 1], () => {
      firstRefreshes += 1;
      return Promise.resolve([]);
    });
    watch(client, ['relatedMovies', 2], () => {
      secondRefreshes += 1;
      return Promise.resolve([]);
    });
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 2, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(firstRefreshes).toBe(1));
    await waitFor(() => expect(secondRefreshes).toBe(1));

    // act
    click('not-interested-2-a');
    await waitFor(() => expect(read('2-a', 'blocked')).toBe('open'));
    const beforeFailureRefresh = firstRefreshes;
    click('watched-1-a');
    gate.resolve();

    // assert
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('unwatched/neutral');
    expect(read('2-a', 'flags')).toBe('unwatched/not-interested');
    expect(firstRefreshes).toBe(beforeFailureRefresh + 1);
    expect(secondRefreshes).toBe(2);
  });

  it('does not retry the POST automatically', async () => {
    // arrange
    let posts = 0;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);

    // act
    click('watched-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(posts).toBe(1);
  });

  it('retries the failed POST once under the same per-movie exclusion', async () => {
    // arrange
    let posts = 0;
    const retryGate = deferred();
    server.use(
      http.post('*/api/movies/:id/watched', async () => {
        posts += 1;
        if (posts === 1) return new HttpResponse(null, { status: 500 });
        await retryGate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, false), clients: clientsFor(client) },
      { movieId: 1, instance: 'b', initial: flags(false, false), clients: clientsFor(client) },
    ]);
    click('watched-1-a');
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));

    // act
    click('retry-1-a');
    await waitFor(() => expect(posts).toBe(2));
    click('retry-1-a');
    click('retry-1-b');

    // assert
    expect(posts).toBe(2);
    expect(read('1-a', 'pending')).toBe('pending');

    retryGate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('watched/neutral');
  });

  it('issues no POST from retry while reconciliation is deferred', async () => {
    // arrange
    let posts = 0;
    let unblockRefresh!: () => void;
    const refreshGate = new Promise<unknown>((resolve) => {
      unblockRefresh = () => resolve([]);
    });
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    let fetches = 0;
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      return fetches === 1 ? Promise.resolve([]) : refreshGate;
    });
    renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches).toBe(1));

    // act
    click('watched-1-a');
    await waitFor(() => expect(read('1-a', 'reconciling')).toBe('reconciling'));
    click('retry-1-a');

    // assert
    expect(posts).toBe(1);

    unblockRefresh();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore authoritative refresh', () => {
  it('refreshes library, details, and recommendation queries and stales inactive ones', async () => {
    // arrange
    const fetches = { browse: 0, details: 0, related: 0, inactive: 0 };
    const queryFns = {
      browse: () => {
        fetches.browse += 1;
        return Promise.resolve({ items: [] });
      },
      details: () => {
        fetches.details += 1;
        return Promise.resolve({ id: 1 });
      },
      related: () => {
        fetches.related += 1;
        return Promise.resolve([]);
      },
      inactive: () => {
        fetches.inactive += 1;
        return Promise.resolve([]);
      },
    };
    server.use(
      http.post('*/api/movies/:id/watched', () => new HttpResponse(null, { status: 204 })),
    );
    const client = makeQueryClient();
    watch(client, BROWSE_KEY, queryFns.browse);
    watch(client, ['movieDetails', 1], queryFns.details);
    watch(client, ['relatedMovies', 1], queryFns.related);
    await client.prefetchQuery({ queryKey: ['relatedMovies', 9], queryFn: queryFns.inactive });
    renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches.related).toBe(1));

    // act
    click('watched-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(fetches).toEqual({ browse: 2, details: 2, related: 2, inactive: 1 });
    expect(client.getQueryState(['relatedMovies', 9])?.isInvalidated).toBe(true);

    await client.fetchQuery({ queryKey: ['relatedMovies', 9], queryFn: queryFns.inactive });
    expect(fetches.inactive).toBe(2);
  });

  it('keeps the confirmed mark when refresh fails and retries refresh without another POST', async () => {
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
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      if (fetches === 1 || !refreshFails) return Promise.resolve([]);
      return Promise.reject(new Error('refresh failed'));
    });
    renderProbes(client, [
      { movieId: 1, initial: flags(false, false), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches).toBe(1));

    // act
    click('watched-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'refresh-error')).toBe(MOVIE_FEEDBACK_REFRESH_ERROR));
    expect(read('1-a', 'error')).toBe('');
    expect(read('1-a', 'flags')).toBe('watched/neutral');
    expect(read('1-a', 'blocked')).toBe('open');

    refreshFails = false;
    click('refresh-1-a');

    await waitFor(() => expect(read('1-a', 'refresh-error')).toBe(''));
    expect(fetches).toBe(3);
    expect(posts).toBe(1);
  });
});
