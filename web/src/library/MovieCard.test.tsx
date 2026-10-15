import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import MovieCard from './MovieCard';

const movie = {
  id: 1,
  title: 'Heat',
  posterUrl: '/posters/heat.jpg',
  releaseYear: 1995,
  runtime: 170,
  imdbRating: 8.3,
  enrichmentStatus: 'Enriched',
  isInWatchlist: false,
} as const;

describe('MovieCard', () => {
  it('renders populated tile fields', () => {
    render(<MovieCard movie={movie} />);

    expect(screen.getByRole('heading', { level: 3, name: 'Heat' })).toBeInTheDocument();
    expect(screen.getByAltText('Heat')).toHaveAttribute('src', '/posters/heat.jpg');
    expect(screen.getByText('1995')).toBeInTheDocument();
    expect(screen.getByText('170 min')).toBeInTheDocument();
    expect(screen.getByText('8.3')).toBeInTheDocument();
    expect(screen.getByText('Enriched')).toBeInTheDocument();
  });

  it('renders a title-labelled placeholder for a null poster', () => {
    render(
      <MovieCard
        movie={{
          ...movie,
          posterUrl: null,
          releaseYear: null,
          runtime: null,
          imdbRating: null,
          enrichmentStatus: 'Pending',
        }}
      />,
    );

    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.queryByAltText('Heat')).not.toBeInTheDocument();
    expect(screen.getAllByText('—')).toHaveLength(3);
    expect(screen.getByText('Pending')).toBeInTheDocument();
  });

  it('replaces the poster with the placeholder when the image fails to load', () => {
    render(<MovieCard movie={movie} />);

    fireEvent.error(screen.getByAltText('Heat'));

    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.queryByAltText('Heat')).not.toBeInTheDocument();
  });

  it('exposes the IMDb rating as an accessible label', () => {
    const { container } = render(<MovieCard movie={movie} />);
    expect(container.querySelector('[aria-label="IMDb rating 8.3"]')).toBeInTheDocument();
  });
});
