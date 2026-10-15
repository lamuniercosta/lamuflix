import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import App from '../App';
import { __resetWatchlistStore } from './watchlistStore';

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

function searchParams(): URLSearchParams {
  return new URLSearchParams(window.location.search);
}

beforeEach(() => {
  __resetWatchlistStore();
  window.history.replaceState({}, '', '/');
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('watchlist quick filter URL sync', () => {
  it('syncs the filter to the URL, resets the page, and retains other filters', () => {
    window.history.replaceState({}, '', '/?genreIds=5&page=3');
    renderApp();
    const toggle = screen.getByRole('button', { name: 'Watchlist' });
    expect(toggle).toHaveAttribute('aria-pressed', 'false');

    fireEvent.click(toggle);

    const params = searchParams();
    expect(params.get('watchlist')).toBe('true');
    expect(params.get('genreIds')).toBe('5');
    expect(params.get('page')).toBe('1');
    expect(toggle).toHaveAttribute('aria-pressed', 'true');
  });

  it('drops the watchlist key when the filter is turned off', () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    renderApp();
    const toggle = screen.getByRole('button', { name: 'Watchlist' });
    expect(toggle).toHaveAttribute('aria-pressed', 'true');

    fireEvent.click(toggle);

    expect(searchParams().has('watchlist')).toBe(false);
    expect(toggle).toHaveAttribute('aria-pressed', 'false');
  });
});

describe('watchlist quick filter label', () => {
  it('labels the toggle "Watchlist" and leaves it unpressed without the flag', () => {
    renderApp();
    const toggle = screen.getByRole('button', { name: 'Watchlist' });
    expect(toggle).toHaveAccessibleName('Watchlist');
    expect(toggle).toHaveAttribute('aria-pressed', 'false');
  });

  it('shows the pressed state at ?watchlist=true', () => {
    window.history.replaceState({}, '', '/?watchlist=true');
    renderApp();
    expect(screen.getByRole('button', { name: 'Watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });
});
