import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { server } from '../../test/server';
import {
  browseMovies,
  movieDetails,
  movieDetailsNullMetadata,
  people,
  problemDetails,
  relatedMovies,
} from '../../mocks/fixtures';
import { browseMoviesQueryKey } from '../../library/libraryQueries';
import {
  __resetMovieFeedbackStore,
  MOVIE_FEEDBACK_REFRESH_ERROR,
  MOVIE_FEEDBACK_SAVE_ERROR,
} from '../../library/movieFeedbackStore';
import MovieDetailsView from './MovieDetailsView';

function renderWith(client: QueryClient, movieId: number | string = 1) {
  return render(
    <QueryClientProvider client={client}>
      <MovieDetailsView movieId={movieId} />
    </QueryClientProvider>,
  );
}

function renderView(movieId: number | string = 1) {
  return renderWith(new QueryClient({ defaultOptions: { queries: { retry: false } } }), movieId);
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

function movieWithActors(actors: string[]) {
  return { ...fullMovie, metadata: { ...fullMovie.metadata, actors } };
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

beforeEach(() => {
  __resetMovieFeedbackStore();
});

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
    expect(await screen.findByRole('button', { name: 'Timothée Chalamet' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rebecca Ferguson' })).toBeInTheDocument();
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

describe('MovieDetailsView Play 403 inline message', () => {
  it('shows the unavailable message as an inline alert, not a dialog', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 403 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Playback unavailable');
    expect(alert).toHaveTextContent('Local playback is disabled on this server.');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('clears the message when Play is pressed again and playback starts', async () => {
    let attempt = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/play', () => {
        attempt += 1;
        return new HttpResponse(null, { status: attempt === 1 ? 403 : 204 });
      }),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    const playButton = screen.getByRole('button', { name: 'Play' });
    fireEvent.click(playButton);
    await screen.findByText('Playback unavailable');

    fireEvent.click(playButton);
    expect(await screen.findByText('Playback started.')).toBeInTheDocument();
    expect(screen.queryByText('Playback unavailable')).not.toBeInTheDocument();
  });
});

describe('MovieDetailsView cast and more-with', () => {
  it('sends both ids when a cast name matches two people', async () => {
    const sent: string[][] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieWithActors(['Timothée Chalamet']))),
      http.get('*/api/people', () =>
        HttpResponse.json([...people, { id: 9, name: 'Timothée Chalamet' }]),
      ),
      http.get('*/api/movies', ({ request }) => {
        sent.push(new URL(request.url).searchParams.getAll('ActorIds'));
        return HttpResponse.json(browseMovies);
      }),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Timothée Chalamet' }));
    expect(
      await screen.findByRole('heading', { name: 'More with Timothée Chalamet in your library' }),
    ).toBeInTheDocument();
    await waitFor(() => expect(sent).toContainEqual(['1', '9']));
  });

  it('keeps a cast name with no matching person as plain text', async () => {
    server.use(
      http.get('*/api/movies/:id', () =>
        HttpResponse.json(movieWithActors(['Timothée Chalamet', 'Unknown Actor'])),
      ),
    );
    renderView();

    expect(await screen.findByRole('button', { name: 'Timothée Chalamet' })).toBeInTheDocument();
    expect(screen.getByText('Unknown Actor')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Unknown Actor' })).not.toBeInTheDocument();
  });

  it('lists other library movies without the current one and opens a row', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieWithActors(['Timothée Chalamet']))),
      http.get('*/api/movies', () => HttpResponse.json(browseMovies)),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Timothée Chalamet' }));
    fireEvent.click(await screen.findByRole('link', { name: 'Arrival' }));

    expect(screen.queryByRole('link', { name: 'Dune' })).not.toBeInTheDocument();
    expect(window.location.pathname).toBe('/movies/2');
  });

  it('shows an empty state when no other library movie has the actor', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieWithActors(['Timothée Chalamet']))),
      http.get('*/api/movies', () =>
        HttpResponse.json({
          ...browseMovies,
          items: browseMovies.items.filter((item) => item.id === 1),
        }),
      ),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Timothée Chalamet' }));
    expect(
      await screen.findByText('No other movies with Timothée Chalamet in your library yet.'),
    ).toBeInTheDocument();
  });

  it('links See all with the actor in Library to the actor filter and opens it', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieWithActors(['Timothée Chalamet']))),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Timothée Chalamet' }));
    const link = await screen.findByRole('link', {
      name: 'See all with Timothée Chalamet in Library',
    });
    expect(link).toHaveAttribute('href', '/?actorIds=1');

    fireEvent.click(link);
    expect(window.location.pathname).toBe('/');
    expect(window.location.search).toBe('?actorIds=1');
  });
});

