import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { movieDetailsNullMetadata } from '../../mocks/fixtures';
import { server } from '../../test/server';
import MovieDetailsView from './MovieDetailsView';

const MATCH_TRIGGER = 'Match manually';

// Direct navigation: the library browse cache was never loaded, so only the details response
// can tell the view the enrichment status.
function renderWithoutLibraryCache() {
  const libraryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={libraryClient}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
}

function useDetailsStatus(enrichmentStatus: string) {
  server.use(
    http.get('*/api/movies/:id', () =>
      HttpResponse.json({ ...movieDetailsNullMetadata, enrichmentStatus }),
    ),
  );
}

describe('MovieDetailsView without a loaded library cache', () => {
  it('offers Match manually and explains a NotFound movie from the details status', async () => {
    useDetailsStatus('NotFound');
    renderWithoutLibraryCache();

    expect(await screen.findByRole('button', { name: MATCH_TRIGGER })).toBeInTheDocument();
    expect(screen.getByText('The last lookup found no match for this title.')).toBeInTheDocument();
  });

  it('offers Match manually for a Failed movie from the details status', async () => {
    useDetailsStatus('Failed');
    renderWithoutLibraryCache();

    expect(await screen.findByRole('button', { name: MATCH_TRIGGER })).toBeInTheDocument();
    expect(screen.getByText('The last metadata lookup failed.')).toBeInTheDocument();
  });

  it('hides Match manually and Fetch metadata while the details status is Pending', async () => {
    useDetailsStatus('Pending');
    renderWithoutLibraryCache();

    expect(await screen.findByText('A metadata lookup is in progress.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: MATCH_TRIGGER })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Fetch metadata' })).toBeNull();
  });

  it('confirms a Fetch metadata request even when the lookup ends NotFound again', async () => {
    useDetailsStatus('NotFound');
    server.use(
      http.post('*/api/movies/:id/enrichment', () => new HttpResponse(null, { status: 202 })),
    );
    renderWithoutLibraryCache();

    fireEvent.click(await screen.findByRole('button', { name: 'Fetch metadata' }));

    expect(await screen.findByText(/Metadata lookup requested/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: MATCH_TRIGGER })).toBeInTheDocument();
  });
});
