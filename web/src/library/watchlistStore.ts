import { useCallback, useSyncExternalStore } from 'react';
import { client } from '../api/client';

export const WATCHLIST_TOGGLE_ERROR = "Couldn't update watchlist. Try again.";

interface WatchlistEntry {
  target: boolean;
  pending: boolean;
  error: string | null;
}

const entries = new Map<string, WatchlistEntry>();
const subscribers = new Set<() => void>();

function subscribe(listener: () => void): () => void {
  subscribers.add(listener);
  return () => {
    subscribers.delete(listener);
  };
}

function write(key: string, entry: WatchlistEntry): void {
  entries.set(key, entry);
  for (const listener of subscribers) {
    listener();
  }
}

export function __resetWatchlistStore(): void {
  entries.clear();
  for (const listener of subscribers) {
    listener();
  }
}

export interface WatchlistToggleState {
  isInWatchlist: boolean;
  pending: boolean;
  error: string | null;
  toggle: () => void;
}

export function useWatchlistToggle(
  movieId: number | string,
  baseline: boolean,
): WatchlistToggleState {
  const key = String(movieId);
  const entry = useSyncExternalStore(subscribe, () => entries.get(key));

  const isInWatchlist = entry ? entry.target : baseline;
  const pending = entry?.pending ?? false;
  const error = entry?.error ?? null;

  const toggle = useCallback(() => {
    const current = entries.get(key);
    if (current?.pending) {
      return;
    }

    const previousTarget = current ? current.target : baseline;
    const target = !previousTarget;
    write(key, { target, pending: true, error: null });

    const request = target
      ? client.POST('/api/movies/{id}/watchlist', { params: { path: { id: movieId } } })
      : client.DELETE('/api/movies/{id}/watchlist', { params: { path: { id: movieId } } });

    void request
      .then(({ response }) => {
        if (response.status === 204 || response.status === 409) {
          write(key, { target, pending: false, error: null });
        } else {
          write(key, { target: previousTarget, pending: false, error: WATCHLIST_TOGGLE_ERROR });
        }
      })
      .catch(() => {
        write(key, { target: previousTarget, pending: false, error: WATCHLIST_TOGGLE_ERROR });
      });
  }, [key, baseline, movieId]);

  return { isInWatchlist, pending, error, toggle };
}
