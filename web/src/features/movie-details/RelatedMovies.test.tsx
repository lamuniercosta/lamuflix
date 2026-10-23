import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { problemDetails, relatedMovies } from '../../mocks/fixtures';
import {
  __resetMovieFeedbackStore,
  MOVIE_FEEDBACK_REFRESH_ERROR,
} from '../../library/movieFeedbackStore';
import RelatedMovies from './RelatedMovies';
import { relatedMoviesQueryKey, type RecommendedMovie } from './relatedMoviesApi';

function recommendation(
  overrides: Partial<RecommendedMovie> & Pick<RecommendedMovie, 'id' | 'title'>,
): RecommendedMovie {
  return {
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    score: 0.5,
    reasons: [],
    ...overrides,
  };
}

function renderRelated(movieId: number | string | null | undefined, client?: QueryClient) {
  const queryClient = client ?? new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const result = render(
    <QueryClientProvider client={queryClient}>
      <RelatedMovies movieId={movieId} />
    </QueryClientProvider>,
  );
  return { ...result, queryClient };
}

function recommendationsHandler(body: RecommendedMovie[] | object, status = 200) {
  return http.get('*/api/movies/:id/recommendations', () => HttpResponse.json(body, { status }));
}

function reasonTexts(scope: HTMLElement): string[] {
  const card = scope.closest('article') ?? scope;
  return Array.from(card.querySelectorAll('.related-movie-reason')).map(
    (chip) => chip.textContent ?? '',
  );
}

