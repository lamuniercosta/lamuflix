import { QueryClient, QueryObserver, type QueryKey } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { HttpResponse, http } from 'msw';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../test/server';
import {
  MOVIE_FEEDBACK_REFRESH_ERROR,
  MOVIE_FEEDBACK_SAVE_ERROR,
  normalizeReview,
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

function flags(isWatched = false, review: MovieFeedbackFlags['review'] = 'none') {
  return { isWatched, review } satisfies MovieFeedbackFlags;
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
        {`${feedback.isWatched ? 'watched' : 'unwatched'}/${feedback.review}`}
      </span>
      <span data-testid={`not-interested-${tag}`}>{feedback.isNotInterested ? 'yes' : 'no'}</span>
      <span data-testid={`pending-${tag}`}>{feedback.pending ? 'pending' : 'settled'}</span>
      <span data-testid={`reconciling-${tag}`}>
        {feedback.reconciling ? 'reconciling' : 'done'}
      </span>
      <span data-testid={`blocked-${tag}`}>{feedback.blocked ? 'blocked' : 'open'}</span>
      <span data-testid={`error-${tag}`}>{feedback.error ?? ''}</span>
      <span data-testid={`refresh-error-${tag}`}>{feedback.refreshError ?? ''}</span>
      <button type="button" onClick={feedback.markWatched}>{`watched-${tag}`}</button>
      <button type="button" onClick={feedback.unmarkWatched}>{`unmark-watched-${tag}`}</button>
      <button type="button" onClick={feedback.like}>{`like-${tag}`}</button>
      <button type="button" onClick={feedback.dislike}>{`dislike-${tag}`}</button>
      <button type="button" onClick={feedback.clearReview}>{`clear-${tag}`}</button>
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

function reviewBody(request: Request): Promise<{ review: string }> {
  return request.json() as Promise<{ review: string }>;
}

function renderProbes(client: QueryClient, probes: ProbeProps[]) {
  return render(
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

describe('movieFeedbackStore review normalization', () => {
  it.each([
    ['none', 'none'],
    ['liked', 'liked'],
    ['disliked', 'disliked'],
    ['', 'none'],
    ['LIKED', 'none'],
    ['unknown', 'none'],
  ] as const)('normalizes %s to %s', (input, expected) => {
    // arrange / act
    const normalized = normalizeReview(input);

    // assert
    expect(normalized).toBe(expected);
  });

  it('normalizes a null or undefined review to none', () => {
    // arrange / act / assert
    expect(normalizeReview(null)).toBe('none');
    expect(normalizeReview(undefined)).toBe('none');
  });
});

describe('movieFeedbackStore optimistic flags', () => {
  it('updates only the watched flag before the response and preserves the review', async () => {
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
      { movieId: 1, initial: flags(false, 'liked'), clients: clientsFor(client) },
    ]);

    // act
    click('watched-1-a');

    // assert
    expect(read('1-a', 'flags')).toBe('watched/liked');
    expect(read('1-a', 'pending')).toBe('pending');
    expect(read('1-a', 'blocked')).toBe('blocked');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('watched/liked');
  });

  it('derives the legacy not-interested projection from a Disliked review only', async () => {
    // arrange
    server.use(http.put('*/api/movies/:id/review', () => new HttpResponse(null, { status: 204 })));
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // assert initial
    expect(read('1-a', 'not-interested')).toBe('no');

    // act
    click('dislike-1-a');

    // assert
    expect(read('1-a', 'not-interested')).toBe('yes');
    expect(read('1-a', 'flags')).toBe('unwatched/disliked');

    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'not-interested')).toBe('yes');
  });

  it('does not dispatch when the movie already holds that review', () => {
    // arrange
    let puts = 0;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(true, 'liked'), clients: clientsFor(client) },
    ]);

    // act
    click('like-1-a');

    // assert
    expect(puts).toBe(0);
    expect(read('1-a', 'blocked')).toBe('open');
    expect(read('1-a', 'flags')).toBe('watched/liked');
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
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);
    click('watched-1-a');

    // act
    rerender(
      <div>
        <Probe movieId={1} initial={flags(false, 'none')} clients={clientsFor(client)} />
      </div>,
    );

    // assert
    expect(read('1-a', 'flags')).toBe('watched/none');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore review transitions', () => {
  it('switches directly from Liked to Disliked with one PUT each', async () => {
    // arrange
    const bodies: string[] = [];
    server.use(
      http.put('*/api/movies/:id/review', async ({ request }) => {
        bodies.push((await reviewBody(request)).review);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // act
    click('like-1-a');
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    click('dislike-1-a');

    // assert
    expect(read('1-a', 'flags')).toBe('unwatched/disliked');
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(bodies).toEqual(['liked', 'disliked']);
  });

  it('clears an active review to none without touching the watched flag', async () => {
    // arrange
    const bodies: string[] = [];
    server.use(
      http.put('*/api/movies/:id/review', async ({ request }) => {
        bodies.push((await reviewBody(request)).review);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(true, 'disliked'), clients: clientsFor(client) },
    ]);

    // act
    click('clear-1-a');

    // assert
    expect(read('1-a', 'flags')).toBe('watched/none');
    expect(read('1-a', 'not-interested')).toBe('no');
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(bodies).toEqual(['none']);
  });
});

describe('movieFeedbackStore unmark actions', () => {
  it('flips the watched flag optimistically and keeps the review while the DELETE is in flight', async () => {
    // arrange
    const gate = deferred();
    let deletes = 0;
    server.use(
      http.delete('*/api/movies/:id/watched', async () => {
        deletes += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(true, 'liked'), clients: clientsFor(client) },
    ]);

    // act
    click('unmark-watched-1-a');

    // assert
    expect(read('1-a', 'flags')).toBe('unwatched/liked');
    expect(read('1-a', 'pending')).toBe('pending');
    expect(read('1-a', 'blocked')).toBe('blocked');
    await waitFor(() => expect(deletes).toBe(1));

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('unwatched/liked');
    expect(read('1-a', 'error')).toBe('');
  });

  it('rolls back a failed review PUT and shows the save error', async () => {
    // arrange
    let puts = 0;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // act
    click('like-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('unwatched/none');
    expect(read('1-a', 'refresh-error')).toBe('');
    expect(puts).toBe(1);
  });

  it('retries a failed review PUT once and clears the error on success', async () => {
    // arrange
    let puts = 0;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        if (puts === 1) {
          return new HttpResponse(null, { status: 500 });
        }
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'disliked'), clients: clientsFor(client) },
    ]);
    click('clear-1-a');
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('unwatched/disliked');

    // act
    click('retry-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'flags')).toBe('unwatched/none'));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'error')).toBe('');
    expect(puts).toBe(2);
  });
});

