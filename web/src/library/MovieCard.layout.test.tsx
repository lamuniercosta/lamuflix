import { render, screen } from '@testing-library/react';
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
} as const;

describe('MovieCard layout semantics', () => {
  it('keeps the poster media at a 2:3 ratio', () => {
    const { container } = render(<MovieCard movie={{ ...base, enrichmentStatus: 'Enriched' }} />);
    const media = container.querySelector('.movie-card-media');
    expect(media).toHaveStyle({ aspectRatio: '2 / 3' });
  });

  it.each([
    ['NotFound', 'Not found'],
    ['Failed', 'Failed'],
    ['Pending', 'Pending'],
  ] as const)('announces enrichment state %s as text', (status, text) => {
    render(<MovieCard movie={{ ...base, enrichmentStatus: status }} />);
    expect(screen.getByText(text)).toBeInTheDocument();
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