beforeEach(() => {
  __resetMovieFeedbackStore();
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('RelatedMovies', () => {
  it('shows the empty message without a carousel for a successful empty response', async () => {
    // arrange
    server.use(recommendationsHandler([]));

    // act
    renderRelated(1);

    // assert
    expect(await screen.findByText('No related movies yet.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Related movies' })).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Related movies' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Previous' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Next' })).not.toBeInTheDocument();
  });

  it('shows an accessible loading placeholder while the first request is pending', async () => {
    // arrange
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      http.get('*/api/movies/:id/recommendations', async () => {
        await gate;
        return HttpResponse.json([]);
      }),
    );

    // act
    renderRelated(1);

    // assert
    expect(screen.getByRole('status')).toHaveTextContent('Loading related movies…');
    expect(screen.queryByRole('region', { name: 'Related movies' })).not.toBeInTheDocument();

    // act
    release();

    // assert
    expect(await screen.findByText('No related movies yet.')).toBeInTheDocument();
  });

  it('renders populated cards in API order inside the carousel', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));

    // act
    renderRelated(1);

    // assert
    const region = await screen.findByRole('region', { name: 'Related movies' });
    const items = within(region).getAllByRole('listitem');
    expect(items).toHaveLength(3);
    expect(items.map((item) => item.querySelector('.movie-card-title')?.textContent)).toEqual([
      'Arrival',
      'Blade Runner 2049',
      'Prisoners',
    ]);
    expect(screen.queryByText(/0\.82/)).not.toBeInTheDocument();
    expect(screen.queryByText('No related movies yet.')).not.toBeInTheDocument();
  });

  it('renders every Reason verbatim and in order as inert text', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));

    // act
    renderRelated(1);

    // assert
    const arrival = await screen.findByRole('link', { name: /Arrival/ });
    expect(reasonTexts(arrival)).toEqual(['Same director', 'Matches your Drama interest']);
    const arrivalCard = arrival.closest('article') as HTMLElement;
    expect(
      Array.from(arrivalCard.querySelectorAll('.related-movie-reason')).map(
        (chip) => chip.className,
      ),
    ).toEqual([
      'related-movie-reason related-movie-reason-structural',
      'related-movie-reason related-movie-reason-personal',
    ]);
    expect(screen.queryByRole('button', { name: 'Same director' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Same director' })).not.toBeInTheDocument();
  });

  it('renders no chips and no invented explanation for empty Reasons', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));

    // act
    renderRelated(1);

    // assert
    const noReasons = await screen.findByRole('link', { name: /Blade Runner 2049/ });
    const card = noReasons.closest('article') as HTMLElement;
    expect(reasonTexts(card)).toEqual([]);
    expect(within(card).getByText('Blade Runner 2049')).toBeInTheDocument();
    expect(within(card).getByText('2017')).toBeInTheDocument();
  });

  it('renders the neutral fallback for missing and broken posters', async () => {
    // arrange
    server.use(
      recommendationsHandler([
        recommendation({ id: 2, title: 'Broken poster', posterUrl: '/posters/gone.jpg' }),
        recommendation({ id: 3, title: 'Missing poster', posterUrl: null }),
      ]),
    );

    // act
    const { container } = renderRelated(1);
    await screen.findByRole('link', { name: /Broken poster/ });

    // assert
    const image = container.querySelector('img');
    expect(image).not.toBeNull();

    // act
    fireEvent.error(image as HTMLImageElement);

    // assert
    expect(container.querySelector('img')).toBeNull();
    expect(container.querySelectorAll('.movie-card-placeholder')).toHaveLength(2);
    expect(screen.getByRole('link', { name: /Broken poster/ })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Missing poster/ })).toBeInTheDocument();
  });

  it('renders ReleaseYear only when the recommendation has one', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));

    // act
    renderRelated(1);

    // assert
    const arrival = await screen.findByRole('link', { name: /Arrival/ });
    const prisoners = screen.getByRole('link', { name: /Prisoners/ });
    expect(within(arrival.closest('article') as HTMLElement).getByText('2016')).toBeInTheDocument();
    expect(prisoners.textContent).not.toMatch(/\b(19|20)\d{2}\b/);
  });

  it('navigates a primary card activation to the movie details route retaining the library return URL', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));
    window.history.replaceState({ libraryReturnUrl: '/?genreIds=1' }, '', '/movies/1');

    // act
    renderRelated(1);
    const arrival = await screen.findByRole('link', { name: /Arrival/ });
    fireEvent.click(arrival);

    // assert
    expect(arrival).toHaveAttribute('href', '/movies/2');
    expect(window.location.pathname).toBe('/movies/2');
    expect(window.history.state).toEqual({ libraryReturnUrl: '/?genreIds=1' });
  });

  it('drops an invalid library return URL when navigating', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));
    window.history.replaceState({ libraryReturnUrl: 'https://evil.example/' }, '', '/movies/1');

    // act
    renderRelated(1);
    const arrival = await screen.findByRole('link', { name: /Arrival/ });
    fireEvent.click(arrival);

    // assert
    expect(window.location.pathname).toBe('/movies/2');
    expect(window.history.state).toEqual({});
  });

  it('keeps native modified activation instead of intercepting it', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));
    window.history.replaceState({}, '', '/movies/1');
    renderRelated(1);
    const arrival = await screen.findByRole('link', { name: /Arrival/ });

    // act
    const ctrlResult = fireEvent.click(arrival, { ctrlKey: true });
    const metaResult = fireEvent.click(arrival, { metaKey: true });

    // assert
    expect(ctrlResult).toBe(true);
    expect(metaResult).toBe(true);
    expect(window.location.pathname).toBe('/movies/1');
  });

  it('shows a local notice and refetches on Retry after a failure', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/movies/:id/recommendations', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json(problemDetails(500), { status: 500 });
        }
        return HttpResponse.json([recommendation({ id: 2, title: 'Arrival' })]);
      }),
    );

    // act
    renderRelated(1);

    // assert
    const notice = await screen.findByRole('alert');
    expect(notice).toHaveTextContent('Could not load related movies.');
    expect(screen.queryByText('No related movies yet.')).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Related movies' })).not.toBeInTheDocument();

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    // assert
    expect(await screen.findByRole('link', { name: /Arrival/ })).toBeInTheDocument();
    expect(calls).toBe(2);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('treats a recommendations 404 as a local failure without a false empty result', async () => {
    // arrange
    server.use(recommendationsHandler(problemDetails(404), 404));

    // act
    renderRelated(1);

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load related movies.');
    expect(screen.queryByText('No related movies yet.')).not.toBeInTheDocument();
    expect(screen.queryByText('Movie not found.')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });

  it('retains populated data and shows the local notice when a background refetch fails', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/movies/:id/recommendations', () => {
        calls += 1;
        if (calls === 2) {
          return HttpResponse.json(problemDetails(500), { status: 500 });
        }
        return HttpResponse.json([recommendation({ id: 2, title: 'Arrival' })]);
      }),
    );
    const { queryClient } = renderRelated(1);
    await screen.findByRole('link', { name: /Arrival/ });

    // act
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: relatedMoviesQueryKey(1) });
    });

    // assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load related movies.');
    expect(screen.getByRole('link', { name: /Arrival/ })).toBeInTheDocument();

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    // assert
    await waitFor(() => expect(calls).toBe(3));
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
    expect(screen.getByRole('link', { name: /Arrival/ })).toBeInTheDocument();
  });

  it('keeps movie identities isolated when requests resolve out of order', async () => {
    // arrange
    let releaseFirst: () => void = () => {};
    const firstGate = new Promise<void>((resolve) => {
      releaseFirst = resolve;
    });
    const requested: number[] = [];
    server.use(
      http.get('*/api/movies/:id/recommendations', async ({ params }) => {
        const id = Number(params.id);
        requested.push(id);
        if (id === 1) {
          await firstGate;
          return HttpResponse.json([recommendation({ id: 21, title: 'First movie' })]);
        }
        return HttpResponse.json([recommendation({ id: 22, title: 'Second movie' })]);
      }),
    );
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { rerender } = render(
      <QueryClientProvider client={queryClient}>
        <RelatedMovies movieId={1} />
      </QueryClientProvider>,
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Loading related movies…');

    // act
    rerender(
      <QueryClientProvider client={queryClient}>
        <RelatedMovies movieId={2} />
      </QueryClientProvider>,
    );

    // assert
    expect(await screen.findByRole('link', { name: /Second movie/ })).toBeInTheDocument();
    releaseFirst();
    await waitFor(() => expect(requested).toEqual([1, 2]));
    await waitFor(() => expect(screen.queryByText('First movie')).not.toBeInTheDocument());
    expect(screen.getByRole('link', { name: /Second movie/ })).toBeInTheDocument();
  });

  it.each([null, undefined, 0, -1, 'abc', '1.5'])(
    'sends no recommendations request for an invalid movie id (%s)',
    (movieId) => {
      // arrange
      const requests: string[] = [];
      server.use(
        http.get('*/api/movies/:id/recommendations', ({ request }) => {
          requests.push(request.url);
          return HttpResponse.json([]);
        }),
      );

      // act
      const { container } = renderRelated(movieId);

      // assert
      expect(container).toBeEmptyDOMElement();
      expect(requests).toEqual([]);
    },
  );
});