describe('movieFeedbackStore per-movie exclusion', () => {
  it('blocks every action for one movie across instances while another movie proceeds', async () => {
    // arrange
    const gate = deferred();
    let firstPosts = 0;
    let secondPuts = 0;
    server.use(
      http.post('*/api/movies/1/watched', async () => {
        firstPosts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
      http.put('*/api/movies/2/review', () => {
        secondPuts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, 'none'), clients: clientsFor(client) },
      { movieId: 1, instance: 'b', initial: flags(false, 'none'), clients: clientsFor(client) },
      { movieId: 2, instance: 'a', initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // act
    click('watched-1-a');
    await waitFor(() => expect(firstPosts).toBe(1));
    click('like-1-b');
    click('like-2-a');

    // assert
    await waitFor(() => expect(secondPuts).toBe(1));
    expect(firstPosts).toBe(1);
    expect(read('1-a', 'blocked')).toBe('blocked');
    expect(read('1-b', 'blocked')).toBe('blocked');
    expect(read('2-a', 'flags')).toBe('unwatched/liked');
    expect(read('2-a', 'blocked')).toBe('open');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });

  it('dispatches at most one PUT under duplicate activation', async () => {
    // arrange
    const gate = deferred();
    let puts = 0;
    server.use(
      http.put('*/api/movies/:id/review', async () => {
        puts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, instance: 'a', initial: flags(false, 'none'), clients: clientsFor(client) },
      { movieId: 1, instance: 'b', initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // act
    click('like-1-a');
    await waitFor(() => expect(puts).toBe(1));
    click('like-1-a');
    click('like-1-b');

    // assert
    expect(puts).toBe(1);

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore unmount survival', () => {
  it('still blocks a remounted instance and reconciles once', async () => {
    // arrange
    const gate = deferred();
    let puts = 0;
    let refreshes = 0;
    server.use(
      http.put('*/api/movies/:id/review', async () => {
        puts += 1;
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
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(refreshes).toBe(1));

    // act
    click('like-1-a');
    await waitFor(() => expect(puts).toBe(1));
    first.unmount();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // assert
    expect(read('1-a', 'blocked')).toBe('blocked');

    gate.resolve();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('unwatched/liked');
    expect(puts).toBe(1);
    expect(refreshes).toBe(2);
  });
});

describe('movieFeedbackStore failure handling', () => {
  it('does not retry the PUT automatically', async () => {
    // arrange
    let puts = 0;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const client = makeQueryClient();
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // act
    click('like-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(puts).toBe(1);
  });

  it('issues no PUT from retry while reconciliation is deferred', async () => {
    // arrange
    let puts = 0;
    let unblockRefresh!: () => void;
    const refreshGate = new Promise<unknown>((resolve) => {
      unblockRefresh = () => resolve([]);
    });
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
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
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches).toBe(1));

    // act
    click('like-1-a');
    await waitFor(() => expect(read('1-a', 'reconciling')).toBe('reconciling'));
    click('retry-1-a');

    // assert
    expect(puts).toBe(1);

    unblockRefresh();
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
  });
});

describe('movieFeedbackStore authoritative refresh', () => {
  it('refreshes library, For You, details and recommendation queries and stales inactive ones', async () => {
    // arrange
    const fetches = { browse: 0, forYou: 0, details: 0, related: 0, inactive: 0 };
    const queryFns = {
      browse: () => {
        fetches.browse += 1;
        return Promise.resolve({ items: [] });
      },
      forYou: () => {
        fetches.forYou += 1;
        return Promise.resolve([]);
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
    server.use(http.put('*/api/movies/:id/review', () => new HttpResponse(null, { status: 204 })));
    const client = makeQueryClient();
    watch(client, BROWSE_KEY, queryFns.browse);
    watch(client, ['forYou', 10], queryFns.forYou);
    watch(client, ['movieDetails', 1], queryFns.details);
    watch(client, ['relatedMovies', 1], queryFns.related);
    await client.prefetchQuery({ queryKey: ['relatedMovies', 9], queryFn: queryFns.inactive });
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches.related).toBe(1));

    // act
    click('like-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(fetches).toEqual({ browse: 2, forYou: 2, details: 2, related: 2, inactive: 1 });
    expect(client.getQueryState(['relatedMovies', 9])?.isInvalidated).toBe(true);
  });

  it('keeps the confirmed review when refresh fails and retries refresh without another PUT', async () => {
    // arrange
    let puts = 0;
    let fetches = 0;
    let refreshFails = true;
    server.use(
      http.put('*/api/movies/:id/review', () => {
        puts += 1;
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
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches).toBe(1));

    // act
    click('like-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'refresh-error')).toBe(MOVIE_FEEDBACK_REFRESH_ERROR));
    expect(read('1-a', 'error')).toBe('');
    expect(read('1-a', 'flags')).toBe('unwatched/liked');
    expect(read('1-a', 'blocked')).toBe('open');

    refreshFails = false;
    click('refresh-1-a');

    await waitFor(() => expect(read('1-a', 'refresh-error')).toBe(''));
    expect(fetches).toBe(3);
    expect(puts).toBe(1);
  });

  it('ignores an inactive failed query when the active refresh succeeds', async () => {
    // arrange
    server.use(http.put('*/api/movies/:id/review', () => new HttpResponse(null, { status: 204 })));
    const client = makeQueryClient();
    watch(client, BROWSE_KEY, () => Promise.resolve({ items: [] }));
    watch(client, ['movieDetails', 2], () => Promise.resolve({ id: 2 }));
    watch(client, ['relatedMovies', 2], () => Promise.resolve([]));
    await client.prefetchQuery({
      queryKey: ['relatedMovies', 1],
      queryFn: () => Promise.reject(new Error('stale failure')),
    });
    expect(client.getQueryState(['relatedMovies', 1])?.status).toBe('error');
    renderProbes(client, [
      { movieId: 2, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);

    // act
    click('dislike-2-a');

    // assert
    await waitFor(() => expect(read('2-a', 'blocked')).toBe('open'));
    expect(read('2-a', 'flags')).toBe('unwatched/disliked');
    expect(read('2-a', 'refresh-error')).toBe('');
  });

  it('never claims Saved when a failed PUT meets a failed reconciliation', async () => {
    // arrange
    let fetches = 0;
    server.use(http.put('*/api/movies/:id/review', () => new HttpResponse(null, { status: 500 })));
    const client = makeQueryClient();
    watch(client, ['relatedMovies', 1], () => {
      fetches += 1;
      if (fetches === 1) return Promise.resolve([]);
      return Promise.reject(new Error('refresh failed'));
    });
    renderProbes(client, [
      { movieId: 1, initial: flags(false, 'none'), clients: clientsFor(client) },
    ]);
    await waitFor(() => expect(fetches).toBe(1));

    // act
    click('like-1-a');

    // assert
    await waitFor(() => expect(read('1-a', 'error')).toBe(MOVIE_FEEDBACK_SAVE_ERROR));
    await waitFor(() => expect(read('1-a', 'blocked')).toBe('open'));
    expect(read('1-a', 'flags')).toBe('unwatched/none');
    expect(read('1-a', 'refresh-error')).toBe('');
    expect(fetches).toBe(2);
  });
});
