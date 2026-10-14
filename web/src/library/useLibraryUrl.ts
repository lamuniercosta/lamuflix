import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  parseLibraryUrl,
  serializeLibraryUrl,
  toApiParams,
  LIBRARY_DEFAULTS,
  type LibraryUrlState,
} from './libraryUrl';

export type LibraryUrlActions = {
  state: LibraryUrlState;
  urlState: LibraryUrlState;
  browseState: LibraryUrlState;
  normalizedState: LibraryUrlState;
  apiParams: ReturnType<typeof toApiParams>;
  generatedParams: ReturnType<typeof toApiParams>;
  applyFilters: (patch: Partial<LibraryUrlState>) => void;
  apply: (patch: Partial<LibraryUrlState>) => void;
  setFilters: (patch: Partial<LibraryUrlState>) => void;
  updateFilters: (patch: Partial<LibraryUrlState>) => void;
  clearFilters: () => void;
  clear: () => void;
  resetFilters: () => void;
  reset: () => void;
  setPage: (page: number) => void;
  goToPage: (page: number) => void;
  push: (next: LibraryUrlState) => void;
  replace: (next: LibraryUrlState) => void;
};

export function useLibraryUrl(): LibraryUrlActions {
  const [state, setState] = useState<LibraryUrlState>(() =>
    parseLibraryUrl(window.location.search),
  );

  useEffect(() => {
    const current = window.location.search.replace(/^\?/, '');
    const canonical = serializeLibraryUrl(state);
    if (current !== canonical) {
      window.history.replaceState({}, '', canonical ? `?${canonical}` : '/');
    }

    const onPopState = () => {
      const next = parseLibraryUrl(window.location.search);
      const raw = window.location.search.replace(/^\?/, '');
      const canonicalNext = serializeLibraryUrl(next);
      if (raw !== canonicalNext) {
        window.history.replaceState({}, '', canonicalNext ? `?${canonicalNext}` : '/');
      }
      setState(next);
    };
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const push = useCallback((next: LibraryUrlState) => {
    window.history.pushState({}, '', `?${serializeLibraryUrl(next)}`);
    setState(next);
  }, []);

  const replace = useCallback((next: LibraryUrlState) => {
    window.history.replaceState({}, '', `?${serializeLibraryUrl(next)}`);
    setState(next);
  }, []);

  const applyFilters = useCallback((patch: Partial<LibraryUrlState>) => {
    setState((prev) => {
      const next: LibraryUrlState = { ...prev, ...patch, page: 1 };
      window.history.pushState({}, '', `?${serializeLibraryUrl(next)}`);
      return next;
    });
  }, []);

  const clearFilters = useCallback(() => {
    window.history.pushState({}, '', `?${serializeLibraryUrl(LIBRARY_DEFAULTS)}`);
    setState({ ...LIBRARY_DEFAULTS });
  }, []);

  const setPage = useCallback((page: number) => {
    setState((prev) => {
      const next: LibraryUrlState = { ...prev, page };
      window.history.pushState({}, '', `?${serializeLibraryUrl(next)}`);
      return next;
    });
  }, []);

  const actions = useMemo<LibraryUrlActions>(
    () => ({
      state,
      urlState: state,
      browseState: state,
      normalizedState: state,
      apiParams: toApiParams(state),
      generatedParams: toApiParams(state),
      applyFilters,
      apply: applyFilters,
      setFilters: applyFilters,
      updateFilters: applyFilters,
      clearFilters,
      clear: clearFilters,
      resetFilters: clearFilters,
      reset: clearFilters,
      setPage,
      goToPage: setPage,
      push,
      replace,
    }),
    [state, applyFilters, clearFilters, setPage, push, replace],
  );

  return actions;
}

export default useLibraryUrl;