describe('MovieDetailsView file section', () => {
  const originalClipboard = Object.getOwnPropertyDescriptor(navigator, 'clipboard');

  afterEach(() => {
    if (originalClipboard) {
      Object.defineProperty(navigator, 'clipboard', originalClipboard);
    } else {
      Reflect.deleteProperty(navigator, 'clipboard');
    }
  });

  it('copies the path and reports Copied', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Copy path' }));
    expect(await screen.findByText('Copied')).toBeInTheDocument();
    expect(writeText).toHaveBeenCalledWith('/media/dune.mkv');
  });

  it('reports when the path cannot be copied', async () => {
    const writeText = vi.fn().mockRejectedValue(new Error('denied'));
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Copy path' }));
    expect(await screen.findByText("Couldn't copy the path.")).toBeInTheDocument();
  });

  it('opens the IMDb title in a new tab without opener access', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    renderView();

    const link = await screen.findByRole('link', { name: 'View on IMDb' });
    expect(link).toHaveAttribute('href', 'https://www.imdb.com/title/tt1160419/');
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener');
  });
});

describe('MovieDetailsView metadata request', () => {
  it('requests metadata, marks the library row Pending and refetches details', async () => {
    let enrichCalls = 0;
    let detailsCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => {
        detailsCalls += 1;
        return HttpResponse.json(movieDetailsNullMetadata);
      }),
      http.post('*/api/movies/:id/enrichment', () => {
        enrichCalls += 1;
        return new HttpResponse(null, { status: 202 });
      }),
    );
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    client.setQueryData(browseMoviesQueryKey, browseMovies);
    renderWith(client);

    fireEvent.click(await screen.findByRole('button', { name: 'Fetch metadata' }));
    const detailsBeforeRequest = detailsCalls;
    expect(await screen.findByText('A metadata lookup is in progress.')).toBeInTheDocument();
    await waitFor(() => expect(enrichCalls).toBe(1));
    await waitFor(() => expect(detailsCalls).toBeGreaterThan(detailsBeforeRequest));
    const page = client.getQueryData<{
      items: Array<{ id: number | string; enrichmentStatus: string }>;
    }>(browseMoviesQueryKey);
    expect(page?.items.find((item) => String(item.id) === '1')?.enrichmentStatus).toBe('Pending');
  });

  it('shows a status line when a lookup is already running (409)', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.post('*/api/movies/:id/enrichment', () => new HttpResponse(null, { status: 409 })),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Fetch metadata' }));
    const message = await screen.findByText('A metadata lookup is already running for this movie.');
    expect(message).toHaveAttribute('role', 'status');
  });

  it('names a failed lookup when the library cache holds a Failed row', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    client.setQueryData(browseMoviesQueryKey, {
      ...browseMovies,
      items: browseMovies.items.map((item) =>
        String(item.id) === '1' ? { ...item, enrichmentStatus: 'Failed' } : item,
      ),
    });
    renderWith(client);

    expect(await screen.findByText('The last metadata lookup failed.')).toBeInTheDocument();
  });
});

