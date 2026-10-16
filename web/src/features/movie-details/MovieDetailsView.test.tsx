import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails, movieDetailsNullMetadata } from '../../mocks/fixtures';
import MovieDetailsView from './MovieDetailsView';

function renderView() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
}

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
    renderView();

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
    renderView();

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.queryByAltText('Dune poster')).not.toBeInTheDocument();
    expect(screen.getByText(/IMDb 8\.0\/10/)).toBeInTheDocument();
    expect(screen.queryByText(/Rotten Tomatoes/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Metacritic/)).not.toBeInTheDocument();
  });

  it('renders null metadata notice', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    renderView();

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByText('No metadata available yet.')).toBeInTheDocument();
  });

  it('renders placeholder poster with no text when missing', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetails)));
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    expect(screen.queryByText('Dune poster')).not.toBeInTheDocument();
  });

  it('reports playback started on 204', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 204 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));

    expect(await screen.findByText('Playback started.')).toBeInTheDocument();
  });

  it('reports removal on play 404', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 404 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));

    expect(await screen.findByText('This movie is no longer in the library.')).toBeInTheDocument();
  });

  it('reports generic error on play failure', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 500 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));

    expect(
      await screen.findByText("Couldn't start playback. Try again later."),
    ).toBeInTheDocument();
  });

  it('does not send a second play request while in flight', async () => {
    let calls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', async () => {
        calls += 1;
        await new Promise((resolve) => setTimeout(resolve, 100));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    const playButton = screen.getByRole('button', { name: 'Play' });
    fireEvent.click(playButton);
    await screen.findByText('Starting...');
    fireEvent.click(playButton);
    await waitFor(() => expect(screen.getByText('Playback started.')).toBeInTheDocument());
    expect(calls).toBe(1);
  });
});

describe('MovieDetailsView load states', () => {
  it('shows loading state while GET is pending', async () => {
    server.use(
      http.get('*/api/movies/:id', async () => {
        await new Promise((resolve) => setTimeout(resolve, 50));
        return HttpResponse.json(fullMovie);
      }),
    );
    renderView();

    expect(screen.getByRole('status')).toHaveTextContent('Loading movie…');
    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
  });

  it('shows Movie not found. on GET 404', async () => {
    server.use(http.get('*/api/movies/:id', () => new HttpResponse(null, { status: 404 })));
    renderView();

    expect(await screen.findByText('Movie not found.')).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Movie not found.');
  });

  it('shows generic error on GET failure', async () => {
    server.use(http.get('*/api/movies/:id', () => new HttpResponse(null, { status: 500 })));
    renderView();

    expect(
      await screen.findByText("Couldn't load this movie. Try again later."),
    ).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent(
      "Couldn't load this movie. Try again later.",
    );
  });
});

describe('MovieDetailsView Play 403 dialog', () => {
  it('opens dialog with focus, escapes and closes, returns focus', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 403 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    const playButton = screen.getByRole('button', { name: 'Play' });
    fireEvent.click(playButton);

    await screen.findByRole('dialog');
    expect(screen.getByText('Playback unavailable')).toBeInTheDocument();
    expect(screen.getByText('Local playback is disabled on this server.')).toBeInTheDocument();
    const closeButton = screen.getByRole('button', { name: 'Close' });
    expect(closeButton).toHaveFocus();

    fireEvent.keyDown(window, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(playButton).toHaveFocus());
  });

  it('closes the 403 dialog with Close button', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 403 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));
    const closeButton = await screen.findByRole('button', { name: 'Close' });
    fireEvent.click(closeButton);

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('keeps focus inside the 403 dialog on Tab', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 403 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));
    const closeButton = await screen.findByRole('button', { name: 'Close' });
    closeButton.focus();
    fireEvent.keyDown(window, { key: 'Tab' });
    expect(closeButton).toHaveFocus();
  });
});
