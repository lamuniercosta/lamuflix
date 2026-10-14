import { act, renderHook } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useDebouncedSearch } from './useDebouncedSearch';

describe('useDebouncedSearch', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not commit before 300 ms', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current as (value: string) => void;

    act(() => {
      search('abc');
      vi.advanceTimersByTime(299);
    });

    expect(onCommit).not.toHaveBeenCalled();
  });

  it('commits once at 300 ms', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current as (value: string) => void;

    act(() => {
      search('abc');
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('abc');
  });

  it('cancels stale commits on rapid input', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    const { result } = renderHook(() => useDebouncedSearch(onCommit));
    const search = result.current as (value: string) => void;

    act(() => {
      search('a');
      vi.advanceTimersByTime(200);
      search('ab');
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('ab');
  });
});
