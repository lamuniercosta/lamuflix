import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { browseMovies } from '../mocks/fixtures';
import { server } from '../test/server';
import LibraryView from './LibraryView';

function Wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

beforeEach(() => {
  window.history.replaceState({}, '', '/');
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
  vi.restoreAllMocks();
});

describe('LibraryView pagination scroll', () => {
  it('scrolls to the top when the page changes', async () => {
    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ ...browseMovies, totalCount: 45 })),
    );
    const scrollSpy = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});

    render(<LibraryView />, { wrapper: Wrapper });
    await screen.findByText('Dune');

    fireEvent.click(
      within(screen.getByRole('navigation', { name: /pagination/i })).getByRole('button', {
        name: 'Next',
      }),
    );

    await waitFor(() => {
      expect(scrollSpy).toHaveBeenCalledWith(0, 0);
    });
  });

  it('scrolls to the top when returning to the previous page', async () => {
    window.history.replaceState({}, '', '/?page=2');
    server.use(
      http.get('*/api/movies', () => HttpResponse.json({ ...browseMovies, totalCount: 45 })),
    );
    const scrollSpy = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});

    render(<LibraryView />, { wrapper: Wrapper });
    await screen.findByText('Dune');

    fireEvent.click(
      within(screen.getByRole('navigation', { name: /pagination/i })).getByRole('button', {
        name: 'Previous',
      }),
    );

    await waitFor(() => {
      expect(scrollSpy).toHaveBeenCalledWith(0, 0);
    });
  });
});
