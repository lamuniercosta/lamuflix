import { useCallback, useEffect, useState } from 'react';

const WATCHLIST_KEY = 'watchlist';
const PAGE_KEY = 'page';

export function readWatchlistFlag(search: string): boolean {
  return new URLSearchParams(search).get(WATCHLIST_KEY) === 'true';
}

export interface WatchlistFilterState {
  active: boolean;
  toggle: () => void;
}

export function useWatchlistFilter(): WatchlistFilterState {
  const [active, setActive] = useState(() => readWatchlistFlag(window.location.search));

  useEffect(() => {
    const onPopState = () => setActive(readWatchlistFlag(window.location.search));
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  const toggle = useCallback(() => {
    const next = !readWatchlistFlag(window.location.search);
    const params = new URLSearchParams(window.location.search);
    if (next) {
      params.set(WATCHLIST_KEY, 'true');
    } else {
      params.delete(WATCHLIST_KEY);
    }
    if (params.has(PAGE_KEY)) {
      params.set(PAGE_KEY, '1');
    }
    const query = params.toString();
    window.history.pushState({}, '', `${window.location.pathname}${query ? `?${query}` : ''}`);
    setActive(next);
  }, []);

  return { active, toggle };
}

export default useWatchlistFilter;
