import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/server';
import { browseMovies } from '../../mocks/fixtures';
import LibraryView from './LibraryView';

describe('LibraryView host', () => {
  it('renders movie titles and count from the generated client', async () => {
    render(<LibraryView />);

    expect(await screen.findByText('2 movies')).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
  });

  it('requests the canonical defaults on an empty URL', async () => {
    let observed = '';
    server.use(
      http.get('*/api/movies', ({ request }) => {
        observed = new URL(request.url).search.slice(1);
        return HttpResponse.json(browseMovies);
      }),
    );

    render(<LibraryView />);

    await screen.findByText('Dune');
    expect(observed).toBe('sort=Title&direction=Ascending&page=1&pageSize=20');
  });

  it('requests the canonical query for a non-empty filter state', async () => {
    window.history.replaceState({}, '', '/?text=dune&genreIds=1');

    let observed = '';
    server.use(
      http.get('*/api/movies', ({ request }) => {
        observed = new URL(request.url).search.slice(1);
        return HttpResponse.json(browseMovies);
      }),
    );

    render(<LibraryView />);

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

    render(<LibraryView />);

    expect(document.querySelector('ul[aria-busy="true"]')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();

    await screen.findByText('Dune');
  });

  it('retains old titles and mounted controls during a pending refetch', async () => {
    render(<LibraryView />);

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

    render(<LibraryView />);

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
    server.use(
      http.get('*/api/movies', () => {
        requestCount += 1;
        return HttpResponse.json(browseMovies);
      }),
    );

    render(<LibraryView />);

    await screen.findByText('Dune');
    const baseline = requestCount;

    fireEvent.click(screen.getByLabelText(/^Pending$/i));

    await waitFor(() => {
      expect(requestCount).toBe(baseline + 1);
    });
  });
});
