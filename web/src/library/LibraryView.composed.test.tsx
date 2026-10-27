import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../App';
import { browseMovies } from '../mocks/fixtures';
import { server } from '../test/server';
import LibraryErrorBoundary from './LibraryErrorBoundary';
import LibraryView from './LibraryView';

function Wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

function renderLibraryView() {
  return render(<LibraryView />, { wrapper: Wrapper });
}

function renderLibraryViewWithBoundary() {
  return render(
    <LibraryErrorBoundary>
      <LibraryView />
    </LibraryErrorBoundary>,
    { wrapper: Wrapper },
  );
}

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<App />, {
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    ),
  });
}

function observeMoviesRequests(seen: string[]) {
  server.use(
    http.get('*/api/movies', ({ request }) => {
      seen.push(new URL(request.url).search.slice(1));
      return HttpResponse.json(browseMovies);
    }),
  );
}

function countMoviesRequests(handler: (request: Request) => void) {
  server.use(
    http.get('*/api/movies', ({ request }) => {
      handler(request);
      return HttpResponse.json(browseMovies);
    }),
  );
}

function ratingAwareMovies(request: Request) {
  const min = new URL(request.url).searchParams.get('minRating');
  const max = new URL(request.url).searchParams.get('maxRating');
  const items = browseMovies.items.filter((item) => {
    if (min === null && max === null) {
      return true;
    }
    if (item.imdbRating == null) {
      return false;
    }
    if (min !== null && min !== '' && item.imdbRating < Number(min)) {
      return false;
    }
    if (max !== null && max !== '' && item.imdbRating > Number(max)) {
      return false;
    }
    return true;
  });
  return HttpResponse.json({ items, totalCount: items.length });
}

