import { StrictMode } from 'react';
import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  currentReturnUrl,
  navigate,
  notifyRouteChange,
  openMovie,
  openMoviePlayer,
  readPlayerIntent,
  replace,
  subscribeToRouteChange,
} from './appNavigation';
import { useAppNavigation } from './useAppNavigation';

beforeEach(() => {
  window.history.replaceState({}, '', '/');
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

function popstateSpy() {
  return vi.spyOn(window, 'dispatchEvent');
}

function popstateCalls(spy: ReturnType<typeof popstateSpy>): number {
  return spy.mock.calls.filter(([event]) => event instanceof PopStateEvent).length;
}

describe('appNavigation openMovie', () => {
  it('pushes the details route and notifies subscribers without a synthetic popstate', () => {
    window.history.replaceState({}, '', '/?text=dune');
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);
    const dispatch = popstateSpy();

    openMovie(7);

    expect(window.location.pathname).toBe('/movies/7');
    expect(window.history.state).toEqual({ libraryReturnUrl: '/?text=dune' });
    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener).toHaveBeenCalledWith({
      pathname: '/movies/7',
      search: '',
      routeKey: expect.any(Number) as number,
    });
    expect(popstateCalls(dispatch)).toBe(0);

    dispatch.mockRestore();
    unsubscribe();
  });

  it('adds player intent only when the caller requests it', () => {
    window.history.replaceState({}, '', '/?statuses=Pending');
    openMovie(3, { playerIntent: true });
    expect(window.history.state).toEqual({
      libraryReturnUrl: '/?statuses=Pending',
      playerIntent: true,
    });

    window.history.replaceState({}, '', '/?statuses=Pending');
    openMovie(3);
    expect(window.history.state).toEqual({ libraryReturnUrl: '/?statuses=Pending' });
    expect(readPlayerIntent(window.history.state)).toBe(false);
  });

  it('carries an explicit validated return URL without inventing player intent', () => {
    window.history.replaceState({ libraryReturnUrl: '/?genreIds=1' }, '', '/movies/1');

    openMovie(2, { returnUrl: '/?genreIds=1' });

    expect(window.location.pathname).toBe('/movies/2');
    expect(window.history.state).toEqual({ libraryReturnUrl: '/?genreIds=1' });
    expect(readPlayerIntent(window.history.state)).toBe(false);
  });

  it('drops an untrusted explicit return URL', () => {
    window.history.replaceState({}, '', '/movies/1');

    openMovie(2, { returnUrl: 'https://example.com/?text=x' });

    expect(window.location.pathname).toBe('/movies/2');
    expect(window.history.state).toEqual({});
  });
});

describe('appNavigation openMoviePlayer', () => {
  it('retains the library return URL and always marks player intent', () => {
    window.history.replaceState({}, '', '/?statuses=Pending');
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);
    const dispatch = popstateSpy();

    openMoviePlayer(3);

    expect(window.location.pathname).toBe('/movies/3');
    expect(window.history.state).toEqual({
      libraryReturnUrl: '/?statuses=Pending',
      playerIntent: true,
    });
    expect(listener).toHaveBeenCalledTimes(1);
    expect(popstateCalls(dispatch)).toBe(0);

    dispatch.mockRestore();
    unsubscribe();
  });

  it('falls back to the existing history return URL from a details route', () => {
    window.history.replaceState({ libraryReturnUrl: '/?text=a' }, '', '/movies/1');

    openMoviePlayer(9);

    expect(window.history.state).toEqual({ libraryReturnUrl: '/?text=a', playerIntent: true });
  });

  it('marks player intent without a return URL when none is valid', () => {
    window.history.replaceState({}, '', '/movies/1');

    openMoviePlayer(9);

    expect(window.history.state).toEqual({ playerIntent: true });
    expect(currentReturnUrl()).toBeNull();
  });
});

