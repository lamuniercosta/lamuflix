import { useCallback, useSyncExternalStore } from 'react';
import type { QueryClient, QueryKey } from '@tanstack/react-query';
import { markMovieNotInterested, markMovieWatched, type FeedbackMovieId } from './movieFeedback';

export const MOVIE_FEEDBACK_SAVE_ERROR = "Couldn't save feedback. Try again.";
export const MOVIE_FEEDBACK_REFRESH_ERROR = 'Saved, but the view could not refresh. Try again.';

export type MovieFeedbackAction = 'watched' | 'notInterested';

export interface MovieFeedbackFlags {
  isWatched: boolean;
  isNotInterested: boolean;
}

export interface MovieFeedbackClients {
  libraryClient: QueryClient;
  detailsClient?: QueryClient;
}

export interface MovieFeedbackState extends MovieFeedbackFlags {
  pending: boolean;
  reconciling: boolean;
  blocked: boolean;
  error: string | null;
  refreshError: string | null;
  markWatched: () => void;
  markNotInterested: () => void;
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

function refreshTargets(
  movieId: FeedbackMovieId,
  clients: MovieFeedbackClients,
  broadRefresh: boolean,
): RefreshTarget[] {
  const targets: RefreshTarget[] = [{ client: clients.libraryClient, queryKey: ['browseMovies'] }];
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
    .findAll({ queryKey })
    .some((query) => query.state.status === 'error');
}

async function reconcile(
  key: string,
  movieId: FeedbackMovieId,
  clients: MovieFeedbackClients,
  broadRefresh: boolean,
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
  const refreshFailed = targets.some(({ client, queryKey }) => hasFailedQuery(client, queryKey));
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
  const optimistic: MovieFeedbackFlags =
    action === 'watched'
      ? { isWatched: true, isNotInterested: before.isNotInterested }
      : { isWatched: before.isWatched, isNotInterested: true };

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
    const outcome =
      action === 'watched'
        ? await markMovieWatched(movieId)
        : await markMovieNotInterested(movieId);

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
      await reconcile(key, movieId, clients, false);
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
    await reconcile(key, movieId, clients, true);
  })();
}

export function useMovieFeedback(
  movieId: FeedbackMovieId,
  initial: MovieFeedbackFlags,
  clients: MovieFeedbackClients,
): MovieFeedbackState {
  const key = String(movieId);
  const entry = useSyncExternalStore(subscribe, () => entries.get(key));

  const isWatched = entry ? entry.isWatched : initial.isWatched;
  const isNotInterested = entry ? entry.isNotInterested : initial.isNotInterested;
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

      const before: MovieFeedbackFlags = current ?? {
        isWatched: initial.isWatched,
        isNotInterested: initial.isNotInterested,
      };
      if (action === 'watched' ? before.isWatched : before.isNotInterested) {
        return;
      }

      dispatch(
        key,
        movieId,
        action,
        { isWatched: before.isWatched, isNotInterested: before.isNotInterested },
        clients,
      );
    },
    [key, movieId, clients, initial.isWatched, initial.isNotInterested],
  );

  const markWatched = useCallback(() => start('watched'), [start]);
  const markNotInterested = useCallback(() => start('notInterested'), [start]);

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
    void reconcile(key, movieId, clients, current.broadRefresh);
  }, [key, movieId, clients]);

  return {
    isWatched,
    isNotInterested,
    pending,
    reconciling,
    blocked,
    error,
    refreshError,
    markWatched,
    markNotInterested,
    retry,
    retryRefresh,
  };
}