describe('MovieDetailsView related movies integration', () => {
  function recommendationsHandler(body: object) {
    return http.get('*/api/movies/:id/recommendations', () => HttpResponse.json(body));
  }

  function recordedRecommendationsHandler(requests: string[]) {
    return http.get('*/api/movies/:id/recommendations', ({ request }) => {
      requests.push(request.url);
      return HttpResponse.json([]);
    });
  }

  it('renders Related movies after More with and before File when populated', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieWithActors(['Timothée Chalamet']))),
      http.get('*/api/movies', () => HttpResponse.json(browseMovies)),
      recommendationsHandler(relatedMovies),
    );
    const { container } = renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Timothée Chalamet' }));
    expect(
      await screen.findByRole('heading', { name: 'More with Timothée Chalamet in your library' }),
    ).toBeInTheDocument();
    const region = await screen.findByRole('region', { name: 'Related movies' });

    const sectionClasses = Array.from(
      container.querySelectorAll('.details > .details-section'),
    ).map((section) => section.className);
    expect(sectionClasses).toEqual([
      'details-section details-more',
      'details-section related-movies',
      'details-section details-file',
    ]);

    const arrival = within(region).getByRole('link', { name: /Arrival/ });
    expect(
      within(arrival.closest('article') as HTMLElement).getByText('Same director'),
    ).toBeInTheDocument();
    expect(within(region).getByRole('link', { name: /Prisoners/ })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'File' })).toBeInTheDocument();
    expect(screen.getByText('/media/dune.mkv')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Copy path' })).toBeInTheDocument();
  });

  it('navigates a recommendation card from the host view retaining the library return URL', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      recommendationsHandler(relatedMovies),
    );
    window.history.replaceState({ libraryReturnUrl: '/?genreIds=1' }, '', '/movies/1');
    renderView();

    const region = await screen.findByRole('region', { name: 'Related movies' });
    fireEvent.click(within(region).getByRole('link', { name: /Arrival/ }));

    expect(window.location.pathname).toBe('/movies/2');
    expect(window.history.state).toEqual({ libraryReturnUrl: '/?genreIds=1' });
  });

  it('does not request recommendations while the host movie is loading', async () => {
    const requests: string[] = [];
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      http.get('*/api/movies/:id', async () => {
        await gate;
        return HttpResponse.json(fullMovie);
      }),
      recordedRecommendationsHandler(requests),
    );
    renderView();

    expect(screen.getByRole('status')).toHaveTextContent('Loading movie…');
    await act(async () => {
      await Promise.resolve();
    });
    expect(requests).toEqual([]);

    release();
    expect(await screen.findByText('No related movies yet.')).toBeInTheDocument();
  });

  it('does not request recommendations when the host movie fails to load', async () => {
    const requests: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => new HttpResponse(null, { status: 500 })),
      recordedRecommendationsHandler(requests),
    );
    renderView();

    expect(
      await screen.findByText("Couldn't load this movie. Try again later."),
    ).toBeInTheDocument();
    expect(requests).toEqual([]);
  });

  it('does not request recommendations when the host movie is missing', async () => {
    const requests: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => new HttpResponse(null, { status: 404 })),
      recordedRecommendationsHandler(requests),
    );
    renderView();

    expect(await screen.findByText('Movie not found.')).toBeInTheDocument();
    expect(requests).toEqual([]);
  });

  it('does not request or render recommendations for an invalid host movie id', async () => {
    const requests: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      recordedRecommendationsHandler(requests),
    );
    renderView('abc');

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Related movies' })).not.toBeInTheDocument();
    expect(requests).toEqual([]);
  });
});

