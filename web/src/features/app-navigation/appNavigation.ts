export interface AppRouteSnapshot {
  pathname: string;
  search: string;
  routeKey: number;
}

export type RouteChangeListener = (snapshot: AppRouteSnapshot) => void;

export interface NavigateOptions {
  replace?: boolean;
  state?: unknown;
}

export interface OpenMovieOptions {
  playerIntent?: boolean;
  returnUrl?: string | null;
}

const listeners = new Set<RouteChangeListener>();
let lastRouteKey = 0;

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

const PLAYER_INTENT_KEY = 'playerIntent';

export function readPlayerIntent(state: unknown): boolean {
  if (state == null || typeof state !== 'object') {
    return false;
  }
  return (state as Record<string, unknown>)[PLAYER_INTENT_KEY] === true;
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

function currentLocation(): string {
  return `${window.location.pathname || '/'}${window.location.search || ''}`;
}

function onNativeTraversal(): void {
  notifyRouteChange();
}

export function readRouteSnapshot(): AppRouteSnapshot {
  return {
    pathname: window.location.pathname || '/',
    search: window.location.search || '',
    routeKey: lastRouteKey,
  };
}

export function nextRouteSnapshot(): AppRouteSnapshot {
  const snapshot = readRouteSnapshot();
  lastRouteKey = snapshot.routeKey + 1;
  return { ...snapshot, routeKey: lastRouteKey };
}

function notifySubscribers(snapshot: AppRouteSnapshot): void {
  for (const listener of [...listeners]) {
    listener(snapshot);
  }
}

export function subscribeToRouteChange(listener: RouteChangeListener): () => void {
  if (listeners.size === 0) {
    window.addEventListener('popstate', onNativeTraversal);
  }
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
    if (listeners.size === 0) {
      window.removeEventListener('popstate', onNativeTraversal);
    }
  };
}

export function notifyRouteChange(): AppRouteSnapshot {
  const snapshot = nextRouteSnapshot();
  notifySubscribers(snapshot);
  return snapshot;
}

function writeHistory(target: string, state: unknown, replace: boolean): boolean {
  try {
    if (replace) {
      window.history.replaceState(state ?? null, '', target);
      return true;
    }
    window.history.pushState(state ?? null, '', target);
    return true;
  } catch {
    return false;
  }
}

export function navigate(to: string, options?: NavigateOptions): boolean {
  const url = new URL(to, window.location.origin);
  const target = `${url.pathname}${url.search}`;
  const sameUrl = target === currentLocation();
  const written = writeHistory(target, options?.state, Boolean(options?.replace) || sameUrl);
  if (written && !sameUrl) {
    notifyRouteChange();
  }
  return written;
}

export function replace(to: string, state?: unknown): boolean {
  return navigate(to, { replace: true, state });
}

export function openMovie(movieId: number | string, options?: OpenMovieOptions): void {
  const returnUrl =
    options?.returnUrl === undefined
      ? currentLocation()
      : validateLibraryReturnUrl(options.returnUrl);
  const state: { libraryReturnUrl?: string; playerIntent?: true } = {};
  if (returnUrl) {
    state.libraryReturnUrl = returnUrl;
  }
  if (options?.playerIntent) {
    state.playerIntent = true;
  }
  navigate(`/movies/${String(movieId)}`, { state });
}

export function openMoviePlayer(movieId: number | string): void {
  const returnUrl =
    validateLibraryReturnUrl(currentLocation()) ?? readReturnUrl(window.history.state);
  const state = returnUrl
    ? { libraryReturnUrl: returnUrl, playerIntent: true as const }
    : { playerIntent: true as const };
  navigate(`/movies/${String(movieId)}`, { state });
}
