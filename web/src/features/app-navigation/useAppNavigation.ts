import { useCallback, useEffect, useState } from 'react';
import {
  navigate as navigateTo,
  readRouteSnapshot,
  replace as replaceWith,
  subscribeToRouteChange,
  type AppRouteSnapshot,
  type NavigateOptions,
} from './appNavigation';

export {
  currentReturnUrl,
  isLibraryRoute,
  libraryReturnSearch,
  openMovie,
  openMoviePlayer,
  readPlayerIntent,
  readReturnUrl,
  subscribeToRouteChange,
  validateLibraryReturnUrl,
} from './appNavigation';
export type { AppRouteSnapshot, NavigateOptions, OpenMovieOptions } from './appNavigation';

export function parseMovieId(pathname: string): number | null {
  const match = /^\/movies\/(\d+)$/.exec(pathname);
  if (!match) {
    return null;
  }
  const id = Number(match[1]);
  if (!Number.isInteger(id) || id <= 0) {
    return null;
  }
  return id;
}

export function useAppNavigation() {
  const [route, setRoute] = useState<AppRouteSnapshot>(() => readRouteSnapshot());

  useEffect(() => {
    const unsubscribe = subscribeToRouteChange(setRoute);
    return unsubscribe;
  }, []);

  const navigate = useCallback(
    (to: string, options?: NavigateOptions) => navigateTo(to, options),
    [],
  );

  const replace = useCallback((to: string, state?: unknown) => replaceWith(to, state), []);

  return { route, navigate, replace };
}

export default useAppNavigation;
