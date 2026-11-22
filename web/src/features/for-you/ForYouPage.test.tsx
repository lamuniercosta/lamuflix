import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { StrictMode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { forYouRecommendations, problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import MovieCard from '../../library/MovieCard';
import { __resetMovieFeedbackStore } from '../../library/movieFeedbackStore';
import { __resetWatchlistStore } from '../../library/watchlistStore';
import ForYouPage, { type ForYouPageProps } from './ForYouPage';
import { FOR_YOU_TAKE, forYouQueryKey, type ForYouRecommendation } from './forYouApi';
import { MAX_SEED_ROWS, MAX_THEMED_ROW_CARDS } from './forYouGrouping';

function makeQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

function renderPage(props: ForYouPageProps = {}) {
  const queryClient = makeQueryClient();
  return {
    ...render(
      <QueryClientProvider client={queryClient}>
        <ForYouPage {...props} />
      </QueryClientProvider>,
    ),
    queryClient,
  };
}

function renderPageInStrictMode() {
  const queryClient = makeQueryClient();
  return {
    ...render(
      <StrictMode>
        <QueryClientProvider client={queryClient}>
          <ForYouPage />
        </QueryClientProvider>
      </StrictMode>,
    ),
    queryClient,
  };
}

interface ForYouRequest {
  take: number;
  offset: number;
  exclude: number[];
}

interface StubOptions {
  failWhen?: (request: ForYouRequest, index: number) => number | null;
  waitWhen?: (request: ForYouRequest, index: number) => Promise<void> | null;
}

function stubRanking(ranked: ReadonlyArray<ForYouRecommendation>, options: StubOptions = {}) {
  const requests: ForYouRequest[] = [];
  server.use(
    http.get('*/api/recommendations/for-you', async ({ request }) => {
      const url = new URL(request.url);
      const observed: ForYouRequest = {
        take: Number(url.searchParams.get('take') ?? String(FOR_YOU_TAKE)),
        offset: Number(url.searchParams.get('offset') ?? '0'),
        exclude: url.searchParams.getAll('exclude').map(Number),
      };
      requests.push(observed);
      const index = requests.length - 1;
      const wait = options.waitWhen?.(observed, index) ?? null;
      if (wait) {
        await wait;
      }
      const failure = options.failWhen?.(observed, index) ?? null;
      if (failure !== null) {
        return HttpResponse.json(problemDetails(failure), { status: failure });
      }
      const pool = ranked.filter((movie) => !observed.exclude.includes(Number(movie.id)));
      return HttpResponse.json({
        items: pool.slice(observed.offset, observed.offset + observed.take),
        hasMore: observed.offset + observed.take < pool.length,
        hasSeeds: ranked.length > 0,
      });
    }),
  );
  return requests;
}

function makeMovie(id: number, title = `Movie ${id}`): ForYouRecommendation {
  return {
    id,
    title,
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    review: 'none',
    score: 0,
    evidence: [],
  };
}

function range(from: number, to: number): ForYouRecommendation[] {
  return Array.from({ length: to - from + 1 }, (_, index) => makeMovie(from + index));
}

function titles(ids: number[]): string[] {
  return ids.map((id) => `Movie ${id}`);
}

type SeedSignal = 'liked' | 'watched' | 'watchlist';

function seeded(
  id: number,
  seed: { movieId: number; title: string; signal: SeedSignal },
  extra: Partial<ForYouRecommendation> = {},
  title?: string,
): ForYouRecommendation {
  return { ...makeMovie(id, title), seed, ...extra };
}

function seedGroup(
  from: number,
  count: number,
  movieId: number,
  title: string,
): ForYouRecommendation[] {
  return Array.from({ length: count }, (_, index) =>
    seeded(from + index, { movieId, title, signal: 'liked' }),
  );
}

function pageSection(): HTMLElement | null {
  const heading = screen.queryByRole('heading', { level: 1, name: 'For You' });
  return heading?.closest('section') ?? null;
}

function renderedTitles(): string[] {
  const page = pageSection();
  if (!page) {
    return [];
  }
  return Array.from(page.querySelectorAll('.for-you-row .movie-card-title')).map(
    (node) => node.textContent ?? '',
  );
}

function rowLabels(): string[] {
  const page = pageSection();
  if (!page) {
    return [];
  }
  return Array.from(page.querySelectorAll('.for-you-row-heading')).map(
    (node) => node.textContent ?? '',
  );
}

function rowFor(label: string): HTMLElement {
  const heading = screen.getByRole('heading', { level: 2, name: label });
  const row = heading.closest('.for-you-row');
  if (!(row instanceof HTMLElement)) {
    throw new Error(`For You row not found for ${label}`);
  }
  return row;
}

function cardTitles(row: HTMLElement): string[] {
  return Array.from(row.querySelectorAll('.movie-card-title')).map(
    (node) => node.textContent ?? '',
  );
}

async function findGrid(count: number): Promise<void> {
  await waitFor(() => expect(renderedTitles()).toHaveLength(count));
}

function loadMoreButton(): HTMLElement {
  return screen.getByRole('button', { name: 'Load more' });
}

function replaceButton(title: string): HTMLElement {
  return screen.getByRole('button', { name: `Replace ${title}` });
}

function deferred() {
  let resolve!: () => void;
  const promise = new Promise<void>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

function flush(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

describe('ForYouPage', () => {
  beforeEach(() => {
    __resetMovieFeedbackStore();
    __resetWatchlistStore();
  });

  it('renders the recommendations grouped into reason rows under the For You heading', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );

    // act
    renderPage();

    // assert
    expect(screen.getByRole('heading', { level: 1, name: 'For You' })).toBeInTheDocument();
    await waitFor(() => expect(rowLabels()).toEqual(['Because you watched Dune', 'More for you']));
    expect(renderedTitles()).toEqual(['The Batman', 'Enemy', 'Sicario', 'Nightcrawler']);
  });

  it('shows an error alert with a Retry button that refetches', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/for-you', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json(problemDetails(500), { status: 500 });
        }
        return HttpResponse.json(forYouRecommendations);
      }),
    );
    renderPage();

    // act
    const retry = await screen.findByRole('button', { name: 'Retry' });

    // assert
    expect(screen.getByRole('alert')).toHaveTextContent('Could not load recommendations.');
    fireEvent.click(retry);
    await waitFor(() => expect(rowLabels().length).toBeGreaterThan(0));
    expect(calls).toBe(2);
  });
});