describe('MovieDetailsView feedback actions', () => {
  function deferred<T = void>() {
    let resolve!: (value: T) => void;
    const promise = new Promise<T>((r) => {
      resolve = r;
    });
    return { promise, resolve };
  }

  it('renders both feedback actions on Movie Details', async () => {
    // arrange
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));

    // act
    renderView();

    // assert
    await screen.findByRole('heading', { name: 'Dune' });
    expect(screen.getByRole('button', { name: 'Mark Watched, Dune' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Not Interested, Dune' })).toBeEnabled();
  });

  it('marks the selected flag before the response and preserves the other flag', async () => {
    // arrange
    const gate = deferred();
    let posts = 0;
    server.use(
      http.get('*/api/movies/:id', () =>
        HttpResponse.json({ ...fullMovie, isNotInterested: true }),
      ),
      http.post('*/api/movies/:id/watched', async () => {
        posts += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Dune' }));

    // assert
    expect(screen.getByRole('button', { name: 'Watched, Dune' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Not Interested (marked), Dune' })).toBeDisabled();
    await waitFor(() => expect(posts).toBe(1));

    gate.resolve();

    await waitFor(() =>
      expect(
        screen.getByRole('button', { name: 'Not Interested (marked), Dune' }),
      ).not.toHaveAttribute('aria-busy'),
    );
    expect(screen.getByRole('button', { name: 'Watched, Dune' })).toBeDisabled();
  });

  it('keeps both actions blocked through deferred reconciliation', async () => {
    // arrange
    const gate = deferred();
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/watched', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Dune' }));

    // assert
    expect(screen.getByRole('button', { name: 'Watched, Dune' })).toHaveAttribute(
      'aria-busy',
      'true',
    );
    expect(screen.getByRole('button', { name: 'Not Interested, Dune' })).toHaveAttribute(
      'aria-busy',
      'true',
    );

    gate.resolve();

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Not Interested, Dune' })).toBeEnabled(),
    );
  });

  it('renders an accessible mutation error with an explicit retry', async () => {
    // arrange
    let posts = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: posts === 1 ? 500 : 204 });
      }),
    );
    renderView();
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Dune' }));

    // assert
    expect(await screen.findByText(MOVIE_FEEDBACK_SAVE_ERROR)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Mark Watched, Dune' })).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: 'Retry saving feedback, Dune' }));

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Watched, Dune' })).toBeVisible(),
    );
    expect(screen.queryByText(MOVIE_FEEDBACK_SAVE_ERROR)).not.toBeInTheDocument();
    expect(posts).toBe(2);
  });

  it('keeps the confirmed mark and retries only the refresh after a refresh failure', async () => {
    // arrange
    let posts = 0;
    let fetches = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
      http.get('*/api/movies/:id/recommendations', () => {
        fetches += 1;
        if (fetches === 2) {
          return HttpResponse.json(problemDetails(500), { status: 500 });
        }
        return HttpResponse.json([]);
      }),
    );
    renderView();
    await screen.findByRole('heading', { name: 'Dune' });
    await screen.findByText('No related movies yet.');

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Dune' }));

    // assert
    expect(await screen.findByText(MOVIE_FEEDBACK_REFRESH_ERROR)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Watched, Dune' })).toBeVisible();
    expect(
      screen.queryByRole('button', { name: 'Retry saving feedback, Dune' }),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Retry refresh, Dune' }));

    await waitFor(() =>
      expect(screen.queryByText(MOVIE_FEEDBACK_REFRESH_ERROR)).not.toBeInTheDocument(),
    );
    expect(screen.getByRole('button', { name: 'Watched, Dune' })).toBeVisible();
    expect(posts).toBe(1);
    expect(fetches).toBe(3);
  });

  it('renders no fabricated reasons on ordinary Details data', async () => {
    // arrange
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.get('*/api/movies/:id/recommendations', () => HttpResponse.json([])),
    );

    // act
    const { container } = renderView();

    // assert
    await screen.findByRole('heading', { name: 'Dune' });
    await screen.findByText('No related movies yet.');
    expect(container.querySelector('.related-movie-reason')).toBeNull();
  });
});
