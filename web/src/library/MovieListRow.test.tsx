import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import MovieListRow from './MovieListRow';
import { __resetWatchlistStore } from './watchlistStore';

const movie = {
  id: 1,
  title: 'Heat',
  posterUrl: '/posters/heat.jpg',
  releaseYear: 1995,
  runtime: 170,
  imdbRating: 8.3,
  enrichmentStatus: 'Enriched',
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
  review: 'none',
} as const;

function renderRow(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

beforeEach(() => {
  __resetWatchlistStore();
});

afterEach(() => {
  __resetWatchlistStore();
});

describe('MovieListRow watchlist label', () => {
  it('names the movie title in the watchlist toggle', () => {
    // arrange / act
    renderRow(<MovieListRow movie={movie} />);

    // assert
    expect(screen.getByRole('button', { name: 'Add to Watchlist, Heat' })).toBeInTheDocument();
  });

  it('names the movie title when the title is already watchlisted', () => {
    // arrange / act
    renderRow(<MovieListRow movie={{ ...movie, isInWatchlist: true }} />);

    // assert
    expect(screen.getByRole('button', { name: 'Remove from Watchlist, Heat' })).toBeInTheDocument();
  });
});