describe('ForYouPage load more', () => {
  it('appends the next ranked page under an unchanged exclusion set and hides Load more at exhaustion', async () => {
    // arrange
    const requests = stubRanking(range(1, 22));
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(20);
    expect(renderedTitles()).toEqual(
      titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20]),
    );
    expect(requests.map((request) => request.offset)).toEqual([0, 10]);
    expect(requests.every((request) => request.exclude.length === 0)).toBe(true);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(22);
    expect(requests.map((request) => request.offset)).toEqual([0, 10, 20]);
    expect(renderedTitles().slice(20)).toEqual(titles([21, 22]));
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();
  });

  it('skips already rendered IDs and fills the append from consecutive slices up to take', async () => {
    // arrange
    const requests = stubRanking([...range(1, 10), ...range(1, 10), ...range(11, 13)]);
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(13);
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]));
    expect(requests.map((request) => [request.offset, request.take])).toEqual([
      [0, FOR_YOU_TAKE],
      [10, FOR_YOU_TAKE],
      [20, FOR_YOU_TAKE],
    ]);
  });

  it('removes Load more when the terminal slice only repeats rendered IDs', async () => {
    // arrange
    const requests = stubRanking([...range(1, 10), ...range(1, 10)]);
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument(),
    );
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
    expect(requests.map((request) => request.offset)).toEqual([0, 10]);
  });

  it('keeps the grid and committed paging after a failed append and retries from the same scan', async () => {
    // arrange
    let failNext = true;
    const requests = stubRanking(range(1, 22), {
      failWhen: (request) => {
        if (failNext && request.offset === 10) {
          failNext = false;
          return 500;
        }
        return null;
      },
    });
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await waitFor(() => expect(requests).toHaveLength(2));
    await waitFor(() => expect(loadMoreButton()).toBeEnabled());
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(20);
    expect(requests.map((request) => request.offset)).toEqual([0, 10, 10]);
  });

  it('rolls back a failed multi-slice append and retries from the committed cursor', async () => {
    // arrange
    let failNext = true;
    const requests = stubRanking([...range(1, 10), ...range(1, 10), ...range(11, 12)], {
      failWhen: (request) => {
        if (failNext && request.offset === 20) {
          failNext = false;
          return 500;
        }
        return null;
      },
    });
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await waitFor(() => expect(requests).toHaveLength(3));
    await waitFor(() => expect(loadMoreButton()).toBeEnabled());
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(12);
    expect(requests.map((request) => request.offset)).toEqual([0, 10, 20, 10, 20]);
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]));
  });

  it('keeps a single Load more in flight', async () => {
    // arrange
    const gate = deferred();
    const requests = stubRanking(range(1, 22), {
      waitWhen: (request) => (request.offset === 10 ? gate.promise : null),
    });
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());
    await waitFor(() => expect(requests).toHaveLength(2));
    fireEvent.click(loadMoreButton());
    await flush();

    // assert
    expect(requests).toHaveLength(2);

    // act
    gate.resolve();

    // assert
    await findGrid(20);
  });
});

