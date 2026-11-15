import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { components } from '../../api/types';
import MorePosterCard from './MorePosterCard';
import MoviePoster from './MoviePoster';

type MovieSummary = components['schemas']['MovieSummaryResponse'];

const item = {
  id: 7,
  title: 'Arrival',
  posterUrl: 'https://example.test/arrival.jpg',
  imdbRating: 7.9,
  releaseYear: 2016,
  enrichmentStatus: 'Enriched',
} as MovieSummary;

describe('MoviePoster', () => {
  it('falls back to the placeholder once its own poster errors', () => {
    render(<MoviePoster title="Dune" posterUrl="https://example.test/dune.jpg" />);

    fireEvent.error(screen.getByAltText('Dune poster'));

    expect(screen.queryByAltText('Dune poster')).not.toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Dune poster' })).toHaveClass(
      'details-poster-placeholder',
    );
  });

  it('renders the placeholder directly when there is no poster URL', () => {
    render(<MoviePoster title="Dune" posterUrl={null} />);

    expect(screen.getByRole('img', { name: 'Dune poster' })).toHaveClass(
      'details-poster-placeholder',
    );
  });
});

describe('MorePosterCard', () => {
  it('reports the card it was activated from through the shell callback', () => {
    const onOpen = vi.fn();
    render(<MorePosterCard item={item} onOpen={onOpen} />);

    fireEvent.click(screen.getByRole('link', { name: 'Arrival' }));

    expect(onOpen).toHaveBeenCalledWith(7);
  });

  it('owns its own image-failure state', () => {
    const onOpen = vi.fn();
    const { container } = render(<MorePosterCard item={item} onOpen={onOpen} />);

    fireEvent.error(container.querySelector('img') as HTMLImageElement);

    expect(container.querySelector('img')).toBeNull();
    expect(container.querySelector('.details-more-placeholder')).not.toBeNull();
    expect(onOpen).not.toHaveBeenCalled();
  });
});
