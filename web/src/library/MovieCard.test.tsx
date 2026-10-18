import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { ReactElement } from 'react';
import { describe, expect, it } from 'vitest';
import { server } from '../test/server';
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
  isWatched: false,
  isNotInterested: false,
} as const;

function renderCard(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('MovieCard', () => {
  it('renders the poster, title, rating and year with no badge or Details link', () => {
    renderCard(<MovieCard movie={movie} />);

    expect(screen.getByRole('heading', { level: 3, name: 'Heat' })).toBeInTheDocument();
    expect(screen.getByAltText('Heat')).toHaveAttribute('src', '/posters/heat.jpg');
    expect(screen.getByText('1995')).toBeInTheDocument();
    expect(screen.getByText('8.3')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Play Heat' })).toBeInTheDocument();
    expect(screen.queryByText('Enriched')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Details' })).not.toBeInTheDocument();
    expect(screen.queryByText('170 min')).not.toBeInTheDocument();
  });

  it('renders a titled placeholder and dashes for missing values', () => {
    renderCard(
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
    expect(screen.getAllByText('—')).toHaveLength(2);
    expect(screen.getByText('Pending')).toBeInTheDocument();
  });

  it('replaces the poster with the placeholder when the image fails to load', () => {
    renderCard(<MovieCard movie={movie} />);

    fireEvent.error(screen.getByAltText('Heat'));

    expect(screen.getByRole('img', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.queryByAltText('Heat')).not.toBeInTheDocument();
  });

  it('exposes the IMDb rating as an accessible label', () => {
    const { container } = renderCard(<MovieCard movie={movie} />);
    expect(container.querySelector('[aria-label="IMDb rating 8.3"]')).toBeInTheDocument();
  });

  it.each(['NotFound', 'Failed'] as const)('offers Retry for enrichment status %s', (status) => {
    renderCard(<MovieCard movie={{ ...movie, enrichmentStatus: status }} />);
    expect(screen.getByRole('button', { name: 'Retry Heat' })).toBeInTheDocument();
  });

  it('shows the forbidden playback message inline on the card without a dialog', async () => {
    server.use(http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 403 })));
    renderCard(<MovieCard movie={movie} />);

    fireEvent.click(screen.getByRole('button', { name: 'Play Heat' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Playback is not available for this title.',
    );
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