describe('ForYouPage replacement', () => {
  it('replaces the card in place with the first unseen candidate from an offset-zero scan', async () => {
    // arrange
    const requests = stubRanking(range(1, 22));
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(replaceButton('Movie 3'));

    // assert
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 11'));
    expect(requests[1]).toEqual({ take: FOR_YOU_TAKE, offset: 0, exclude: [3] });
    expect(renderedTitles()).toEqual(titles([1, 2, 11, 4, 5, 6, 7, 8, 9, 10]));

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(20);
    expect(requests[2]).toEqual({ take: FOR_YOU_TAKE, offset: 10, exclude: [3] });
    expect(renderedTitles().slice(10)).toEqual(titles([12, 13, 14, 15, 16, 17, 18, 19, 20, 21]));
  });

  it('scans past an all-rendered slice with the same tentative exclusion and retains the extra row for Load more', async () => {
    // arrange
    const requests = stubRanking([...range(1, 10), ...range(1, 10), ...range(11, 20)]);
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(replaceButton('Movie 3'));

    // assert
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 11'));
    expect(requests.slice(1).map((request) => [request.offset, request.exclude])).toEqual([
      [0, [3]],
      [10, [3]],
    ]);
    expect(renderedTitles()).toEqual(titles([1, 2, 11, 4, 5, 6, 7, 8, 9, 10]));

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(19);
    expect(requests[3]).toEqual({ take: FOR_YOU_TAKE, offset: 20, exclude: [3] });
    expect(renderedTitles().slice(10)).toEqual(titles([12, 13, 14, 15, 16, 17, 18, 19, 20]));
  });

  it('rolls back a rejected replacement, keeps Load more usable and retries the swap', async () => {
    // arrange
    let rejectNext = true;
    const requests = stubRanking(range(1, 22), {
      failWhen: (request) => {
        if (rejectNext && request.exclude.includes(3)) {
          rejectNext = false;
          return 400;
        }
        return null;
      },
    });
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(replaceButton('Movie 3'));

    // assert
    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
    expect(screen.queryByText(/Mock problem for status 400/)).not.toBeInTheDocument();

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(20);
    expect(requests[2]).toEqual({ take: FOR_YOU_TAKE, offset: 10, exclude: [] });

    // act
    fireEvent.click(replaceButton('Movie 3'));

    // assert
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 21'));
    expect(requests.slice(3).map((request) => [request.offset, request.exclude])).toEqual([
      [0, [3]],
      [10, [3]],
    ]);
  });

  it('keeps the original card and shows a non-error status when no unseen candidate remains', async () => {
    // arrange
    const requests = stubRanking(range(1, 10));
    renderPage();
    await findGrid(10);
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();

    // act
    const statusesBefore = screen.getAllByRole('status').length;
    fireEvent.click(replaceButton('Movie 5'));

    // assert
    await waitFor(() => expect(requests).toHaveLength(2));
    await waitFor(() => expect(screen.getAllByRole('status')).toHaveLength(statusesBefore + 1));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
  });

  it('appends after a swap from the replacement scan state instead of the pre-swap cursor', async () => {
    // arrange
    const requests = stubRanking([
      ...range(1, 10),
      ...range(11, 20),
      ...range(1, 10),
      ...range(21, 23),
    ]);
    renderPage();
    await findGrid(10);
    fireEvent.click(loadMoreButton());
    await findGrid(20);

    // act
    fireEvent.click(replaceButton('Movie 3'));

    // assert
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 21'));

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(22);
    const afterSwap = requests.filter((request) => request.exclude.includes(3));
    expect(afterSwap.map((request) => request.offset)).toEqual([0, 10, 20, 30]);
    expect(renderedTitles().slice(20)).toEqual(titles([22, 23]));
  });

  it('serializes Load more and replacement into a single in-flight action', async () => {
    // arrange
    const gate = deferred();
    const requests = stubRanking(range(1, 22), {
      waitWhen: (request) => (request.exclude.includes(3) ? gate.promise : null),
    });
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(replaceButton('Movie 3'));
    await waitFor(() => expect(requests).toHaveLength(2));
    fireEvent.click(loadMoreButton());
    fireEvent.click(replaceButton('Movie 5'));
    await flush();

    // assert
    expect(requests).toHaveLength(2);

    // act
    gate.resolve();

    // assert
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 11'));
    expect(renderedTitles()).toEqual(titles([1, 2, 11, 4, 5, 6, 7, 8, 9, 10]));
  });

  it('keeps the ordered grid and committed cursor after an incidental invalidation', async () => {
    // arrange
    const requests = stubRanking(range(1, 22));
    const view = renderPage();
    await findGrid(10);
    fireEvent.click(replaceButton('Movie 3'));
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 11'));

    // act
    await view.queryClient.invalidateQueries({ queryKey: forYouQueryKey });

    // assert
    expect(renderedTitles()).toEqual(titles([1, 2, 11, 4, 5, 6, 7, 8, 9, 10]));
    expect(replaceButton('Movie 4')).toBeEnabled();

    // act
    fireEvent.click(replaceButton('Movie 4'));

    // assert
    await waitFor(() => expect(renderedTitles()[3]).toBe('Movie 12'));
    expect(
      requests
        .filter((request) => request.exclude.length > 0)
        .map((request) => [request.offset, request.exclude]),
    ).toEqual([
      [0, [3]],
      [0, [3, 4]],
    ]);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(20);
    expect(requests[requests.length - 1]).toEqual({
      take: FOR_YOU_TAKE,
      offset: 10,
      exclude: [3, 4],
    });
  });

  it('starts a fresh visit without prior committed exclusions after a remount', async () => {
    // arrange
    const requests = stubRanking(range(1, 22));
    const first = renderPage();
    await findGrid(10);
    fireEvent.click(replaceButton('Movie 3'));
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 11'));

    // act
    first.unmount();
    renderPage();

    // assert
    await findGrid(10);
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
    expect(requests[requests.length - 1].exclude).toEqual([]);
  });

  it('aborts the visit scan on leave in Strict Mode and ignores the late response', async () => {
    // arrange
    const gate = deferred();
    const recorded: Array<{ url: string; signal: AbortSignal }> = [];
    const realFetch = globalThis.fetch;
    vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
      const request = input instanceof Request ? input : new Request(input);
      recorded.push({ url: request.url, signal: request.signal });
      return realFetch(request);
    });
    const requests = stubRanking([...range(1, 10), ...range(1, 10), ...range(11, 20)], {
      waitWhen: (request) =>
        request.offset === 10 && request.exclude.includes(3) ? gate.promise : null,
    });
    const view = renderPageInStrictMode();
    await findGrid(10);

    // act
    fireEvent.click(replaceButton('Movie 3'));

    // assert
    await waitFor(() =>
      expect(requests.some((request) => request.offset === 10 && request.exclude.includes(3))).toBe(
        true,
      ),
    );
    const pending = recorded.find((entry) => {
      const url = new URL(entry.url);
      return (
        url.searchParams.get('offset') === '10' && url.searchParams.getAll('exclude').includes('3')
      );
    });
    expect(pending).toBeDefined();

    // act
    view.unmount();

    // assert
    await waitFor(() => expect(pending?.signal.aborted).toBe(true));
    const settled = requests.length;
    gate.resolve();
    await flush();
    expect(requests).toHaveLength(settled);

    // act
    renderPage();

    // assert
    await findGrid(10);
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
    expect(requests[requests.length - 1].exclude).toEqual([]);
    vi.restoreAllMocks();
  });

  it('exposes the replace action as a focusable button that does not open or play the movie', async () => {
    // arrange
    const onOpenMovie = vi.fn();
    stubRanking(range(1, 22));
    renderPage({ onOpenMovie });
    await findGrid(10);

    // act
    const replace = replaceButton('Movie 3');
    replace.focus();

    // assert
    expect(replace.tagName).toBe('BUTTON');
    expect(replace).toHaveAttribute('type', 'button');
    expect(replace.closest('a')).toBeNull();
    expect(replace).toHaveFocus();

    // act
    fireEvent.click(replace);

    // assert
    expect(onOpenMovie).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Play Movie 3' })).toBeInTheDocument();
    await waitFor(() => expect(renderedTitles()[2]).toBe('Movie 11'));
  });

  it('renders a shared MovieCard without the optional replace action when the prop is omitted', () => {
    // arrange
    const movie = { ...makeMovie(1, 'Heat'), failureCategory: null, failureMessage: null };

    // act
    render(
      <QueryClientProvider client={makeQueryClient()}>
        <MovieCard movie={movie} />
      </QueryClientProvider>,
    );

    // assert
    expect(screen.getByRole('link', { name: 'Heat' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Play Heat' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Replace / })).not.toBeInTheDocument();
  });
});

describe('ForYouPage More for you controls', () => {
  it('shows Load more and Swap only inside the More for you row', async () => {
    // arrange
    stubRanking([
      seeded(1301, { movieId: 900, title: 'Seed A', signal: 'liked' }),
      seeded(1302, { movieId: 900, title: 'Seed A', signal: 'liked' }),
      ...range(1303, 1312),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toEqual(['Because you liked Seed A', 'More for you']));
    const themed = rowFor('Because you liked Seed A');
    const more = rowFor('More for you');
    expect(within(more).getByRole('button', { name: 'Load more' })).toBeInTheDocument();
    expect(within(themed).queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();
    expect(within(themed).queryByRole('button', { name: /^Replace / })).not.toBeInTheDocument();
    expect(within(themed).getAllByRole('link', { name: /^Movie / })).toHaveLength(2);
    expect(within(more).getAllByRole('button', { name: /^Replace / })).toHaveLength(8);
    expect(screen.getAllByRole('button', { name: 'Load more' })).toHaveLength(1);
  });

  it('renders a card-less More for you region with its heading and Load more until a card arrives', async () => {
    // arrange
    stubRanking([
      ...seedGroup(1401, 6, 900, 'Seed A'),
      ...seedGroup(1407, 4, 901, 'Seed B'),
      ...range(1411, 1412),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() =>
      expect(rowLabels()).toEqual([
        'Because you liked Seed A',
        'Because you liked Seed B',
        'More for you',
      ]),
    );
    const more = rowFor('More for you');
    expect(screen.getAllByRole('region', { name: 'More for you' })).toContain(more);
    expect(
      within(more).getByRole('heading', { level: 2, name: 'More for you' }),
    ).toBeInTheDocument();
    expect(within(more).queryByRole('group', { name: 'More for you' })).not.toBeInTheDocument();
    expect(cardTitles(more)).toEqual([]);
    expect(within(more).queryByRole('button', { name: /^Replace / })).not.toBeInTheDocument();

    // act
    fireEvent.click(within(more).getByRole('button', { name: 'Load more' }));

    // assert
    await findGrid(12);
    const loaded = rowFor('More for you');
    expect(cardTitles(loaded)).toEqual(titles([1411, 1412]));
    expect(within(loaded).getByRole('group', { name: 'More for you' })).toBeInTheDocument();
    expect(within(loaded).getAllByRole('button', { name: /^Replace / })).toHaveLength(2);
  });

  it('omits the More for you section when it has neither cards nor controls', async () => {
    // arrange
    stubRanking([...seedGroup(1501, 6, 900, 'Seed A'), ...seedGroup(1507, 4, 901, 'Seed B')]);

    // act
    renderPage();

    // assert
    await waitFor(() =>
      expect(rowLabels()).toEqual(['Because you liked Seed A', 'Because you liked Seed B']),
    );
    expect(
      screen.queryByRole('heading', { level: 2, name: 'More for you' }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Replace / })).not.toBeInTheDocument();
  });

  it('deduplicates Load more results against the themed rows', async () => {
    // arrange
    const requests = stubRanking([
      ...seedGroup(1601, 6, 900, 'Seed A'),
      ...seedGroup(1607, 4, 901, 'Seed B'),
      ...range(1601, 1602),
      ...range(1611, 1612),
    ]);
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(12);
    expect(cardTitles(rowFor('More for you'))).toEqual(titles([1611, 1612]));
    expect(renderedTitles()).toHaveLength(new Set(renderedTitles()).size);
    expect(requests.map((request) => request.offset)).toEqual([0, 10]);
  });

  it('dispatches one action when Load more and Swap are attempted together', async () => {
    // arrange
    const gate = deferred();
    const requests = stubRanking(range(1, 30), {
      waitWhen: (request) => (request.offset === 10 ? gate.promise : null),
    });
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());
    await waitFor(() => expect(requests).toHaveLength(2));
    fireEvent.click(replaceButton('Movie 2'));
    fireEvent.click(replaceButton('Movie 3'));
    fireEvent.click(screen.getByRole('button', { name: 'Load more' }));
    await flush();

    // assert
    expect(requests).toHaveLength(2);

    // act
    gate.resolve();

    // assert
    await findGrid(20);
    const settled = renderedTitles();
    expect(settled).toHaveLength(new Set(settled).size);
    expect(settled.slice(10)).toEqual(titles(range(11, 20).map((movie) => Number(movie.id))));
  });

  it('ignores an obsolete Load more response after a page reset', async () => {
    // arrange
    const gate = deferred();
    const requests = stubRanking(range(1, 30), {
      waitWhen: (request) => (request.offset === 10 ? gate.promise : null),
    });
    const first = renderPage();
    await findGrid(10);
    fireEvent.click(loadMoreButton());
    await waitFor(() => expect(requests).toHaveLength(2));

    // act
    first.unmount();
    renderPage();
    await findGrid(10);
    gate.resolve();
    await flush();

    // assert
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
  });

  it('keeps themed rows frozen across Load more and Swap', async () => {
    // arrange
    stubRanking([
      seeded(1701, { movieId: 900, title: 'Seed A', signal: 'liked' }),
      seeded(1702, { movieId: 900, title: 'Seed A', signal: 'liked' }),
      ...range(1703, 1730),
    ]);
    renderPage();
    await waitFor(() => expect(rowLabels()).toEqual(['Because you liked Seed A', 'More for you']));
    const themedBefore = cardTitles(rowFor('Because you liked Seed A'));

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await findGrid(20);
    expect(rowLabels()).toEqual(['Because you liked Seed A', 'More for you']);
    expect(cardTitles(rowFor('Because you liked Seed A'))).toEqual(themedBefore);

    // act
    fireEvent.click(replaceButton('Movie 1705'));

    // assert
    await waitFor(() => expect(renderedTitles()).toContain('Movie 1721'));
    expect(renderedTitles()).not.toContain('Movie 1705');
    expect(rowLabels()).toEqual(['Because you liked Seed A', 'More for you']);
    expect(cardTitles(rowFor('Because you liked Seed A'))).toEqual(themedBefore);
  });

  it('keeps cards through empty Load more and empty Swap follow-ups without empty states', async () => {
    // arrange
    const requests = stubRanking([...range(1, 10), ...range(1, 3)]);
    renderPage();
    await findGrid(10);

    // act
    fireEvent.click(loadMoreButton());

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument(),
    );
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
    expect(screen.queryByText(/^No recommendations/)).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Browse the library' })).not.toBeInTheDocument();

    // act
    fireEvent.click(replaceButton('Movie 5'));

    // assert
    await waitFor(() => expect(requests).toHaveLength(4));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.queryByText(/^No recommendations/)).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Browse the library' })).not.toBeInTheDocument();
    expect(renderedTitles()).toEqual(titles([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
    expect(await screen.findByText('No new suggestions to swap in right now.')).toBeInTheDocument();
  });
});

describe('ForYouPage grouping', () => {
  it('builds the wanted row order with series priority, seed rows, watchlist and More for you', async () => {
    // arrange
    const requests = stubRanking([
      seeded(
        101,
        { movieId: 900, title: 'Seed X', signal: 'liked' },
        { releaseYear: 2020, startedCollectionId: 1 },
      ),
      seeded(
        102,
        { movieId: 901, title: 'Seed Y', signal: 'watched' },
        { releaseYear: 1999, startedCollectionId: 1 },
      ),
      seeded(
        103,
        { movieId: 900, title: 'Seed X', signal: 'liked' },
        { releaseYear: 2005, startedCollectionId: 2 },
      ),
      seeded(104, { movieId: 900, title: 'Seed X', signal: 'liked' }),
      seeded(105, { movieId: 900, title: 'Seed X', signal: 'liked' }),
      seeded(106, { movieId: 901, title: 'Seed Y', signal: 'watched' }),
      seeded(107, { movieId: 901, title: 'Seed Y', signal: 'watched' }),
      seeded(108, { movieId: 902, title: 'Seed Z', signal: 'watchlist' }),
      seeded(109, { movieId: 903, title: 'Seed W', signal: 'watchlist' }),
      makeMovie(110),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(5));
    expect(rowLabels()).toEqual([
      'Next in a series you started',
      'Because you liked Seed X',
      'Because you watched Seed Y',
      'Close to your watchlist',
      'More for you',
    ]);
    expect(cardTitles(rowFor('Next in a series you started'))).toEqual(['Movie 102', 'Movie 103']);
    expect(cardTitles(rowFor('Because you liked Seed X'))).toEqual([
      'Movie 101',
      'Movie 104',
      'Movie 105',
    ]);
    expect(cardTitles(rowFor('Because you watched Seed Y'))).toEqual(['Movie 106', 'Movie 107']);
    expect(cardTitles(rowFor('Close to your watchlist'))).toEqual(['Movie 108', 'Movie 109']);
    expect(cardTitles(rowFor('More for you'))).toEqual(['Movie 110']);
    const shown = renderedTitles();
    expect(shown).toHaveLength(new Set(shown).size);
    expect(requests).toHaveLength(1);
  });

  it('omits themed rows that have no candidates', async () => {
    // arrange
    stubRanking([makeMovie(201), makeMovie(202)]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toEqual(['More for you']));
    expect(cardTitles(rowFor('More for you'))).toEqual(['Movie 201', 'Movie 202']);
  });

  it('picks one series candidate per collection by known year, unknown years last, then API rank', async () => {
    // arrange
    stubRanking([
      seeded(
        301,
        { movieId: 910, title: 'Seed A', signal: 'liked' },
        { releaseYear: 2015, startedCollectionId: 7 },
      ),
      seeded(
        302,
        { movieId: 911, title: 'Seed B', signal: 'liked' },
        { releaseYear: 1999, startedCollectionId: 7 },
      ),
      seeded(
        303,
        { movieId: 912, title: 'Seed C', signal: 'liked' },
        { releaseYear: null, startedCollectionId: 7 },
      ),
      seeded(
        320,
        { movieId: 913, title: 'Seed D', signal: 'liked' },
        { releaseYear: 2000, startedCollectionId: 8 },
      ),
      seeded(
        310,
        { movieId: 914, title: 'Seed E', signal: 'liked' },
        { releaseYear: 2000, startedCollectionId: 8 },
      ),
      seeded(
        340,
        { movieId: 915, title: 'Seed F', signal: 'liked' },
        { releaseYear: null, startedCollectionId: 9 },
      ),
      seeded(
        330,
        { movieId: 916, title: 'Seed G', signal: 'liked' },
        { releaseYear: null, startedCollectionId: 9 },
      ),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()[0]).toBe('Next in a series you started'));
    expect(cardTitles(rowFor('Next in a series you started'))).toEqual([
      'Movie 302',
      'Movie 320',
      'Movie 340',
    ]);
    expect(cardTitles(rowFor('More for you'))).toEqual([
      'Movie 301',
      'Movie 303',
      'Movie 310',
      'Movie 330',
    ]);
  });

  it('assigns a duplicate initial movie ID once using its first API occurrence', async () => {
    // arrange
    stubRanking([
      seeded(401, { movieId: 900, title: 'Seed X', signal: 'liked' }, {}, 'First Copy'),
      seeded(401, { movieId: 901, title: 'Seed Y', signal: 'liked' }, {}, 'Second Copy'),
      seeded(402, { movieId: 900, title: 'Seed X', signal: 'liked' }),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toEqual(['Because you liked Seed X']));
    expect(cardTitles(rowFor('Because you liked Seed X'))).toEqual(['First Copy', 'Movie 402']);
    expect(renderedTitles()).not.toContain('Second Copy');
  });

  it('splits seed groups by seed movie ID and reported signal', async () => {
    // arrange
    stubRanking([
      seeded(501, { movieId: 900, title: 'Shared', signal: 'liked' }),
      seeded(502, { movieId: 900, title: 'Shared', signal: 'liked' }),
      seeded(503, { movieId: 900, title: 'Shared', signal: 'watched' }),
      seeded(504, { movieId: 900, title: 'Shared', signal: 'watched' }),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(2));
    expect(rowLabels()).toEqual(['Because you liked Shared', 'Because you watched Shared']);
    expect(cardTitles(rowFor('Because you liked Shared'))).toEqual(['Movie 501', 'Movie 502']);
    expect(cardTitles(rowFor('Because you watched Shared'))).toEqual(['Movie 503', 'Movie 504']);
  });

  it('ranks seed groups by candidate count then first API appearance', async () => {
    // arrange
    stubRanking([
      seeded(601, { movieId: 931, title: 'Seed A', signal: 'liked' }),
      seeded(602, { movieId: 931, title: 'Seed A', signal: 'liked' }),
      seeded(603, { movieId: 932, title: 'Seed B', signal: 'liked' }),
      seeded(604, { movieId: 932, title: 'Seed B', signal: 'liked' }),
      seeded(605, { movieId: 930, title: 'Seed C', signal: 'liked' }),
      seeded(606, { movieId: 930, title: 'Seed C', signal: 'liked' }),
      seeded(607, { movieId: 930, title: 'Seed C', signal: 'liked' }),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(3));
    expect(rowLabels()).toEqual([
      'Because you liked Seed C',
      'Because you liked Seed A',
      'Because you liked Seed B',
    ]);
  });

  it('keeps API order within seed and watchlist rows and folds singletons into More for you', async () => {
    // arrange
    stubRanking([
      seeded(701, { movieId: 940, title: 'Seed X', signal: 'liked' }),
      seeded(702, { movieId: 940, title: 'Seed X', signal: 'liked' }),
      seeded(703, { movieId: 941, title: 'Lonely', signal: 'liked' }),
      makeMovie(704),
      seeded(
        705,
        { movieId: 942, title: 'Solo Series', signal: 'watched' },
        { startedCollectionId: 3 },
      ),
      seeded(706, { movieId: 943, title: 'Watch One', signal: 'watchlist' }),
      seeded(707, { movieId: 944, title: 'Watch Two', signal: 'watchlist' }),
      makeMovie(708),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(3));
    expect(rowLabels()).toEqual([
      'Because you liked Seed X',
      'Close to your watchlist',
      'More for you',
    ]);
    expect(cardTitles(rowFor('Because you liked Seed X'))).toEqual(['Movie 701', 'Movie 702']);
    expect(cardTitles(rowFor('Close to your watchlist'))).toEqual(['Movie 706', 'Movie 707']);
    expect(cardTitles(rowFor('More for you'))).toEqual([
      'Movie 703',
      'Movie 704',
      'Movie 705',
      'Movie 708',
    ]);
  });

  it('moves seed groups beyond four rows into More for you', async () => {
    // arrange
    const ranked: ForYouRecommendation[] = [];
    for (let group = 0; group < 5; group += 1) {
      const movieId = 950 + group;
      ranked.push(
        seeded(800 + group * 2, { movieId, title: `Seed ${movieId}`, signal: 'liked' }),
        seeded(801 + group * 2, { movieId, title: `Seed ${movieId}`, signal: 'liked' }),
      );
    }
    stubRanking(ranked);
    expect(MAX_SEED_ROWS).toBe(4);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(5));
    expect(rowLabels().filter((label) => label.startsWith('Because you liked'))).toEqual([
      'Because you liked Seed 950',
      'Because you liked Seed 951',
      'Because you liked Seed 952',
      'Because you liked Seed 953',
    ]);
    expect(rowLabels()[4]).toBe('More for you');
    expect(cardTitles(rowFor('More for you'))).toEqual(['Movie 808', 'Movie 809']);
  });

  it('sends cards beyond the six-card themed cap into More for you', async () => {
    // arrange
    const ranked: ForYouRecommendation[] = [];
    for (let index = 0; index < 8; index += 1) {
      ranked.push(seeded(801 + index, { movieId: 960, title: 'Seed X', signal: 'liked' }));
    }
    stubRanking(ranked);
    expect(MAX_THEMED_ROW_CARDS).toBe(6);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(2));
    expect(cardTitles(rowFor('Because you liked Seed X'))).toEqual([
      'Movie 801',
      'Movie 802',
      'Movie 803',
      'Movie 804',
      'Movie 805',
      'Movie 806',
    ]);
    expect(cardTitles(rowFor('More for you'))).toEqual(['Movie 807', 'Movie 808']);
  });

  it('caps the watchlist row at six cards and folds its overflow into More for you', async () => {
    // arrange
    const ranked: ForYouRecommendation[] = [
      seeded(830, { movieId: 980, title: 'Seed L', signal: 'liked' }),
    ];
    ranked.push(seeded(831, { movieId: 980, title: 'Seed L', signal: 'liked' }));
    for (let index = 0; index < 7; index += 1) {
      ranked.push(seeded(811 + index, { movieId: 970, title: 'Seed W', signal: 'watchlist' }));
    }
    stubRanking(ranked);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(3));
    expect(cardTitles(rowFor('Close to your watchlist'))).toHaveLength(6);
    expect(cardTitles(rowFor('More for you'))).toEqual(['Movie 817']);
  });
});

describe('ForYouPage row chrome', () => {
  it('labels each populated row as a region with a level-two heading and carousel controls', async () => {
    // arrange
    stubRanking([
      seeded(1001, { movieId: 900, title: 'Seed X', signal: 'liked' }),
      seeded(1002, { movieId: 900, title: 'Seed X', signal: 'liked' }),
      makeMovie(1003),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toHaveLength(2));
    for (const label of rowLabels()) {
      const row = rowFor(label);
      expect(row.tagName).toBe('SECTION');
      expect(screen.getAllByRole('region', { name: label })).toContain(row);
      expect(within(row).getByRole('heading', { level: 2, name: label })).toBeInTheDocument();
      expect(within(row).getByRole('group', { name: label })).toBeInTheDocument();
      expect(within(row).getByRole('button', { name: 'Previous' })).toBeInTheDocument();
      expect(within(row).getByRole('button', { name: 'Next' })).toBeInTheDocument();
    }
  });

  it('moves a row carousel with the arrow keys the same way as the details rail', async () => {
    // arrange
    const layout = { scrollWidth: 1000, clientWidth: 300, scrollLeft: 0 };
    const scrollDeltas: number[] = [];
    const replaceProperty = (property: string, descriptor: PropertyDescriptor) => {
      const saved = Object.getOwnPropertyDescriptor(Element.prototype, property);
      Object.defineProperty(Element.prototype, property, { configurable: true, ...descriptor });
      return () => {
        if (saved) {
          Object.defineProperty(Element.prototype, property, saved);
        } else {
          Reflect.deleteProperty(Element.prototype, property);
        }
      };
    };
    const restores = [
      replaceProperty('scrollWidth', { get: () => layout.scrollWidth }),
      replaceProperty('clientWidth', { get: () => layout.clientWidth }),
      replaceProperty('scrollLeft', {
        get: () => layout.scrollLeft,
        set: (value: number) => {
          layout.scrollLeft = value;
        },
      }),
      replaceProperty('scrollBy', {
        value(this: Element, options: ScrollToOptions = {}) {
          scrollDeltas.push(options.left ?? 0);
        },
      }),
    ];

    try {
      stubRanking([
        seeded(1101, { movieId: 900, title: 'Seed X', signal: 'liked' }),
        seeded(1102, { movieId: 900, title: 'Seed X', signal: 'liked' }),
      ]);
      renderPage();
      await waitFor(() => expect(rowLabels()).toHaveLength(1));
      const carousel = within(rowFor('Because you liked Seed X')).getByRole('group', {
        name: 'Because you liked Seed X',
      });

      // act
      fireEvent.keyDown(carousel, { key: 'ArrowRight' });

      // assert
      expect(scrollDeltas).toEqual([270]);
    } finally {
      for (const restore of restores) {
        restore();
      }
    }
  });

  it('hides the reason line on For You row cards while retaining delivered evidence', async () => {
    // arrange
    const evidence = [{ criterion: 'Genre', text: 'Same genres: Drama', matchedNames: ['Drama'] }];
    stubRanking([
      seeded(1201, { movieId: 900, title: 'Seed X', signal: 'liked' }, { evidence }),
      seeded(1202, { movieId: 900, title: 'Seed X', signal: 'liked' }),
      seeded(1203, { movieId: 901, title: 'Seed Y', signal: 'watchlist' }, { evidence }),
    ]);

    // act
    renderPage();

    // assert
    await waitFor(() => expect(rowLabels()).toEqual(['Because you liked Seed X', 'More for you']));
    expect(pageSection()?.querySelectorAll('.movie-card-seed')).toHaveLength(0);
    expect(screen.queryByRole('link', { name: 'Open Seed X' })).not.toBeInTheDocument();
    const likedCard = rowFor('Because you liked Seed X').querySelector(
      '.movie-card',
    ) as HTMLElement;
    expect(within(likedCard).queryByText(/Because you/)).not.toBeInTheDocument();
    expect(
      Array.from(likedCard.querySelectorAll('.movie-card-evidence > li')).map(
        (node) => node.textContent,
      ),
    ).toEqual(['Same genres: Drama']);
  });
});

describe('ForYouPage empty states', () => {
  it('shows the no-seed state with a library link after a successful empty load', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () =>
        HttpResponse.json({ items: [], hasMore: false, hasSeeds: false }),
      ),
    );

    // act
    renderPage();

    // assert
    expect(
      await screen.findByText(
        'No recommendations yet. Mark movies as watched or liked to get some.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Browse the library' })).toHaveAttribute('href', '/');
    expect(rowLabels()).toEqual([]);
  });

  it('shows the no-strong-match state with a library link when the empty load has seeds', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () =>
        HttpResponse.json({ items: [], hasMore: false, hasSeeds: true }),
      ),
    );

    // act
    renderPage();

    // assert
    expect(
      await screen.findByText(
        'No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Browse the library' })).toHaveAttribute('href', '/');
    expect(screen.queryByText(/^No recommendations yet\./)).not.toBeInTheDocument();
  });

  it('keeps the seeded empty state for a zero-item response that still reports more pages', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () =>
        HttpResponse.json({ items: [], hasMore: true, hasSeeds: true }),
      ),
    );

    // act
    renderPage();

    // assert
    expect(
      await screen.findByText(
        'No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Browse the library' })).toBeInTheDocument();
  });

  it('keeps the loading state until the initial response settles', async () => {
    // arrange
    const gate = deferred();
    stubRanking(range(1, 3), { waitWhen: () => gate.promise });

    // act
    renderPage();

    // assert
    expect(screen.getByRole('status')).toHaveTextContent('Loading recommendations');
    expect(screen.queryByRole('link', { name: 'Browse the library' })).not.toBeInTheDocument();

    // act
    gate.resolve();

    // assert
    await waitFor(() => expect(rowLabels()).toEqual(['More for you']));
  });
});
