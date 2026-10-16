import { useCallback, useEffect, useState } from 'react';

export interface AppRouteSnapshot {
  pathname: string;
  search: string;
  routeKey: number;
}

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

export function isLibraryRoute(pathname: string): boolean {
  return pathname === '/';
}

export function validateLibraryReturnUrl(value: unknown): string | null {
  if (typeof value !== 'string' || value === '') {
    return null;
  }
  let url: URL;
  try {
    url = new URL(value, window.location.origin);
  } catch {
    return null;
  }
  if (url.origin !== window.location.origin) {
    return null;
  }
  if (!isLibraryRoute(url.pathname)) {
    return null;
  }
  return `${url.pathname}${url.search}`;
}

export function readReturnUrl(state: unknown): string | null {
  if (state == null || typeof state !== 'object') {
    return null;
  }
  const candidate = (state as Record<string, unknown>).libraryReturnUrl;
  return validateLibraryReturnUrl(candidate);
}

export function currentReturnUrl(): string | null {
  return readReturnUrl(window.history.state);
}

export function libraryReturnSearch(returnUrl: string | null): string {
  if (!returnUrl) {
    return '';
  }
  try {
    return new URL(returnUrl, window.location.origin).search;
  } catch {
    return '';
  }
}

function snapshot(key: number): AppRouteSnapshot {
  return {
    pathname: window.location.pathname || '/',
    search: window.location.search || '',
    routeKey: key,
  };
}

export interface NavigateOptions {
  replace?: boolean;
  state?: unknown;
}

export function useAppNavigation() {
  const [route, setRoute] = useState<AppRouteSnapshot>(() => snapshot(0));

  useEffect(() => {
    const onPopState = () => {
      setRoute((prev) => snapshot(prev.routeKey + 1));
    };
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  const navigate = useCallback((to: string, options?: NavigateOptions) => {
    const url = new URL(to, window.location.origin);
    const target = `${url.pathname}${url.search}`;
    const state = options?.state;
    const sameUrl = target === `${window.location.pathname || '/'}${window.location.search || ''}`;
    try {
      if (options?.replace || sameUrl) {
        window.history.replaceState(state ?? null, '', target);
      } else {
        window.history.pushState(state ?? null, '', target);
      }
    } catch {
      return;
    }
    setRoute((prev) => {
      const next: AppRouteSnapshot = {
        pathname: url.pathname,
        search: url.search,
        routeKey: prev.routeKey + 1,
      };
      if (next.pathname === prev.pathname && next.search === prev.search) {
        return prev;
      }
      return next;
    });
  }, []);

  const replace = useCallback(
    (to: string, state?: unknown) => navigate(to, { replace: true, state }),
    [navigate],
  );

  return { route, navigate, replace };
}

export default useAppNavigation;
