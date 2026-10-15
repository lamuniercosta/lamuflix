import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/server';
import { browseMovies } from '../../mocks/fixtures';
import LibraryView from './LibraryView';

function renderLibraryView() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(<LibraryView queryClient={queryClient} />);
}

function countMoviesRequests(handler: (request: Request) => void) {
  server.use(
    http.get('*/api/movies', ({ request }) => {
      handler(request);
      return HttpResponse.json(browseMovies);
    }),
  );
}

describe('LibraryView host', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/');
  });

  it('renders movie titles and count from the generated client', async () => {
    renderLibraryView();

    expect(await screen.findByText('2 movies')).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
  });

  it('requests the canonical defaults on an empty URL', async () => {
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

    expect(
      document.querySelector('[aria-live="polite"] p[aria-hidden="true"]'),
    ).toBeInTheDocument();
    expect(screen.queryByText(/^\d+ movies?$/)).not.toBeInTheDocument();
    expect(document.querySelector('ul[aria-busy="true"]')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();

    await screen.findByText('Dune');
    expect(screen.getByText('2 movies')).toBeInTheDocument();
    expect(
      document.querySelector('[aria-live="polite"] p[aria-hidden="true"]'),
    ).not.toBeInTheDocument();
  });

  it('retains old titles and mounted controls during a pending refetch', async () => {
    renderLibraryView();

    await screen.findByText('Dune');

    server.use(
      http.get('*/api/movies', async () => {
        await new Promise((resolve) => setTimeout(resolve, 200));
        return HttpResponse.json(browseMovies);
      }),
    );

    window.history.replaceState({}, '', '/?text=dune');
    window.dispatchEvent(new PopStateEvent('popstate'));

    expect(screen.getByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();

    await waitFor(() => screen.getByText('Dune'));
  });

  it('uses replaceState instead of pushState on filter interactions', async () => {
    const replaceSpy = vi.spyOn(window.history, 'replaceState');
    const pushSpy = vi.spyOn(window.history, 'pushState');

    renderLibraryView();

    await screen.findByText('Dune');

    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: 'Title (Z-A)' } });

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

  it('cancels a pending text search when navigation lands within the debounce window', async () => {
    const requests: string[] = [];
    countMoviesRequests((request) => {
      requests.push(new URL(request.url).search.slice(1));
    });

    renderLibraryView();
    await screen.findByText('Dune');

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'stale' } });
    window.history.replaceState({}, '', '/?genreIds=1');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 400));
    });

    expect(window.location.search).toBe('?genreIds=1');
    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('');
    expect(requests.every((query) => !query.includes('text=stale'))).toBe(true);
  });

  it('shows the empty-result copy and a zero count when no movies match', async () => {
    server.use(
      http.get('*/api/movies', () =>
        HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      ),
    );

    renderLibraryView();

    expect(await screen.findByText('No movies match these filters.')).toBeInTheDocument();
    expect(screen.getByText('0 movies')).toBeInTheDocument();
  });

  it('shows the refresh alert without the empty copy on a first-load failure', async () => {
    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ title: 'Error' }, { status: 500 })),
    );

    renderLibraryView();

    expect(await screen.findByRole('alert')).toHaveTextContent('Movies could not be refreshed.');
    expect(screen.queryByText('No movies match these filters.')).not.toBeInTheDocument();
  });

  it('keeps prior rows when a refetch fails', async () => {
    renderLibraryView();
    await screen.findByText('Dune');

    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ title: 'Error' }, { status: 500 })),
    );

    fireEvent.click(screen.getByLabelText(/^Pending$/i));

    expect(await screen.findByRole('alert')).toHaveTextContent('Movies could not be refreshed.');
    expect(screen.getByText('Dune')).toBeInTheDocument();
  });
});
