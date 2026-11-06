import { useCallback, useSyncExternalStore } from 'react';
import type { QueryClient, QueryKey } from '@tanstack/react-query';
import {
  markMovieWatched,
  setMovieReview,
  unmarkMovieWatched,
  REVIEW_STATES,
  type FeedbackMovieId,
  type MovieFeedbackOutcome,
  type ReviewState,
} from './movieFeedback';

export const MOVIE_FEEDBACK_SAVE_ERROR = "Couldn't save feedback. Try again.";
export const MOVIE_FEEDBACK_REFRESH_ERROR = 'Saved, but the view could not refresh. Try again.';

export type MovieFeedbackAction = 'watched' | 'unwatched' | 'like' | 'dislike' | 'clearReview';

export interface MovieFeedbackFlags {
  isWatched: boolean;
  review: ReviewState;
}

export interface MovieFeedbackClients {
  libraryClient: QueryClient;
  detailsClient?: QueryClient;
}

export interface MovieFeedbackState extends MovieFeedbackFlags {
  isNotInterested: boolean;
  pending: boolean;
  reconciling: boolean;
  blocked: boolean;
  error: string | null;
  refreshError: string | null;
  markWatched: () => void;
  unmarkWatched: () => void;
  like: () => void;
  dislike: () => void;
  clearReview: () => void;
  retry: () => void;
  retryRefresh: () => void;
}

interface Entry extends MovieFeedbackFlags {
  pending: MovieFeedbackAction | null;
  reconciling: boolean;
  error: string | null;
  refreshError: string | null;
  lastAction: MovieFeedbackAction | null;
  before: MovieFeedbackFlags;
  broadRefresh: boolean;
}

interface RefreshTarget {
  client: QueryClient;
  queryKey: QueryKey;
}

const entries = new Map<string, Entry>();
const subscribers = new Set<() => void>();

const networkCalls: Record<
  MovieFeedbackAction,
  (movieId: FeedbackMovieId) => Promise<MovieFeedbackOutcome>
> = {
  watched: markMovieWatched,
  unwatched: unmarkMovieWatched,
  like: (movieId) => setMovieReview(movieId, 'liked'),
  dislike: (movieId) => setMovieReview(movieId, 'disliked'),
  clearReview: (movieId) => setMovieReview(movieId, 'none'),
};

export function normalizeReview(review: string | null | undefined): ReviewState {
  return REVIEW_STATES.find((state) => state === review) ?? 'none';
}

function subscribe(listener: () => void): () => void {
  subscribers.add(listener);
  return () => {
    subscribers.delete(listener);
  };
}

function notify(): void {
  for (const listener of subscribers) {
    listener();
  }
}

function write(key: string, entry: Entry): void {
  entries.set(key, entry);
  notify();
}

export function __resetMovieFeedbackStore(): void {
  entries.clear();
  notify();
}

function isBusy(entry: Entry): boolean {
  return entry.pending !== null || entry.reconciling;
}

function optimisticFlags(
  action: MovieFeedbackAction,
  before: MovieFeedbackFlags,
): MovieFeedbackFlags {
  switch (action) {
    case 'watched':
      return { isWatched: true, review: before.review };
    case 'unwatched':
      return { isWatched: false, review: before.review };
    case 'like':
      return { isWatched: before.isWatched, review: 'liked' };
    case 'dislike':
      return { isWatched: before.isWatched, review: 'disliked' };
    case 'clearReview':
      return { isWatched: before.isWatched, review: 'none' };
  }
}

function isNoOp(action: MovieFeedbackAction, before: MovieFeedbackFlags): boolean {
  const next = optimisticFlags(action, before);
  return next.isWatched === before.isWatched && next.review === before.review;
}

function refreshTargets(
  movieId: FeedbackMovieId,
  clients: MovieFeedbackClients,
  broadRefresh: boolean,
): RefreshTarget[] {
  const targets: RefreshTarget[] = [
    { client: clients.libraryClient, queryKey: ['browseMovies'] },
    { client: clients.libraryClient, queryKey: ['forYou'] },
  ];
  if (clients.detailsClient) {
    targets.push(
      { client: clients.detailsClient, queryKey: ['movieDetails', movieId] },
      {
        client: clients.detailsClient,
        queryKey: broadRefresh ? ['relatedMovies'] : ['relatedMovies', movieId],
      },
    );
  }
  return targets;
}

function hasFailedQuery(client: QueryClient, queryKey: QueryKey): boolean {
  return client
    .getQueryCache()
    .findAll({ queryKey, type: 'active' })
    .some((query) => query.state.status === 'error');
}

