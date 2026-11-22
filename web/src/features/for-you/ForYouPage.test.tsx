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

/**
 * Serves the DEV-541 shape for a ranked candidate list: the endpoint strips the
 * wire `exclude` set first, then cuts `take` items at `offset`, and reports
 * `hasMore` for the remaining pool. Requests are recorded in call order.
 */
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

function renderedTitles(): string[] {
  const list = screen.queryByRole('list', { name: 'Recommended movies' });
  if (!list) {
    return [];
  }
  return within(list)
    .getAllByRole('listitem')
    .map((item) => item.querySelector('.movie-card-title')?.textContent ?? '');
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

function recommendationCards(list: HTMLElement): HTMLElement[] {
  return Array.from(list.querySelectorAll<HTMLElement>(':scope > [role="listitem"]'));
}

describe('ForYouPage', () => {
  beforeEach(() => {
    __resetMovieFeedbackStore();
    __resetWatchlistStore();
  });

  it('renders the recommendations as a grid of cards under the For You heading', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );

    // act
    renderPage();

    // assert
    expect(screen.getByRole('heading', { level: 1, name: 'For You' })).toBeInTheDocument();
    const list = await screen.findByRole('list', { name: 'Recommended movies' });
    const items = recommendationCards(list);
    expect(items).toHaveLength(4);
    expect(items.map((item) => item.querySelector('.movie-card-title')?.textContent)).toEqual([
      'The Batman',
      'Sicario',
      'Nightcrawler',
      'Enemy',
    ]);
  });

  it('shows the seed line first and then the capped evidence list on a For You card', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );
    const onOpenMovie = vi.fn();

    // act
    renderPage({ onOpenMovie });
    const list = await screen.findByRole('list', { name: 'Recommended movies' });
    const [first] = recommendationCards(list);

    // assert
    const seedLink = within(first).getByRole('link', { name: 'Open Dune' });
    expect(seedLink).toHaveTextContent('Dune');
    expect(seedLink.closest('p')).toHaveTextContent('Because you watched Dune');
    expect(seedLink).toHaveAttribute('href', '/movies/2');
    const evidenceList = first.querySelector('.movie-card-evidence');
    expect(evidenceList).not.toBeNull();
    expect(
      seedLink.compareDocumentPosition(evidenceList as Node) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(
      Array.from(evidenceList!.querySelectorAll('li')).map((item) => item.textContent),
    ).toEqual([
      'Same director: Denis Villeneuve',
      'Shared cast: Name One, Name Two, Name Three +2',
    ]);

    // act
    fireEvent.click(seedLink);

    // assert
    expect(onOpenMovie).toHaveBeenLastCalledWith(2);

    // act
    fireEvent.click(within(first).getByRole('link', { name: 'The Batman' }));

    // assert
    expect(onOpenMovie).toHaveBeenLastCalledWith(7);
  });

  it('shows the empty-state text for a successful empty response', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () =>
        HttpResponse.json({ items: [], hasMore: false }),
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
    expect(screen.queryByRole('list', { name: 'Recommended movies' })).not.toBeInTheDocument();
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
    expect(await screen.findByRole('list', { name: 'Recommended movies' })).toBeInTheDocument();
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
