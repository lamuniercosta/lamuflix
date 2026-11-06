import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { ReactElement } from 'react';
import { describe, expect, it } from 'vitest';
import { server } from '../test/server';
import MovieCard from './MovieCard';

const base = {
  id: 7,
  title: 'Heat',
  releaseYear: 1995,
  runtime: 170,
  imdbRating: 8.3,
  enrichmentStatus: 'Enriched',
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
  review: 'none',
} as const;

function renderWithClient(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('MovieCard thumbnail fallback', () => {
  it('shows the poster when present', () => {
    // arrange / act
    renderWithClient(<MovieCard movie={{ ...base, posterUrl: '/posters/heat.jpg' }} />);

    // assert
    expect(screen.getByAltText('Heat')).toHaveAttribute('src', '/posters/heat.jpg');
  });

  it('shows the cached thumbnail when there is no poster', () => {
    // arrange / act
    const { container } = renderWithClient(<MovieCard movie={{ ...base, posterUrl: null }} />);

    // assert
    expect(screen.getByAltText('Heat')).toHaveAttribute('src', '/api/movies/7/thumbnail');
    expect(container.querySelector('.movie-card-poster')).toHaveStyle({ aspectRatio: '2 / 3' });
  });

  it('shows the placeholder without requesting a thumbnail before enrichment', () => {
    // arrange / act
    const { container } = renderWithClient(
      <MovieCard movie={{ ...base, posterUrl: null, enrichmentStatus: 'Pending' }} />,
    );

    // assert
    expect(container.querySelector('img')).toBeNull();
    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
  });

  it('shows the placeholder when the thumbnail is missing', () => {
    // arrange
    server.use(
      http.get('*/api/movies/:id/thumbnail', () => new HttpResponse(null, { status: 404 })),
    );

    // act
    renderWithClient(<MovieCard movie={{ ...base, posterUrl: null }} />);
    fireEvent.error(screen.getByAltText('Heat'));

    // assert
    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.queryByAltText('Heat')).not.toBeInTheDocument();
  });
});
