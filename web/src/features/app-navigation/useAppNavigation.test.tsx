import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import {
  currentReturnUrl,
  isLibraryRoute,
  libraryReturnSearch,
  parseMovieId,
  readReturnUrl,
  useAppNavigation,
  validateLibraryReturnUrl,
} from './useAppNavigation';

describe('parseMovieId', () => {
  it.each([
    ['/movies/1', 1],
    ['/movies/42', 42],
  ])('treats %s as a details route', (path, id) => {
    expect(parseMovieId(path)).toBe(id);
  });

  it.each([['/'], ['/movies/0'], ['/movies/-3'], ['/movies/abc'], ['/movies/1.5'], ['/other']])(
    'falls back to the library for %s',
    (path) => {
      expect(parseMovieId(path)).toBeNull();
    },
  );
});

describe('isLibraryRoute', () => {
  it('accepts only the root browse route', () => {
    expect(isLibraryRoute('/')).toBe(true);
    expect(isLibraryRoute('/movies/1')).toBe(false);
  });
});

describe('validateLibraryReturnUrl', () => {
  it('accepts an exact same-origin library path and query', () => {
    window.history.replaceState({}, '', '/');
    expect(validateLibraryReturnUrl('/?text=dune&page=2')).toBe('/?text=dune&page=2');
  });

  it.each([[null], [undefined], [''], ['not a url %%'], ['https://example.com/?text=x']])(
    'rejects %s',
    (value) => {
      window.history.replaceState({}, '', '/');
      expect(validateLibraryReturnUrl(value)).toBeNull();
    },
  );

  it('rejects a same-origin non-library path', () => {
    window.history.replaceState({}, '', '/');
    expect(validateLibraryReturnUrl('/movies/1?text=x')).toBeNull();
  });
});

describe('readReturnUrl', () => {
  it('reads the library return URL from history state', () => {
    expect(readReturnUrl({ libraryReturnUrl: '/?text=dune' })).toBe('/?text=dune');
  });

  it.each([[null], [undefined], [{}], [{ libraryReturnUrl: '/movies/1' }]])(
    'returns null for %s',
    (state) => {
      expect(readReturnUrl(state)).toBeNull();
    },
  );
});

describe('libraryReturnSearch', () => {
  it('returns the query portion without the leading path', () => {
    expect(libraryReturnSearch('/?statuses=Pending&text=dune')).toBe('?statuses=Pending&text=dune');
  });

  it('returns empty for a missing return URL', () => {
    expect(libraryReturnSearch(null)).toBe('');
  });
});

describe('currentReturnUrl', () => {
  it('falls back to null without valid navigation state', () => {
    window.history.replaceState(null, '/movies/1');
    expect(currentReturnUrl()).toBeNull();
  });
});

describe('useAppNavigation', () => {
  it('publishes app-initiated navigation and subscribes to popstate', () => {
    window.history.replaceState({}, '', '/');
    const { result } = renderHook(() => useAppNavigation());

    expect(result.current.route.pathname).toBe('/');

    act(() => {
      result.current.navigate('/movies/3', { state: { libraryReturnUrl: '/?text=x' } });
    });
    expect(result.current.route.pathname).toBe('/movies/3');
    expect(currentReturnUrl()).toBe('/?text=x');

    act(() => {
      window.history.replaceState({}, '', '/?text=y');
      window.dispatchEvent(new PopStateEvent('popstate'));
    });
    expect(result.current.route.pathname).toBe('/');
    expect(result.current.route.search).toBe('?text=y');
  });

  it('replaces the entry when asked instead of pushing', () => {
    window.history.replaceState({}, '', '/');
    const { result } = renderHook(() => useAppNavigation());
    const before = window.history.length;

    act(() => {
      result.current.replace('/?text=dune');
    });

    expect(result.current.route.search).toBe('?text=dune');
    expect(window.history.length).toBe(before);
  });

  it('replaces rather than pushes when navigating to the current URL', () => {
    window.history.replaceState({}, '', '/?text=dune');
    const { result } = renderHook(() => useAppNavigation());
    const before = window.history.length;

    act(() => {
      result.current.navigate('/?text=dune', { state: { libraryReturnUrl: '/?text=dune' } });
    });

    expect(window.history.length).toBe(before);
    expect(currentReturnUrl()).toBe('/?text=dune');
  });

  it('resyncs on popstate even when only history state changed', () => {
    window.history.replaceState({ libraryReturnUrl: '/?text=a' }, '', '/movies/1');
    const { result } = renderHook(() => useAppNavigation());
    const before = result.current.route.routeKey;

    act(() => {
      window.history.replaceState({ libraryReturnUrl: '/?text=b' }, '', '/movies/1');
      window.dispatchEvent(new PopStateEvent('popstate'));
    });

    expect(result.current.route.routeKey).toBeGreaterThan(before);
    expect(currentReturnUrl()).toBe('/?text=b');
  });
});
