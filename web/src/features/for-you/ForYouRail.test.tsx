import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { forYouRecommendations, problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import ForYouRail from './ForYouRail';
import { forYouQueryKey, type ForYouRecommendation } from './forYouApi';

function recommendation(
  overrides: Partial<ForYouRecommendation> & Pick<ForYouRecommendation, 'id' | 'title'>,
): ForYouRecommendation {
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

function forYouHandler(body: ForYouRecommendation[] | object, status = 200) {
  return http.get('*/api/recommendations/for-you', () => HttpResponse.json(body, { status }));
}

function renderRail(options: { seed?: ForYouRecommendation[]; hostHeading?: boolean } = {}) {
  const { seed, hostHeading = true } = options;
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  if (seed) {
    queryClient.setQueryData(forYouQueryKey, seed);
  }
  const result = render(
    <QueryClientProvider client={queryClient}>
      <div className="library-view">
        {hostHeading ? <h2 className="library-title lamu-section-heading">All movies</h2> : null}
        <ForYouRail />
      </div>
    </QueryClientProvider>,
  );
  return { ...result, queryClient };
}

function reasonTexts(scope: HTMLElement): string[] {
  const card = scope.closest('article') ?? scope;
  return Array.from(card.querySelectorAll('.related-movie-reason')).map(
    (chip) => chip.textContent ?? '',
  );
}

describe('ForYouRail', () => {
  it('renders the heading, carousel and cards in returned order', async () => {
    // arrange
    server.use(forYouHandler(forYouRecommendations));

    // act
    renderRail();

    // assert
    const region = await screen.findByRole('region', { name: 'For You' });
    expect(screen.getByRole('heading', { name: 'For You' })).toBeInTheDocument();
    const items = within(region).getAllByRole('listitem');
    expect(items).toHaveLength(4);
    expect(items.map((item) => item.querySelector('.movie-card-title')?.textContent)).toEqual([
      'The Batman',
      'Sicario',
      'Nightcrawler',
      'Enemy',
    ]);
    expect(screen.queryByText(/0\.91/)).not.toBeInTheDocument();
  });

  it('renders personal and structural reasons verbatim with empty reasons retained', async () => {
    // arrange
    server.use(forYouHandler(forYouRecommendations));

    // act
    renderRail();

    // assert
    const batman = await screen.findByRole('link', { name: /The Batman/ });
    expect(reasonTexts(batman)).toEqual(['Because you watched Dune', 'Same director']);
    const batmanCard = batman.closest('article') as HTMLElement;
    expect(
      Array.from(batmanCard.querySelectorAll('.related-movie-reason')).map(
        (chip) => chip.className,
      ),
    ).toEqual([
      'related-movie-reason related-movie-reason-personal',
      'related-movie-reason related-movie-reason-structural',
    ]);
    const sicario = screen.getByRole('link', { name: /Sicario/ });
    expect(reasonTexts(sicario)).toEqual([]);
    const nightcrawler = screen.getByRole('link', { name: /Nightcrawler/ });
    expect(reasonTexts(nightcrawler)).toEqual(['Same genre']);
    const enemy = screen.getByRole('link', { name: /Enemy/ });
    expect(reasonTexts(enemy)).toEqual(['Matches your Thriller interest']);
    expect(screen.queryByRole('button', { name: 'Same director' })).not.toBeInTheDocument();
  });

  it('renders nullable poster and metadata through unchanged MovieCard fallbacks', async () => {
    // arrange
    server.use(forYouHandler(forYouRecommendations));

    // act
    renderRail();

    // assert
    const enemy = await screen.findByRole('link', { name: /Enemy/ });
    const card = enemy.closest('article') as HTMLElement;
    expect(within(card).getByRole('img', { name: 'Enemy' })).toBeInTheDocument();
    expect(within(card).queryByRole('img', { name: /poster/i })).not.toBeInTheDocument();
    expect(card.querySelector('.movie-card-placeholder')).not.toBeNull();
    expect(within(card).getAllByText('—')).toHaveLength(2);
  });

  it('renders nothing for a successful empty response including the heading', async () => {
    // arrange
    server.use(forYouHandler([]));

    // act
    const { queryClient } = renderRail();
    await waitFor(() => expect(queryClient.getQueryState(forYouQueryKey)?.status).toBe('success'));

    // assert
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Previous' })).not.toBeInTheDocument();
  });

  it('renders nothing while the first request is pending', async () => {
    // arrange
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      http.get('*/api/recommendations/for-you', async () => {
        await gate;
        return HttpResponse.json(forYouRecommendations);
      }),
    );

    // act
    renderRail();

    // assert
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    release();
  });

  it.each([400, 422, 500])('renders nothing for a %i initial failure', async (status) => {
    // arrange
    server.use(forYouHandler(problemDetails(status), status));

    // act
    const { queryClient } = renderRail();
    await waitFor(() => expect(queryClient.getQueryState(forYouQueryKey)?.status).toBe('error'));

    // assert
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('renders nothing for a network failure', async () => {
    // arrange
    server.use(http.get('*/api/recommendations/for-you', () => HttpResponse.error()));

    // act
    const { queryClient } = renderRail();
    await waitFor(() => expect(queryClient.getQueryState(forYouQueryKey)?.status).toBe('error'));

    // assert
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByText(/error/i)).not.toBeInTheDocument();
  });

  it('keeps cached nonempty data visible while a refresh is pending', async () => {
    // arrange
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      http.get('*/api/recommendations/for-you', async () => {
        await gate;
        return HttpResponse.json([recommendation({ id: 30, title: 'Refreshed' })]);
      }),
    );

    // act
    renderRail({ seed: [...forYouRecommendations] });

    // assert
    expect(await screen.findByRole('link', { name: /The Batman/ })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Enemy/ })).toBeInTheDocument();
    release();
    expect(await screen.findByRole('link', { name: /Refreshed/ })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /The Batman/ })).not.toBeInTheDocument();
  });

  it('hides the whole shelf when a refresh returns an empty array', async () => {
    // arrange
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      http.get('*/api/recommendations/for-you', async () => {
        await gate;
        return HttpResponse.json([]);
      }),
    );

    // act
    renderRail({ seed: [...forYouRecommendations] });
    expect(await screen.findByRole('link', { name: /The Batman/ })).toBeInTheDocument();
    release();

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument(),
    );
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
  });

  it('moves focus to the For You heading when refresh removes the focused card', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/for-you', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json([
            recommendation({ id: 20, title: 'Removed' }),
            recommendation({ id: 21, title: 'Kept' }),
          ]);
        }
        return HttpResponse.json([recommendation({ id: 21, title: 'Kept' })]);
      }),
    );
    const { queryClient } = renderRail();
    const removed = await screen.findByRole('link', { name: /Removed/ });
    removed.focus();
    expect(document.activeElement).toBe(removed);

    // act
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: forYouQueryKey });
    });

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /Removed/ })).not.toBeInTheDocument(),
    );
    const heading = screen.getByRole('heading', { name: 'For You' });
    await waitFor(() => expect(heading).toHaveFocus());
    expect(heading).toHaveAttribute('tabindex', '-1');
    expect(screen.getByRole('link', { name: /Kept/ })).toBeInTheDocument();
  });

  it('moves focus to the Library heading when a refresh empties the focused shelf', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/for-you', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json([recommendation({ id: 20, title: 'Removed' })]);
        }
        return HttpResponse.json([]);
      }),
    );
    const { queryClient } = renderRail();
    const removed = await screen.findByRole('link', { name: /Removed/ });
    removed.focus();
    expect(document.activeElement).toBe(removed);

    // act
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: forYouQueryKey });
    });

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /Removed/ })).not.toBeInTheDocument(),
    );
    const libraryHeading = screen.getByRole('heading', { name: 'All movies' });
    await waitFor(() => expect(libraryHeading).toHaveFocus());
    expect(libraryHeading).toHaveAttribute('tabindex', '-1');
  });

  it('leaves focus alone when a refresh empties the shelf and focus is outside it', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/for-you', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json([recommendation({ id: 20, title: 'Removed' })]);
        }
        return HttpResponse.json([]);
      }),
    );
    const { queryClient } = renderRail();
    await screen.findByRole('link', { name: /Removed/ });
    const outside = document.createElement('button');
    outside.textContent = 'Outside';
    document.body.append(outside);
    outside.focus();

    // act
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: forYouQueryKey });
    });

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /Removed/ })).not.toBeInTheDocument(),
    );
    expect(document.activeElement).toBe(outside);
    outside.remove();
  });

  it('no-ops when the Library heading landmark is missing', async () => {
    // arrange
    let calls = 0;
    server.use(
      http.get('*/api/recommendations/for-you', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json([recommendation({ id: 20, title: 'Removed' })]);
        }
        return HttpResponse.json([]);
      }),
    );
    const { queryClient } = renderRail({ hostHeading: false });
    const removed = await screen.findByRole('link', { name: /Removed/ });
    removed.focus();

    // act
    await act(async () => {
      await queryClient.refetchQueries({ queryKey: forYouQueryKey });
    });

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /Removed/ })).not.toBeInTheDocument(),
    );
    expect(document.activeElement).not.toBe(removed);
  });
});