describe('appNavigation navigate and replace', () => {
  it('pushes a new entry and notifies exactly once, keeping history', () => {
    window.history.replaceState({}, '', '/');
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);
    const before = window.history.length;

    navigate('/?text=once');

    expect(window.history.length).toBe(before + 1);
    expect(window.location.search).toBe('?text=once');
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it('replaces the entry instead of pushing when asked', () => {
    window.history.replaceState({}, '', '/?text=dune');
    const before = window.history.length;
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);

    replace('/?text=arrival');

    expect(window.history.length).toBe(before);
    expect(window.location.search).toBe('?text=arrival');
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it('replaces rather than pushes when the target is already current', () => {
    window.history.replaceState({}, '', '/?text=dune');
    const before = window.history.length;

    navigate('/?text=dune', { state: { libraryReturnUrl: '/?text=dune' } });

    expect(window.history.length).toBe(before);
    expect(currentReturnUrl()).toBe('/?text=dune');
  });

  it('does not notify when history refuses the write', () => {
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);
    const pushState = vi.spyOn(window.history, 'pushState').mockImplementation(() => {
      throw new Error('history unavailable');
    });

    expect(navigate('/movies/5')).toBe(false);
    expect(listener).not.toHaveBeenCalled();

    pushState.mockRestore();
    unsubscribe();
  });

  it('notifies every live subscriber once per change', () => {
    const first = vi.fn();
    const second = vi.fn();
    const unsubscribeFirst = subscribeToRouteChange(first);
    const unsubscribeSecond = subscribeToRouteChange(second);

    notifyRouteChange();

    expect(first).toHaveBeenCalledTimes(1);
    expect(second).toHaveBeenCalledTimes(1);

    unsubscribeFirst();
    unsubscribeSecond();
  });

  it('stops notifying a subscriber after it unsubscribes', () => {
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);

    unsubscribe();
    navigate('/movies/4');

    expect(listener).not.toHaveBeenCalled();
  });

  it('assigns a strictly increasing route key per notification', () => {
    const keys: number[] = [];
    const unsubscribe = subscribeToRouteChange((snapshot) => keys.push(snapshot.routeKey));

    navigate('/movies/1');
    navigate('/movies/2');

    expect(keys).toHaveLength(2);
    expect(keys[1]).toBeGreaterThan(keys[0]);

    unsubscribe();
  });
});

describe('useAppNavigation over the shared mechanism', () => {
  it('resyncs the mounted route from a programmatic navigation without popstate', () => {
    window.history.replaceState({}, '', '/');
    const { result } = renderHook(() => useAppNavigation());
    const dispatch = popstateSpy();

    act(() => {
      openMovie(3, { playerIntent: true });
    });

    expect(result.current.route.pathname).toBe('/movies/3');
    expect(popstateCalls(dispatch)).toBe(0);
    dispatch.mockRestore();
  });

  it('applies exactly one route update per programmatic navigation', () => {
    window.history.replaceState({}, '', '/');
    const { result } = renderHook(() => useAppNavigation());
    const before = result.current.route.routeKey;

    act(() => {
      result.current.navigate('/?text=once');
    });

    expect(result.current.route.routeKey).toBe(before + 1);
  });

  it('keeps native popstate for browser Back and Forward', () => {
    window.history.replaceState({}, '', '/');
    const { result } = renderHook(() => useAppNavigation());

    act(() => {
      window.history.replaceState({}, '', '/?text=back');
      window.dispatchEvent(new PopStateEvent('popstate'));
    });

    expect(result.current.route.search).toBe('?text=back');
  });

  it('unsubscribes on unmount', () => {
    window.history.replaceState({}, '', '/');
    const { result, unmount } = renderHook(() => useAppNavigation());
    const before = result.current.route.routeKey;

    unmount();
    act(() => {
      navigate('/movies/8');
    });

    expect(result.current.route.routeKey).toBe(before);
  });

  it('subscribes once across a StrictMode setup-cleanup-setup cycle', () => {
    window.history.replaceState({}, '', '/');
    const { result } = renderHook(() => useAppNavigation(), { wrapper: StrictMode });
    const before = result.current.route.routeKey;

    act(() => {
      navigate('/movies/6');
    });

    expect(result.current.route.routeKey).toBe(before + 1);
  });
});

describe('appNavigation test isolation', () => {
  it('leaves no subscribers behind for the next test', () => {
    const listener = vi.fn();
    const unsubscribe = subscribeToRouteChange(listener);
    unsubscribe();

    navigate('/movies/1');

    expect(listener).not.toHaveBeenCalled();
  });
});
