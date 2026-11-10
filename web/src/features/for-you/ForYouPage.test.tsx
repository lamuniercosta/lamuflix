import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { forYouRecommendations, problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import { __resetMovieFeedbackStore } from '../../library/movieFeedbackStore';
import { __resetWatchlistStore } from '../../library/watchlistStore';
import ForYouPage from './ForYouPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ForYouPage />
    </QueryClientProvider>,
  );
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
    const items = within(list).getAllByRole('listitem');
    expect(items).toHaveLength(4);
    expect(items.map((item) => item.querySelector('.movie-card-title')?.textContent)).toEqual([
      'The Batman',
      'Sicario',
      'Nightcrawler',
      'Enemy',
    ]);
  });

  it('shows the empty-state text for a successful empty response', async () => {
    // arrange
    server.use(http.get('*/api/recommendations/for-you', () => HttpResponse.json([])));

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