describe('RelatedMovies shared-card feedback integration', () => {
  it('mounts the shared feedback actions on the current recommendation card', async () => {
    // arrange
    server.use(recommendationsHandler(relatedMovies as RecommendedMovie[]));

    // act
    renderRelated(1);

    // assert
    expect(await screen.findByRole('button', { name: 'Mark Watched, Arrival' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Not Interested, Arrival' })).toBeEnabled();
  });

  it('refreshes membership, flags and reasons after a successful action', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/movies/:id/recommendations', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json([
            recommendation({
              id: 2,
              title: 'Arrival',
              reasons: [{ kind: 'structural', text: 'Same director' }],
            }),
            recommendation({ id: 3, title: 'Prisoners' }),
          ]);
        }
        return HttpResponse.json([
          recommendation({
            id: 2,
            title: 'Arrival',
            isWatched: true,
            reasons: [
              { kind: 'personal', text: 'Now watched' },
              { kind: 'structural', text: 'Same director' },
            ],
          }),
          recommendation({ id: 4, title: 'Newcomer' }),
        ]);
      }),
    );
    renderRelated(1);
    await screen.findByRole('link', { name: /Arrival/ });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Arrival' }));

    // assert
    expect(await screen.findByRole('link', { name: /Newcomer/ })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Prisoners/ })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Watched, Arrival' })).toBeDisabled();
    const arrivalCard = screen
      .getByRole('link', { name: /Arrival/ })
      .closest('article') as HTMLElement;
    expect(reasonTexts(arrivalCard)).toEqual(['Now watched', 'Same director']);
    expect(calls).toBe(2);
  });

  it('stales other recommendation queries for their next fetch after success', async () => {
    // arrange
    server.use(recommendationsHandler([recommendation({ id: 3, title: 'Prisoners' })]));
    const { queryClient } = renderRelated(1);
    queryClient.setQueryData(relatedMoviesQueryKey(2), []);
    expect(queryClient.getQueryState(relatedMoviesQueryKey(2))?.isInvalidated).toBe(false);
    await screen.findByRole('link', { name: /Prisoners/ });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Prisoners' }));

    // assert
    await waitFor(() =>
      expect(queryClient.getQueryState(relatedMoviesQueryKey(2))?.isInvalidated).toBe(true),
    );
  });

  it('renders a refresh-only error and retries without another POST', async () => {
    // arrange
    let posts = 0;
    let fetches = 0;
    let refreshFails = true;
    server.use(
      http.post('*/api/movies/:id/watched', () => {
        posts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
      http.get('*/api/movies/:id/recommendations', () => {
        fetches += 1;
        if (fetches === 1 || !refreshFails) {
          return HttpResponse.json([recommendation({ id: 3, title: 'Prisoners' })]);
        }
        return HttpResponse.json(problemDetails(500), { status: 500 });
      }),
    );
    renderRelated(1);
    await screen.findByRole('link', { name: /Prisoners/ });

    // act
    fireEvent.click(screen.getByRole('button', { name: 'Mark Watched, Prisoners' }));

    // assert
    expect(await screen.findByText(MOVIE_FEEDBACK_REFRESH_ERROR)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Watched, Prisoners' })).toBeVisible();

    refreshFails = false;
    fireEvent.click(screen.getByRole('button', { name: 'Retry refresh, Prisoners' }));

    await waitFor(() =>
      expect(screen.queryByText(MOVIE_FEEDBACK_REFRESH_ERROR)).not.toBeInTheDocument(),
    );
    expect(screen.getByRole('button', { name: 'Watched, Prisoners' })).toBeVisible();
    expect(posts).toBe(1);
    expect(fetches).toBe(3);
  });

  it('renders every reason verbatim in API order including duplicates and HTML-looking text', async () => {
    // arrange
    server.use(
      recommendationsHandler([
        recommendation({
          id: 2,
          title: 'Arrival',
          reasons: [
            { kind: 'structural', text: 'Same director' },
            { kind: 'structural', text: 'Same director' },
            { kind: 'structural', text: '<b>Bold</b> & <script>alert(1)</script>' },
          ],
        }),
      ]),
    );

    // act
    renderRelated(1);

    // assert
    const card = (await screen.findByRole('link', { name: /Arrival/ })).closest(
      'article',
    ) as HTMLElement;
    expect(reasonTexts(card)).toEqual([
      'Same director',
      'Same director',
      '<b>Bold</b> & <script>alert(1)</script>',
    ]);
    expect(card.querySelector('b')).toBeNull();
    expect(card.querySelector('script')).toBeNull();
  });

  it('renders no chips when reasons are absent', async () => {
    // arrange
    const noReasons = recommendation({ id: 2, title: 'Arrival' });
    delete (noReasons as { reasons?: RecommendedMovie['reasons'] }).reasons;
    server.use(recommendationsHandler([noReasons]));

    // act
    renderRelated(1);

    // assert
    const card = (await screen.findByRole('link', { name: /Arrival/ })).closest(
      'article',
    ) as HTMLElement;
    expect(reasonTexts(card)).toEqual([]);
    expect(card.querySelector('.related-movie-reasons')).toBeNull();
  });

  it('moves focus to the section heading when refresh removes the focused card', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/movies/:id/recommendations', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json([
            recommendation({ id: 2, title: 'Arrival' }),
            recommendation({ id: 3, title: 'Prisoners' }),
          ]);
        }
        return HttpResponse.json([recommendation({ id: 3, title: 'Prisoners' })]);
      }),
    );
    renderRelated(1);
    await screen.findByRole('link', { name: /Arrival/ });
    const watched = screen.getByRole('button', { name: 'Mark Watched, Arrival' });
    watched.focus();
    expect(document.activeElement).toBe(watched);

    // act
    fireEvent.click(watched);

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /Arrival/ })).not.toBeInTheDocument(),
    );
    const heading = screen.getByRole('heading', { name: 'Related movies' });
    await waitFor(() => expect(heading).toHaveFocus());
    expect(heading).toHaveAttribute('tabindex', '-1');
    expect(screen.getByRole('link', { name: /Prisoners/ })).toBeInTheDocument();
  });
});
