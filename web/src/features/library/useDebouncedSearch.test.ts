import { act, renderHook } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useDebouncedSearch } from './useDebouncedSearch';

describe('useDebouncedSearch', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not commit before 300 ms', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn<(value: string) => void>();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current;

    act(() => {
      search.schedule('abc');
      vi.advanceTimersByTime(299);
    });

    expect(onCommit).not.toHaveBeenCalled();
  });

  it('commits once at 300 ms', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn<(value: string) => void>();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current;

    act(() => {
      search.schedule('abc');
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('abc');
  });

  it('cancels stale commits on rapid input', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn<(value: string) => void>();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current;

    act(() => {
      search.schedule('a');
      vi.advanceTimersByTime(200);
      search.schedule('ab');
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('ab');
  });

  it('cancel() stops a pending commit and the hook still schedules afterwards', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn<(value: string) => void>();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current;

    act(() => {
      search.schedule('abc');
      vi.advanceTimersByTime(100);
      search.cancel();
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).not.toHaveBeenCalled();

    act(() => {
      search.schedule('def');
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('def');
  });

  it('cancel() with nothing pending is a no-op', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn<(value: string) => void>();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current;

    expect(() => search.cancel()).not.toThrow();

    act(() => {
      search.schedule('abc');
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('abc');
  });
});
