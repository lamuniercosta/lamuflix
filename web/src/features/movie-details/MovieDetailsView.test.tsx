import { http, HttpResponse } from 'msw';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails, movieDetailsNullMetadata } from '../../mocks/fixtures';
import MovieDetailsView from './MovieDetailsView';

const fullMovie = {
  ...movieDetails,
  metadata: {
    ...movieDetails.metadata!,
    directors: ['Denis Villeneuve', 'Jon Spaihts'],
    actors: ['Timothée Chalamet', 'Rebecca Ferguson'],
    posterUrl: '/posters/dune.jpg',
    rottenTomatoesRating: 85,
    metaScore: 74,
    imdbRating: 8.0,
    runtime: 136,
    releaseYear: 2021,
    synopsis: 'A noble family becomes embroiled in a war.',
  },
};

describe('MovieDetailsView', () => {
  it('renders full metadata', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    render(<MovieDetailsView movieId={1} />);

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByAltText('Dune poster')).toBeInTheDocument();
    expect(screen.getByText(/IMDb 8\.0\/10/)).toBeInTheDocument();
    expect(screen.getByText(/Rotten Tomatoes/)).toBeInTheDocument();
    expect(screen.getByText(/Metacritic/)).toBeInTheDocument();
    expect(screen.getByText(/Denis Villeneuve, Jon Spaihts/)).toBeInTheDocument();
    expect(screen.getByText(/Timothée Chalamet, Rebecca Ferguson/)).toBeInTheDocument();
  });

  it('renders partial/null metadata', async () => {
    const partial = {
      ...movieDetails,
      metadata: {
        ...movieDetails.metadata!,
        posterUrl: null,
        rottenTomatoesRating: null,
        metaScore: null,
        directors: [],
        actors: [],
        synopsis: null,
      },
    };
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(partial)));
    render(<MovieDetailsView movieId={1} />);

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.queryByAltText('Dune poster')).not.toBeInTheDocument();
    expect(screen.getByText(/IMDb 8\.0\/10/)).toBeInTheDocument();
    expect(screen.queryByText(/Rotten Tomatoes/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Metacritic/)).not.toBeInTheDocument();
  });

  it('renders null metadata notice', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    render(<MovieDetailsView movieId={1} />);

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByText('No metadata available yet.')).toBeInTheDocument();
  });

  it('renders placeholder poster with no text when missing', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetails)));
    render(<MovieDetailsView movieId={1} />);

    await screen.findByRole('heading', { name: 'Dune' });
    expect(screen.queryByText('Dune poster')).not.toBeInTheDocument();
  });
});