async function reconcile(
  key: string,
  movieId: FeedbackMovieId,
  clients: MovieFeedbackClients,
  broadRefresh: boolean,
  succeeded: boolean,
): Promise<void> {
  const targets = refreshTargets(movieId, clients, broadRefresh);
  await Promise.all(
    targets.map(({ client, queryKey }) =>
      client.invalidateQueries({ queryKey }).catch(() => undefined),
    ),
  );

  const current = entries.get(key);
  if (!current) {
    return;
  }
  const refreshFailed =
    succeeded && targets.some(({ client, queryKey }) => hasFailedQuery(client, queryKey));
  write(key, {
    ...current,
    pending: null,
    reconciling: false,
    refreshError: refreshFailed ? MOVIE_FEEDBACK_REFRESH_ERROR : null,
  });
}

function dispatch(
  key: string,
  movieId: FeedbackMovieId,
  action: MovieFeedbackAction,
  before: MovieFeedbackFlags,
  clients: MovieFeedbackClients,
): void {
  const optimistic = optimisticFlags(action, before);

  write(key, {
    ...optimistic,
    pending: action,
    reconciling: false,
    error: null,
    refreshError: null,
    lastAction: action,
    before,
    broadRefresh: true,
  });

  void (async () => {
    const outcome = await networkCalls[action](movieId);

    const current = entries.get(key);
    if (!current) {
      return;
    }

    if (!outcome.ok) {
      write(key, {
        ...current,
        ...before,
        pending: null,
        reconciling: true,
        error: MOVIE_FEEDBACK_SAVE_ERROR,
        broadRefresh: false,
      });
      await reconcile(key, movieId, clients, false, false);
      return;
    }

    write(key, {
      ...current,
      ...optimistic,
      pending: null,
      reconciling: true,
      error: null,
      broadRefresh: true,
    });
    await reconcile(key, movieId, clients, true, true);
  })();
}

export function useMovieFeedback(
  movieId: FeedbackMovieId,
  initial: MovieFeedbackFlags,
  clients: MovieFeedbackClients,
): MovieFeedbackState {
  const key = String(movieId);
  const entry = useSyncExternalStore(subscribe, () => entries.get(key));
  const baseline: MovieFeedbackFlags = {
    isWatched: initial.isWatched,
    review: normalizeReview(initial.review),
  };

  const isWatched = entry ? entry.isWatched : baseline.isWatched;
  const review = entry ? entry.review : baseline.review;
  const pending = entry ? entry.pending !== null : false;
  const reconciling = entry ? entry.reconciling : false;
  const error = entry ? entry.error : null;
  const refreshError = entry ? entry.refreshError : null;
  const blocked = pending || reconciling;

  const start = useCallback(
    (action: MovieFeedbackAction) => {
      const current = entries.get(key);
      if (current && isBusy(current)) {
        return;
      }

      const before: MovieFeedbackFlags = current
        ? { isWatched: current.isWatched, review: current.review }
        : { isWatched: initial.isWatched, review: normalizeReview(initial.review) };
      if (isNoOp(action, before)) {
        return;
      }

      dispatch(key, movieId, action, before, clients);
    },
    [key, movieId, clients, initial.isWatched, initial.review],
  );

  const markWatched = useCallback(() => start('watched'), [start]);
  const unmarkWatched = useCallback(() => start('unwatched'), [start]);
  const like = useCallback(() => start('like'), [start]);
  const dislike = useCallback(() => start('dislike'), [start]);
  const clearReview = useCallback(() => start('clearReview'), [start]);

  const retry = useCallback(() => {
    const current = entries.get(key);
    if (!current || current.lastAction === null || current.error === null) {
      return;
    }
    if (isBusy(current)) {
      return;
    }
    dispatch(key, movieId, current.lastAction, current.before, clients);
  }, [key, movieId, clients]);

  const retryRefresh = useCallback(() => {
    const current = entries.get(key);
    if (!current || current.refreshError === null) {
      return;
    }
    if (isBusy(current)) {
      return;
    }
    write(key, { ...current, reconciling: true, refreshError: null });
    void reconcile(key, movieId, clients, current.broadRefresh, true);
  }, [key, movieId, clients]);

  return {
    isWatched,
    review,
    isNotInterested: review === 'disliked',
    pending,
    reconciling,
    blocked,
    error,
    refreshError,
    markWatched,
    unmarkWatched,
    like,
    dislike,
    clearReview,
    retry,
    retryRefresh,
  };
}
