import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { forYouRecommendations, problemDetails } from '../mocks/fixtures';
import { parseFilterQuery } from '../features/library/filterQuery';
import { forYouQueryKey } from '../features/for-you/forYouApi';
import { server } from '../test/server';
import LibraryView from './LibraryView';
import type { LibraryViewProps } from './LibraryView';

function deferred() {
  let resolve!: () => void;
  const promise = new Promise<void>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}

function renderLibrary(
  props: LibraryViewProps,
  queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } }),
) {
  const renderTree = (next: LibraryViewProps) => (
    <QueryClientProvider client={queryClient}>
      <LibraryView {...next} />
    </QueryClientProvider>
  );
  const result = render(renderTree(props));
  return {
    ...result,
    queryClient,
    rerenderWith: (next: LibraryViewProps) => result.rerender(renderTree(next)),
  };
}

function countForYouRequests() {
  let count = 0;
  server.use(
    http.get('*/api/recommendations/for-you', () => {
      count += 1;
      return HttpResponse.json(forYouRecommendations);
    }),
  );
  return () => count;
}

beforeEach(() => {
  window.history.replaceState({}, '', '/');
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('LibraryView For You integration', () => {
  it('mounts the populated For You rail above the Results region', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );

    // act
    renderLibrary({ query: parseFilterQuery({}) });

    // assert
    const rail = await screen.findByRole('region', { name: 'For You' });
    const heading = screen.getByRole('heading', { name: 'For You' });
    const results = screen.getByRole('region', { name: 'Results' });
    expect(
      heading.compareDocumentPosition(results) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(results.contains(rail)).toBe(false);
    const items = within(rail).getAllByRole('listitem');
    expect(items.map((item) => item.querySelector('.movie-card-title')?.textContent)).toEqual([
      'The Batman',
      'Sicario',
      'Nightcrawler',
      'Enemy',
    ]);
  });

  it('suppresses a cached rail and starts no request while filters are active', async () => {
    // arrange
    const requests = countForYouRequests();
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    queryClient.setQueryData(forYouQueryKey, [...forYouRecommendations]);
    renderLibrary({ query: parseFilterQuery({ text: 'dune' }) }, queryClient);

    // act
    await screen.findByRole('link', { name: 'Dune' });

    // assert
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(requests()).toBe(0);
  });

  it('suppresses a cached rail and starts no request in watchlist-only mode', async () => {
    // arrange
    const requests = countForYouRequests();
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    queryClient.setQueryData(forYouQueryKey, [...forYouRecommendations]);
    renderLibrary({ query: parseFilterQuery({}), inWatchlist: true }, queryClient);

    // act
    await screen.findByRole('region', { name: 'Results' });

    // assert
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(requests()).toBe(0);
  });

  it('does not reveal an in-flight response that completes while filters are active', async () => {
    // arrange
    const gate = deferred();
    let requests = 0;
    let completed = false;
    server.use(
      http.get('*/api/recommendations/for-you', async () => {
        requests += 1;
        await gate.promise;
        completed = true;
        return HttpResponse.json(forYouRecommendations);
      }),
    );
    const view = renderLibrary({ query: parseFilterQuery({}) });
    await waitFor(() => expect(requests).toBe(1));

    // act
    view.rerenderWith({ query: parseFilterQuery({ text: 'dune' }) });
    gate.resolve();

    // assert
    await waitFor(() => expect(completed).toBe(true));
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'For You' })).not.toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Results' })).toBeInTheDocument();
  });

  it('restores the rail when active filters are cleared', async () => {
    // arrange
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    queryClient.setQueryData(forYouQueryKey, [...forYouRecommendations]);
    const view = renderLibrary({ query: parseFilterQuery({ text: 'dune' }) }, queryClient);
    await screen.findByRole('link', { name: 'Dune' });
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();

    // act
    view.rerenderWith({ query: parseFilterQuery({}) });

    // assert
    expect(await screen.findByRole('heading', { name: 'For You' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'For You' })).toBeInTheDocument();
  });

  it('keeps existing plain-click and modified-click card behavior', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );
    const onOpenMovie = vi.fn();
    renderLibrary({ query: parseFilterQuery({}), onOpenMovie });
    const link = await screen.findByRole('link', { name: 'The Batman' });
    expect(link).toHaveAttribute('href', '/movies/7');

    // act
    fireEvent.click(link);
    const plainCalls = onOpenMovie.mock.calls.length;
    const ctrlResult = fireEvent.click(link, { ctrlKey: true });
    const metaResult = fireEvent.click(link, { metaKey: true });

    // assert
    expect(plainCalls).toBe(1);
    expect(ctrlResult).toBe(true);
    expect(metaResult).toBe(true);
    expect(onOpenMovie).toHaveBeenCalledWith(7);
    expect(onOpenMovie).toHaveBeenCalledTimes(1);
  });

  it('leaves the Library grid usable while the rail request is pending', async () => {
    // arrange
    const gate = deferred();
    server.use(
      http.get('*/api/recommendations/for-you', async () => {
        await gate.promise;
        return HttpResponse.json(forYouRecommendations);
      }),
    );

    // act
    renderLibrary({ query: parseFilterQuery({}) });

    // assert
    expect(await screen.findByRole('link', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Arrival' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Results' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
    gate.resolve();
  });

  it('leaves the Library grid usable when the rail request fails', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () =>
        HttpResponse.json(problemDetails(500), { status: 500 }),
      ),
    );

    // act
    const view = renderLibrary({ query: parseFilterQuery({}) });

    // assert
    expect(await screen.findByRole('link', { name: 'Dune' })).toBeInTheDocument();
    await waitFor(() =>
      expect(view.queryClient.getQueryState(forYouQueryKey)?.status).toBe('error'),
    );
    expect(screen.getByRole('link', { name: 'Arrival' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Results' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
  });

  it('moves focus to the Library heading when eligibility unmounts the focused rail', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );
    const view = renderLibrary({ query: parseFilterQuery({}) });
    const link = await screen.findByRole('link', { name: 'The Batman' });
    link.focus();
    expect(document.activeElement).toBe(link);

    // act
    view.rerenderWith({ query: parseFilterQuery({ text: 'dune' }) });

    // assert
    const libraryHeading = document.querySelector<HTMLElement>('h2.library-title');
    expect(libraryHeading).not.toBeNull();
    await waitFor(() => expect(libraryHeading).toHaveFocus());
    expect(libraryHeading).toHaveAttribute('tabindex', '-1');
    expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument();
  });

  it('preserves focus outside the rail when eligibility unmounts it', async () => {
    // arrange
    server.use(
      http.get('*/api/recommendations/for-you', () => HttpResponse.json(forYouRecommendations)),
    );
    const view = renderLibrary({ query: parseFilterQuery({}) });
    await screen.findByRole('link', { name: 'The Batman' });
    const outside = document.createElement('button');
    outside.textContent = 'Outside';
    document.body.append(outside);
    outside.focus();

    // act
    view.rerenderWith({ query: parseFilterQuery({ text: 'dune' }) });

    // assert
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'For You' })).not.toBeInTheDocument(),
    );
    expect(document.activeElement).toBe(outside);
    outside.remove();
  });
});
