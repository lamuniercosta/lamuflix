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
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import { playerRegionLabel, playLabel } from '../playback/playerCopy';
import { movieStreamUrl } from '../playback/streamSource';
import { browseMoviesQueryKey } from '../../library/libraryQueries';
import type {
  EnrichmentStatusNotification,
  EnrichmentStatusSubscription,
} from '../../library/useEnrichmentStatus';
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
});

describe('MovieDetailsView browser Play default', () => {
  let media: MediaStubHandle;

  beforeEach(() => {
    media = installMediaStubs();
  });

  afterEach(() => {
    media.restore();
  });

  function recordLaunchPosts(posts: string[]) {
    return http.post('*/api/movies/:id/play', ({ request }) => {
      posts.push(request.url);
      return new HttpResponse(null, { status: 204 });
    });
  }

  it('opens the labelled player from Play and never posts the launch contract', async () => {
    const posts: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      recordLaunchPosts(posts),
    );
    const { container } = renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    expect(screen.queryByRole('region', { name: playerRegionLabel('Dune') })).toBeNull();

    const playButton = screen.getByRole('button', { name: playLabel('Dune') });
    fireEvent.click(playButton);

    const playerRegion = await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    expect(screen.getByRole('button', { name: playLabel('Dune') })).toBe(playButton);
    expect(playerRegion.querySelector('media-control-bar media-play-button')).toBeInTheDocument();
    expect(container.querySelectorAll('video')).toHaveLength(1);
    expect(media.calls).toContain(`src:${movieStreamUrl(1)}`);
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(posts).toEqual([]);
  });

  it('keeps one active video when Play is activated twice', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    const { container } = renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    const playButton = screen.getByRole('button', { name: playLabel('Dune') });
    fireEvent.click(playButton);
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    fireEvent.click(playButton);

    expect(container.querySelectorAll('video')).toHaveLength(1);
  });

  it('opens the player and attempts playback when details arrive with card player intent', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    window.history.replaceState({ playerIntent: true }, '', '/movies/1');
    renderView();

    const region = await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    expect(region).toBeInTheDocument();
    await waitFor(() => expect(media.calls).toContain('play'));
  });

  it('leaves the player closed on a direct load without player intent', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    window.history.replaceState(null, '', '/movies/1');
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    expect(screen.queryByRole('region', { name: playerRegionLabel('Dune') })).toBeNull();
    expect(screen.getByRole('button', { name: playLabel('Dune') })).toBeInTheDocument();
  });

  it('keeps the rest of the details page usable while the player is open', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });

    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'File' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remove from library' })).toBeEnabled();
  });

  it('tears the outgoing player down before a card-navigation swap attaches the incoming one', async () => {
    server.use(
      http.get('*/api/movies/:id', ({ params }) =>
        HttpResponse.json({
          ...fullMovie,
          id: Number(params.id),
          title: params.id === '2' ? 'Arrival' : 'Dune',
        }),
      ),
      http.get('*/api/movies/:id/recommendations', () => HttpResponse.json([])),
    );
    const first = renderView(1);

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    media.calls.length = 0;

    window.history.replaceState({ playerIntent: true }, '', '/movies/2');
    first.unmount();
    renderView(2);

    await screen.findByRole('region', { name: playerRegionLabel('Arrival') });
    const pause = media.calls.indexOf('pause');
    const clear = media.calls.indexOf('clear:src');
    const incoming = media.calls.indexOf(`src:${movieStreamUrl(2)}`);
    expect(pause).toBeGreaterThanOrEqual(0);
    expect(clear).toBeGreaterThan(pause);
    expect(incoming).toBeGreaterThan(clear);
    expect(document.querySelectorAll('video')).toHaveLength(1);
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

  it('requests the people lookup with role=actor', async () => {
    const recorded: (string | null)[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieWithActors(['Timothée Chalamet']))),
      http.get('*/api/people', ({ request }) => {
        recorded.push(new URL(request.url).searchParams.get('role'));
        return HttpResponse.json(people, { status: 200 });
      }),
    );
    renderView();

    await waitFor(() => expect(recorded.length).toBeGreaterThan(0));
    recorded.forEach((role) => expect(role).toBe('actor'));
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
      'details-section details-trailer',
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

  it('renders the shared four-icon row on Movie Details with one bookmark', async () => {
    // arrange
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));

    // act
    const { container } = renderView();

    // assert
    await screen.findByRole('heading', { name: 'Dune' });
    const row = container.querySelector('.movie-feedback-actions') as HTMLElement;
    expect(row).not.toBeNull();
    expect(row.querySelectorAll('button')).toHaveLength(4);
    expect(screen.getByRole('button', { name: 'Mark Watched, Dune' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Like, Dune' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Dislike, Dune' })).toBeEnabled();
    expect(screen.getAllByRole('button', { name: 'Add to Watchlist, Dune' })).toHaveLength(1);
    expect(container.querySelector('.details-watchlist')).toBeNull();
    expect(container.textContent).not.toContain('Not Interested');
  });

  it('marks the selected flag before the response and preserves the review', async () => {
    // arrange
    const gate = deferred();
    let posts = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json({ ...fullMovie, review: 'disliked' })),
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
    expect(screen.getByRole('button', { name: 'Unmark Watched, Dune' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Clear disliked review, Dune' })).toBeDisabled();
    await waitFor(() => expect(posts).toBe(1));

    gate.resolve();

    await waitFor(() =>
      expect(
        screen.getByRole('button', { name: 'Clear disliked review, Dune' }),
      ).not.toHaveAttribute('aria-busy'),
    );
    expect(screen.getByRole('button', { name: 'Unmark Watched, Dune' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Clear disliked review, Dune' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('reflects a review change made on Details in the shared store', async () => {
    // arrange
    const bodies: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.put('*/api/movies/:id/review', async ({ request }) => {
        bodies.push(((await request.json()) as { review: string }).review);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Like, Dune' }));

    // assert
    expect(screen.getByRole('button', { name: 'Clear liked review, Dune' })).toBeDisabled();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Clear liked review, Dune' })).toBeEnabled(),
    );
    expect(screen.getByRole('button', { name: 'Clear liked review, Dune' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: 'Dislike, Dune' })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
    expect(bodies).toEqual(['liked']);
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
    expect(screen.getByRole('button', { name: 'Unmark Watched, Dune' })).toHaveAttribute(
      'aria-busy',
      'true',
    );
    expect(screen.getByRole('button', { name: 'Like, Dune' })).toHaveAttribute('aria-busy', 'true');

    gate.resolve();

    await waitFor(() => expect(screen.getByRole('button', { name: 'Like, Dune' })).toBeEnabled());
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
    const retryButton = await screen.findByRole('button', {
      name: 'Retry saving feedback, Dune',
    });
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Mark Watched, Dune' })).toBeEnabled(),
    );
    await waitFor(() => expect(retryButton).toBeEnabled());

    fireEvent.click(retryButton);

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Unmark Watched, Dune' })).toBeVisible(),
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
    expect(screen.getByRole('button', { name: 'Unmark Watched, Dune' })).toBeVisible();
    expect(
      screen.queryByRole('button', { name: 'Retry saving feedback, Dune' }),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Retry refresh, Dune' }));

    await waitFor(() =>
      expect(screen.queryByText(MOVIE_FEEDBACK_REFRESH_ERROR)).not.toBeInTheDocument(),
    );
    expect(screen.getByRole('button', { name: 'Unmark Watched, Dune' })).toBeVisible();
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

describe('MovieDetailsView enrichment hub bridge', () => {
  function createSubscriptionSeam() {
    const listeners = new Set<(notification: EnrichmentStatusNotification) => void>();
    const subscribe = vi.fn((listener: (notification: EnrichmentStatusNotification) => void) => {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    });
    return {
      seam: { subscribe } satisfies EnrichmentStatusSubscription,
      subscribe,
      publish(notification: EnrichmentStatusNotification) {
        for (const listener of [...listeners]) {
          listener(notification);
        }
      },
      listenerCount: () => listeners.size,
    };
  }

  function renderWithSeam(
    client: QueryClient,
    seam: EnrichmentStatusSubscription,
    movieId: number | string = 1,
  ) {
    return render(
      <QueryClientProvider client={client}>
        <MovieDetailsView movieId={movieId} enrichmentStatus={seam} />
      </QueryClientProvider>,
    );
  }

  function countingDetailsHandler(counter: { calls: number }) {
    return http.get('*/api/movies/:id', () => {
      counter.calls += 1;
      return HttpResponse.json(fullMovie);
    });
  }

  it('reconciles mounted details for an id absent from every browse cache', async () => {
    // arrange
    const counter = { calls: 0 };
    server.use(countingDetailsHandler(counter));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { seam, publish } = createSubscriptionSeam();
    renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });
    const before = counter.calls;

    // act
    act(() => {
      publish({ kind: 'event', payload: { id: 1, enrichmentStatus: 'Enriched' } });
    });

    // assert
    await waitFor(() => expect(counter.calls).toBeGreaterThan(before));
    expect(client.getQueryCache().find({ queryKey: ['browseMovies'] })).toBeUndefined();
  });

  it('reconciles on every status transition and stays independent of status diffs', async () => {
    // arrange
    const counter = { calls: 0 };
    server.use(countingDetailsHandler(counter));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { seam, publish } = createSubscriptionSeam();
    renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    for (const status of ['Pending', 'Enriched', 'NotFound', 'Failed', 'Failed'] as const) {
      const before = counter.calls;
      act(() => {
        publish({ kind: 'event', payload: { id: 1, enrichmentStatus: status } });
      });

      // assert
      await waitFor(() => expect(counter.calls).toBeGreaterThan(before));
    }
  });

  it('reconciles on successful initial start and on reconnect with no event', async () => {
    // arrange
    const counter = { calls: 0 };
    server.use(countingDetailsHandler(counter));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { seam, publish } = createSubscriptionSeam();
    renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    const startBefore = counter.calls;
    act(() => {
      publish({ kind: 'recovery' });
    });

    // assert
    await waitFor(() => expect(counter.calls).toBeGreaterThan(startBefore));

    const reconnectBefore = counter.calls;
    act(() => {
      publish({ kind: 'recovery' });
    });
    await waitFor(() => expect(counter.calls).toBeGreaterThan(reconnectBefore));
  });

  it('ignores events for a different movie id', async () => {
    // arrange
    const counter = { calls: 0 };
    server.use(countingDetailsHandler(counter));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { seam, publish } = createSubscriptionSeam();
    renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });
    const before = counter.calls;

    // act
    act(() => {
      publish({ kind: 'event', payload: { id: 2, enrichmentStatus: 'Enriched' } });
    });
    await Promise.resolve();

    // assert
    expect(counter.calls).toBe(before);
  });

  it('unsubscribes on cleanup without leaving a second hub connection', async () => {
    // arrange
    const counter = { calls: 0 };
    server.use(countingDetailsHandler(counter));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { seam, publish, subscribe, listenerCount } = createSubscriptionSeam();
    const { unmount } = renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });
    expect(subscribe).toHaveBeenCalledTimes(1);

    // act
    unmount();
    const before = counter.calls;
    act(() => {
      publish({ kind: 'recovery' });
    });
    await Promise.resolve();

    // assert
    expect(listenerCount()).toBe(0);
    expect(counter.calls).toBe(before);
  });

  it('does not invalidate root browse caches from the details bridge alone', async () => {
    // arrange
    server.use(countingDetailsHandler({ calls: 0 }));
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    client.setQueryData(browseMoviesQueryKey, browseMovies);
    const { seam, publish } = createSubscriptionSeam();
    renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    act(() => {
      publish({ kind: 'event', payload: { id: 1, enrichmentStatus: 'Enriched' } });
      publish({ kind: 'recovery' });
    });
    await waitFor(() =>
      expect(client.getQueryState(browseMoviesQueryKey)?.isInvalidated ?? false).toBe(false),
    );

    // assert
    expect(client.getQueryState(browseMoviesQueryKey)?.isInvalidated ?? false).toBe(false);
  });

  it('yields a fresh details pass when an event arrives during an older refetch', async () => {
    // arrange
    const responses = [
      fullMovie,
      { ...fullMovie, title: 'Dune Stale' },
      { ...fullMovie, title: 'Dune Part Two' },
    ];
    const resolvers: Array<() => void> = [];
    let detailsCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => {
        const pass = detailsCalls;
        detailsCalls += 1;
        if (pass === 0) {
          return HttpResponse.json(responses[0]);
        }
        return new Promise((resolve) => {
          resolvers.push(() => resolve(HttpResponse.json(responses[pass])));
        });
      }),
    );
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { seam, publish } = createSubscriptionSeam();
    renderWithSeam(client, seam);
    await screen.findByRole('heading', { name: 'Dune' });

    // act
    act(() => {
      publish({ kind: 'event', payload: { id: 1, enrichmentStatus: 'Enriched' } });
    });
    await waitFor(() => expect(detailsCalls).toBe(2));
    act(() => {
      publish({ kind: 'event', payload: { id: 1, enrichmentStatus: 'NotFound' } });
    });
    await waitFor(() => expect(detailsCalls).toBe(3));
    resolvers[0]();
    await act(async () => {
      await Promise.resolve();
    });
    resolvers[1]();

    // assert
    expect(await screen.findByRole('heading', { name: 'Dune Part Two' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Dune Stale' })).not.toBeInTheDocument();
  });
});
