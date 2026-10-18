import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it } from 'vitest';
import MovieCard from './MovieCard';
import MovieGrid from './MovieGrid';

const base = {
  id: 1,
  title: 'Heat',
  posterUrl: '/posters/heat.jpg',
  releaseYear: 1995,
  runtime: 170,
  imdbRating: 8.3,
  isInWatchlist: false,
  isWatched: false,
  isNotInterested: false,
} as const;

function renderWithClient(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('MovieCard layout semantics', () => {
  it('keeps the poster at a 2:3 ratio', () => {
    const { container } = renderWithClient(
      <MovieCard movie={{ ...base, enrichmentStatus: 'Enriched' }} />,
    );
    const poster = container.querySelector('.movie-card-poster');
    expect(poster).toHaveStyle({ aspectRatio: '2 / 3' });
  });

  it.each([
    ['NotFound', 'Not found'],
    ['Failed', 'Failed'],
    ['Pending', 'Pending'],
  ] as const)('announces enrichment state %s as text', (status, text) => {
    renderWithClient(<MovieCard movie={{ ...base, enrichmentStatus: status }} />);
    expect(screen.getByText(text)).toBeInTheDocument();
  });

  it('offers Retry only for NotFound and Failed', () => {
    const { unmount } = renderWithClient(
      <MovieCard movie={{ ...base, enrichmentStatus: 'Pending' }} />,
    );
    expect(screen.queryByRole('button', { name: 'Retry Heat' })).not.toBeInTheDocument();
    unmount();

    renderWithClient(<MovieCard movie={{ ...base, enrichmentStatus: 'Failed' }} />);
    expect(screen.getByRole('button', { name: 'Retry Heat' })).toBeInTheDocument();
  });
});

describe('MovieGrid loading semantics', () => {
  it('marks loading skeletons busy with the poster ratio', () => {
    const { container } = render(<MovieGrid isLoading pageSize={4} />);
    const busy = container.querySelector('[aria-busy="true"]');
    expect(busy).toBeInTheDocument();
    const skeletons = container.querySelectorAll('.movie-skeleton');
    expect(skeletons.length).toBeGreaterThan(0);
    for (const skeleton of skeletons) {
      expect(skeleton).toHaveStyle({ aspectRatio: '2 / 3' });
    }
  });
});
