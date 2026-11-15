import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FilterSearchField } from './FilterSearchField';

function renderField(text = '', onCommit = vi.fn()) {
  return render(<FilterSearchField inputId="search" text={text} onCommit={onCommit} />);
}

describe('FilterSearchField', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/');
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('debounces a typed value into a single commit', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    renderField('', onCommit);

    const input = screen.getByPlaceholderText('Search titles');
    fireEvent.change(input, { target: { value: 'd' } });
    fireEvent.change(input, { target: { value: 'du' } });
    fireEvent.change(input, { target: { value: 'dune' } });

    expect(onCommit).not.toHaveBeenCalled();
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).toHaveBeenCalledTimes(1);
    expect(onCommit).toHaveBeenCalledWith('dune');
  });

  it('re-seeds the draft from the committed text the shell passes', () => {
    const onCommit = vi.fn();
    const { rerender } = renderField('', onCommit);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'stale' } });
    rerender(<FilterSearchField inputId="search" text="restored" onCommit={onCommit} />);

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('restored');
  });

  it('drops a pending commit when remounted for a resync', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    const { rerender } = renderField('', onCommit);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'stale' } });
    act(() => {
      vi.advanceTimersByTime(100);
    });

    // The shell resets a draft by remounting the sibling with a new key.
    rerender(<FilterSearchField key="resync" inputId="search" text="" onCommit={onCommit} />);
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).not.toHaveBeenCalledWith('stale');
    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('');
  });

  it('shows the committed text from the shell on a fresh mount', () => {
    renderField('restored');

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('restored');
  });

  it('drops a pending commit when the field unmounts', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    const { unmount } = renderField('', onCommit);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'stale' } });
    unmount();
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(onCommit).not.toHaveBeenCalled();
  });

  it('commits an empty draft synchronously when cleared and refocuses the input', () => {
    vi.useFakeTimers();
    const onCommit = vi.fn();
    renderField('', onCommit);

    const input = screen.getByPlaceholderText('Search titles');
    fireEvent.change(input, { target: { value: 'dune' } });
    fireEvent.click(screen.getByRole('button', { name: 'Clear search' }));

    expect(onCommit).toHaveBeenCalledWith('');
    expect(input).toHaveValue('');
    expect(input).toHaveFocus();
    expect(screen.queryByRole('button', { name: 'Clear search' })).not.toBeInTheDocument();
  });
});
