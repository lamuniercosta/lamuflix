import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import MovieGrid from './MovieGrid';

const item = {
  id: 1,
  title: 'Heat',
  posterUrl: '/posters/heat.jpg',
  releaseYear: 1995,
  runtime: 170,
  imdbRating: 8.3,
  enrichmentStatus: 'Enriched' as const,
  isInWatchlist: false,
};

function renderWithClient(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('MovieGrid', () => {
  it('renders a responsive grid of cards', () => {
    const { container } = renderWithClient(
      <MovieGrid items={[item, { ...item, id: 2, title: 'Arrival' }]} />,
    );

    expect(container.querySelector('.library-grid')).toBeInTheDocument();
    expect(screen.getAllByRole('article')).toHaveLength(2);
  });

  it('shows the empty-library state with clear filters', async () => {
    const onClear = vi.fn();
    render(<MovieGrid items={[]} totalCount={0} onClearFilters={onClear} />);

    expect(screen.getByText('Your library is empty.')).toBeInTheDocument();
    expect(screen.getByText('Titles added to your library will show up here.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /clear filters/i }));
    expect(onClear).toHaveBeenCalled();
  });

  it('shows the filtered-empty state', () => {
    render(<MovieGrid items={[]} totalCount={0} filtersActive onClearFilters={() => {}} />);

    expect(screen.getByText('No titles match these filters.')).toBeInTheDocument();
  });

  it('shows a past-end action', async () => {
    const onFirst = vi.fn();
    render(
      <MovieGrid items={[]} totalCount={2} page={3} pageSize={24} onGoToFirstPage={onFirst} />,
    );

    expect(screen.getByText('This page is past the end of the results.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /go to page 1/i }));
    expect(onFirst).toHaveBeenCalled();
  });
});