beforeEach(() => {
  window.history.replaceState({}, '', '/');
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('LibraryView composition', () => {
  it('renders the filter panel, the poster grid, and pagination', async () => {
    renderLibraryView();

    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Arrival' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
    expect(
      within(screen.getByRole('region', { name: 'Results' })).getAllByRole('article'),
    ).toHaveLength(2);
    expect(screen.getByRole('navigation', { name: /pagination/i })).toBeInTheDocument();
  });

  it('requests the standing query on an empty URL', async () => {
    let observed = '';
    countMoviesRequests((request) => {
      observed = new URL(request.url).search.slice(1);
    });

    renderLibraryView();

    await screen.findByText('Dune');
    expect(observed).toBe('sort=Title&direction=Ascending&page=1&pageSize=20');
  });

  it('requests the canonical query for a non-empty filter state', async () => {
    window.history.replaceState({}, '', '/?text=dune&genreIds=1');

    let observed = '';
    countMoviesRequests((request) => {
      observed = new URL(request.url).search.slice(1);
    });

    renderLibraryView();

    await screen.findByText('Dune');
    expect(observed).toBe('text=dune&genreIds=1&sort=Title&direction=Ascending&page=1&pageSize=20');
  });

  it('shows loading frames and stable controls before movie data resolves', async () => {
    server.use(
      http.get('*/api/movies', async () => {
        await new Promise((resolve) => setTimeout(resolve, 200));
        return HttpResponse.json(browseMovies);
      }),
    );

    renderLibraryView();

    expect(document.querySelector('[aria-busy="true"]')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();

    await screen.findByText('Dune');
    expect(document.querySelector('[aria-busy="true"]')).not.toBeInTheDocument();
  });

  it('uses replaceState instead of pushState on filter interactions', async () => {
    const replaceSpy = vi.spyOn(window.history, 'replaceState');
    const pushSpy = vi.spyOn(window.history, 'pushState');

    renderLibraryView();

    await screen.findByText('Dune');

    fireEvent.click(screen.getByRole('button', { name: /^Filters/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Year' }));

    await waitFor(() => {
      expect(replaceSpy).toHaveBeenCalled();
    });
    expect(pushSpy).not.toHaveBeenCalled();

    replaceSpy.mockRestore();
    pushSpy.mockRestore();
  });

  it('fetches once per committed effective query change', async () => {
    let requestCount = 0;
    countMoviesRequests(() => {
      requestCount += 1;
    });

    renderLibraryView();

    await screen.findByText('Dune');
    const baseline = requestCount;

    fireEvent.click(screen.getByLabelText(/^Pending$/i));

    await waitFor(() => {
      expect(requestCount).toBe(baseline + 1);
    });
  });

  it('does not refetch when the URL gains explicit transport defaults', async () => {
    let requestCount = 0;
    countMoviesRequests(() => {
      requestCount += 1;
    });

    renderLibraryView();
    await screen.findByText('Dune');
    expect(requestCount).toBe(1);

    window.history.replaceState({}, '', '/?sort=Title&direction=Ascending&page=1&pageSize=20');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 50));
    });

    expect(requestCount).toBe(1);
  });

  it('fetches exactly once for a committed debounced text search', async () => {
    let requestCount = 0;
    countMoviesRequests(() => {
      requestCount += 1;
    });

    renderLibraryView();
    await screen.findByText('Dune');
    const baseline = requestCount;

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'dune' } });

    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 100));
    });
    expect(requestCount).toBe(baseline);

    await waitFor(() => {
      expect(requestCount).toBe(baseline + 1);
    });

    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 100));
    });
    expect(requestCount).toBe(baseline + 1);
  });

  it('resets page from the URL and requests page 1 after a filter interaction', async () => {
    window.history.replaceState({}, '', '/?text=dune&page=3');

    let observed = '';
    countMoviesRequests((request) => {
      observed = new URL(request.url).search.slice(1);
    });

    renderLibraryView();
    await screen.findByText('Dune');
    expect(observed).toBe('text=dune&sort=Title&direction=Ascending&page=3&pageSize=20');

    fireEvent.click(screen.getByLabelText(/^Pending$/i));

    await waitFor(() => {
      expect(observed).toBe(
        'text=dune&statuses=Pending&sort=Title&direction=Ascending&page=1&pageSize=20',
      );
    });
    expect(window.location.search).toBe('?text=dune&statuses=Pending');
  });

  it('restores the filtered view on popstate navigation', async () => {
    const requests: string[] = [];
    countMoviesRequests((request) => {
      requests.push(new URL(request.url).search.slice(1));
    });

    renderLibraryView();
    await screen.findByText('Dune');
    const baseline = requests.length;

    window.history.replaceState({}, '', '/?text=dune');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await waitFor(() => {
      expect(requests.length).toBe(baseline + 1);
    });
    expect(requests.at(-1)).toBe('text=dune&sort=Title&direction=Ascending&page=1&pageSize=20');
    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('dune');
  });

  it('requests the next page from pagination and syncs the URL', async () => {
    const requests: string[] = [];
    server.use(
      http.get('*/api/movies', ({ request }) => {
        requests.push(new URL(request.url).search.slice(1));
        return HttpResponse.json({ ...browseMovies, totalCount: 45 });
      }),
    );

    renderLibraryView();
    await screen.findByText('Dune');

    fireEvent.click(
      within(screen.getByRole('navigation', { name: /pagination/i })).getByRole('button', {
        name: 'Next',
      }),
    );

    await waitFor(() => {
      expect(requests.at(-1)).toBe('sort=Title&direction=Ascending&page=2&pageSize=20');
    });
    expect(new URLSearchParams(window.location.search).get('page')).toBe('2');
  });

  it('clears the filters from the grid empty state', async () => {
    window.history.replaceState({}, '', '/?text=dune');
    server.use(http.get('*/api/movies', () => HttpResponse.json({ items: [], totalCount: 0 })));

    renderLibraryView();

    expect(await screen.findByText('No titles match these filters.')).toBeInTheDocument();
    const clear = document.querySelector('.movie-grid-empty button');
    expect(clear).not.toBeNull();
    fireEvent.click(clear as HTMLElement);

    await waitFor(() => {
      expect(window.location.search).toBe('');
    });
  });

  it('shows the refresh alert without grid tiles on a first-load failure', async () => {
    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ title: 'Error' }, { status: 500 })),
    );

    renderLibraryView();

    expect(await screen.findByRole('alert')).toHaveTextContent('Movies could not be refreshed.');
    expect(
      within(screen.getByRole('region', { name: 'Results' })).queryByRole('article'),
    ).not.toBeInTheDocument();
  });

  it('keeps prior tiles when a refetch fails', async () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(<LibraryView />, {
      wrapper: ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
      ),
    });
    await screen.findByRole('heading', { name: 'Dune' });

    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ title: 'Error' }, { status: 500 })),
    );

    await act(async () => {
      await client.invalidateQueries({ queryKey: ['browseMovies'] });
    });

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Movies could not be refreshed.');
    });
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
  });

  it('keeps prior tiles without a misleading empty state when a filter change fails', async () => {
    renderLibraryView();
    await screen.findByRole('heading', { name: 'Dune' });

    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ title: 'Error' }, { status: 500 })),
    );

    window.history.replaceState({}, '', '/?text=dune');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Movies could not be refreshed.');
    });
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.queryByText('No titles match these filters.')).not.toBeInTheDocument();
  });

  it('resets to page 1 in the request when the header watchlist toggle engages', async () => {
    window.history.replaceState({}, '', '/?page=3');
    const seen: string[] = [];
    observeMoviesRequests(seen);

    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });

    fireEvent.click(screen.getByRole('button', { name: 'Watchlist' }));

    await waitFor(() => {
      expect(seen.at(-1)).toBe(
        'sort=Title&direction=Ascending&page=1&pageSize=20&InWatchlist=true',
      );
    });
  });

  it('keeps watchlist=true in the URL and request across subsequent filter changes', async () => {
    const seen: string[] = [];
    observeMoviesRequests(seen);

    renderApp();
    await screen.findByRole('heading', { name: 'Dune' });

    fireEvent.click(screen.getByRole('button', { name: 'Watchlist' }));
    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('watchlist')).toBe('true');
    });

    fireEvent.click(screen.getByLabelText(/^Pending$/i));

    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('statuses')).toBe('Pending');
    });
    expect(new URLSearchParams(window.location.search).get('watchlist')).toBe('true');
    expect(seen.at(-1)).toBe(
      'statuses=Pending&sort=Title&direction=Ascending&page=1&pageSize=20&InWatchlist=true',
    );
  });

  it('shows the invalid-filter copy from the error boundary on a 422', async () => {
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.json(
          {
            type: 'about:blank',
            title: 'Invalid filters',
            status: 422,
            detail: 'Invalid filters',
            instance: null,
          },
          { status: 422 },
        ),
      ),
    );

    renderLibraryViewWithBoundary();

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid filters');
  });

  it('drives the visible grid and count from the IMDb rating slider', async () => {
    window.history.replaceState({}, '', '/?text=dune&foo=bar');
    const seen: string[] = [];
    server.use(
      http.get('*/api/movies', ({ request }) => {
        seen.push(new URL(request.url).search.slice(1));
        return ratingAwareMovies(request);
      }),
    );

    renderLibraryView();
    await screen.findByText('Dune');
    expect(seen.at(-1)).toBe('text=dune&sort=Title&direction=Ascending&page=1&pageSize=20');

    fireEvent.click(screen.getByRole('button', { name: /^Filters/ }));
    fireEvent.change(screen.getByLabelText('IMDb rating minimum'), { target: { value: '9' } });

    await waitFor(() => {
      expect(seen.at(-1)).toBe(
        'text=dune&minRating=9&maxRating=10&sort=Title&direction=Ascending&page=1&pageSize=20',
      );
    });
    await waitFor(() => {
      expect(screen.getByText('0 movies')).toBeInTheDocument();
    });
    expect(screen.queryByText('Dune')).not.toBeInTheDocument();
    expect(window.location.search).toBe('?text=dune&minRating=9&maxRating=10&foo=bar');
  });

  it('counts an active rating group once in the filter badge', async () => {
    window.history.replaceState({}, '', '/?minRating=0&maxRating=10');

    renderLibraryView();
    await screen.findByText('Dune');

    const toggle = screen.getByRole('button', { name: /^Filters/ });
    expect(within(toggle).getByText(/1 filter group active/)).toBeInTheDocument();
  });

  it('shows no filter group badge when the rating group is absent', async () => {
    renderLibraryView();
    await screen.findByText('Dune');

    const toggle = screen.getByRole('button', { name: /^Filters/ });
    expect(within(toggle).queryByText(/filter group active/)).not.toBeInTheDocument();
  });

  it('surfaces the invalid filter UI for a malformed rating URL without rewriting it', async () => {
    window.history.replaceState({}, '', '/?minRating=abc&maxRating=5');
    let observed = '';
    server.use(
      http.get('*/api/movies', ({ request }) => {
        observed = new URL(request.url).search.slice(1);
        return HttpResponse.json(
          {
            type: 'about:blank',
            title: 'Invalid filters',
            status: 422,
            detail: 'Invalid filters',
            instance: null,
          },
          { status: 422 },
        );
      }),
    );

    renderLibraryViewWithBoundary();

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid filters');
    expect(observed).toBe(
      'minRating=abc&maxRating=5&sort=Title&direction=Ascending&page=1&pageSize=20',
    );
    expect(window.location.search).toBe('?minRating=abc&maxRating=5');
  });

  it('forwards a partial rating URL without inventing the missing pair', async () => {
    window.history.replaceState({}, '', '/?minRating=7');
    let observed = '';
    server.use(
      http.get('*/api/movies', ({ request }) => {
        observed = new URL(request.url).search.slice(1);
        return HttpResponse.json({ title: 'Invalid filters' }, { status: 422 });
      }),
    );

    renderLibraryViewWithBoundary();

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid filters');
    expect(observed).toBe('minRating=7&sort=Title&direction=Ascending&page=1&pageSize=20');
    expect(window.location.search).toBe('?minRating=7');
  });
});
