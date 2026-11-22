import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { forYouRecommendations, problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import { __resetMovieFeedbackStore } from '../../library/movieFeedbackStore';
import { __resetWatchlistStore } from '../../library/watchlistStore';
import ForYouPage, { type ForYouPageProps } from './ForYouPage';

function renderPage(props: ForYouPageProps = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ForYouPage {...props} />
    </QueryClientProvider>,
  );
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
