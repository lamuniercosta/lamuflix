import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it } from 'vitest';
import { problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import { __resetMovieFeedbackStore } from '../../library/movieFeedbackStore';
import { __resetWatchlistStore } from '../../library/watchlistStore';
import ForYouPage from './ForYouPage';
import OutsideLibrarySection from './OutsideLibrarySection';
import type { OutsideLibraryCard } from './outsideLibraryApi';

const card: OutsideLibraryCard = {
  tmdbId: 945961,
  title: 'Outside Library Movie',
  releaseYear: 1995,
  posterPath: '/outside-library.jpg',
  reason: 'Recommended for Heat and Collateral',
};

function renderWithQuery(children: ReactNode) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>);
}

function deferred() {
  let release!: (body: OutsideLibraryCard[]) => void;
  const promise = new Promise<OutsideLibraryCard[]>((resolve) => {
    release = resolve;
  });
  return { promise, release };
}

function heading() {
  return screen.queryByRole('heading', { level: 2, name: 'Not in your library yet' });
}

describe('OutsideLibrarySection', () => {
  beforeEach(() => {
    __resetMovieFeedbackStore();
    __resetWatchlistStore();
  });

  it('renders a distinct read-only card with poster, year, plain-text copy, safe link and credit', async () => {
    // arrange
    server.use(http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([card])));

    // act
    renderWithQuery(<OutsideLibrarySection />);
    const section = await screen.findByRole('region', { name: 'Not in your library yet' });

    // assert
    expect(heading()).toBeInTheDocument();
    const item = within(section).getByRole('listitem');
    expect(within(item).getByRole('heading', { level: 3 }).textContent).toBe(card.title);
    expect(within(item).getByText(card.reason)).toBeInTheDocument();
    expect(within(item).getByText('1995')).toBeInTheDocument();
    expect(within(item).getByRole('img')).toHaveAttribute(
      'src',
      'https://image.tmdb.org/t/p/w500/outside-library.jpg',
    );
    const link = within(item).getByRole('link', {
      name: 'View Outside Library Movie on TMDB (opens in a new tab)',
    });
    expect(link).toHaveAttribute('href', 'https://www.themoviedb.org/movie/945961');
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    expect(within(item).queryByRole('button')).not.toBeInTheDocument();
    expect(within(item).getAllByRole('link')).toEqual([link]);
    expect(item.innerHTML).not.toContain('/movies/');
    expect(
      screen.getByText(/not endorsed, certified, or otherwise approved by TMDB/),
    ).toBeVisible();
    expect(screen.getByRole('link', { name: 'TMDB API terms of use' })).toHaveAttribute(
      'href',
      'https://www.themoviedb.org/api-terms-of-use',
    );
  });

  it('renders nullable metadata with a poster fallback and markup-bearing text as text', async () => {
    // arrange
    const hostile: OutsideLibraryCard = {
      tmdbId: 12,
      title: '<img src=x onerror=alert(1)>Dune',
      releaseYear: null,
      posterPath: '/../secret.jpg',
      reason: '<b>Recommended</b> for Dune and Arrival',
    };
    server.use(
      http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([hostile])),
    );

    // act
    renderWithQuery(<OutsideLibrarySection />);
    const item = await screen.findByRole('listitem');

    // assert
    expect(within(item).getByRole('heading', { level: 3 }).textContent).toBe(hostile.title);
    expect(within(item).getByText(hostile.reason)).toBeInTheDocument();
    expect(within(item).queryByRole('img')).not.toBeInTheDocument();
    expect(within(item).getByText('Poster unavailable')).toBeInTheDocument();
    expect(item.querySelector('img, b')).toBeNull();
  });

  it.each(['Tmdb Enabled false', 'blank Tmdb ApiKey'])(
    'stays hidden for the %s API 200 [] response',
    async () => {
      // arrange
      server.use(http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([])));

      // act
      renderWithQuery(<OutsideLibrarySection />);
      await waitFor(() => expect(screen.queryByRole('listitem')).not.toBeInTheDocument());

      // assert
      expect(heading()).not.toBeInTheDocument();
    },
  );

  it('stays hidden while the independent request is loading', async () => {
    // arrange
    const gate = deferred();
    server.use(
      http.get('*/api/recommendations/not-in-library', async () =>
        HttpResponse.json(await gate.promise),
      ),
    );

    // act
    renderWithQuery(<OutsideLibrarySection />);

    // assert
    expect(heading()).not.toBeInTheDocument();
    gate.release([card]);
    expect(await screen.findByRole('region', { name: 'Not in your library yet' })).toBeVisible();
  });

  it('stays hidden when the independent request fails', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/not-in-library', () =>
        HttpResponse.json(problemDetails(503), { status: 503 }),
      ),
    );

    // act
    renderWithQuery(<OutsideLibrarySection />);
    await waitFor(() => expect(screen.queryByRole('listitem')).not.toBeInTheDocument());

    // assert
    expect(heading()).not.toBeInTheDocument();
  });
});

describe('ForYouPage with discovery', () => {
  beforeEach(() => {
    __resetMovieFeedbackStore();
    __resetWatchlistStore();
  });

  it.each(['Tmdb Enabled false', 'blank Tmdb ApiKey'])(
    'keeps the library grid usable with the %s empty response',
    async () => {
      // arrange
      server.use(
        http.get('*/api/recommendations/for-you', () =>
          HttpResponse.json({
            items: [
              {
                id: 7,
                title: 'The Batman',
                posterUrl: null,
                releaseYear: 2022,
                runtime: 176,
                imdbRating: 7.8,
                enrichmentStatus: 'Enriched',
                isInWatchlist: false,
                isWatched: false,
                isNotInterested: false,
                review: 'none',
                score: 0.91,
                evidence: [],
              },
            ],
            hasMore: false,
            hasSeeds: true,
          }),
        ),
        http.get('*/api/recommendations/not-in-library', () => HttpResponse.json([])),
      );

      // act
      renderWithQuery(<ForYouPage />);

      // assert
      expect(await screen.findByText('The Batman')).toBeVisible();
      expect(heading()).not.toBeInTheDocument();
    },
  );

  it('shows the section below a usable library grid once discovery resolves', async () => {
    // arrange
    const gate = deferred();
    server.use(
      http.get('*/api/recommendations/for-you', () =>
        HttpResponse.json({
          items: [
            {
              id: 7,
              title: 'The Batman',
              posterUrl: null,
              releaseYear: 2022,
              runtime: 176,
              imdbRating: 7.8,
              enrichmentStatus: 'Enriched',
              isInWatchlist: false,
              isWatched: false,
              isNotInterested: false,
              review: 'none',
              score: 0.91,
              evidence: [],
            },
          ],
          hasMore: false,
          hasSeeds: true,
        }),
      ),
      http.get('*/api/recommendations/not-in-library', async () =>
        HttpResponse.json(await gate.promise),
      ),
    );

    // act
    renderWithQuery(<ForYouPage />);
    expect(await screen.findByText('The Batman')).toBeVisible();
    expect(heading()).not.toBeInTheDocument();
    gate.release([card]);

    // assert
    expect(await screen.findByRole('region', { name: 'Not in your library yet' })).toBeVisible();
    expect(heading()).toBeInTheDocument();
  });
});
