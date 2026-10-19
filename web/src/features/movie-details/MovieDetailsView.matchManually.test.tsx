import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { browseMoviesQueryKey } from '../../library/libraryQueries';
import { movieDetailsNullMetadata, problemDetails } from '../../mocks/fixtures';
import { server } from '../../test/server';
import MovieDetailsView from './MovieDetailsView';

type EnrichmentStatusValue = 'NotFound' | 'Failed' | 'Pending' | 'Enriched';

const MATCH_TRIGGER = 'Match manually';
const MATCH_INPUT_LABEL = 'IMDb id or URL';
const NOT_IN_LIBRARY = 'This movie is no longer in the library.';
const INVALID_INPUT = 'Enter an IMDb id like tt0083658, or an IMDb title URL.';
const GENERIC_MATCH_ERROR = "Couldn't match this movie. Try again later.";
const ALREADY_ASSIGNED_FALLBACK = 'Another movie in your library already has this IMDb id.';

function seedLibrary(libraryClient: QueryClient, status: EnrichmentStatusValue) {
  libraryClient.setQueryData(browseMoviesQueryKey, {
    items: [
      {
        id: 1,
        title: 'Dune',
        posterUrl: null,
        releaseYear: null,
        runtime: null,
        imdbRating: null,
        enrichmentStatus: status,
        isInWatchlist: false,
      },
    ],
    totalCount: 1,
  });
}

function renderView(status: EnrichmentStatusValue = 'NotFound') {
  const libraryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  seedLibrary(libraryClient, status);
  const rendered = render(
    <QueryClientProvider client={libraryClient}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
  return { libraryClient, ...rendered };
}

function useNullMetadataDetails() {
  let detailsGets = 0;
  server.use(
    http.get('*/api/movies/:id', () => {
      detailsGets += 1;
      return HttpResponse.json(movieDetailsNullMetadata);
    }),
  );
  return () => detailsGets;
}

async function openMatchDialog() {
  fireEvent.click(await screen.findByRole('button', { name: MATCH_TRIGGER }));
  return screen.getByRole('dialog', { name: 'Match manually' });
}

function submitMatch(value: string) {
  const input = screen.getByLabelText(MATCH_INPUT_LABEL);
  fireEvent.change(input, { target: { value } });
  fireEvent.submit(input.closest('form')!);
}

describe('MovieDetailsView manual IMDb match', () => {
  it('offers Match manually for a NotFound movie with no metadata', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    renderView('NotFound');

    expect(await screen.findByRole('button', { name: MATCH_TRIGGER })).toBeInTheDocument();
  });

  it('offers Match manually for a Failed movie with no metadata', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    renderView('Failed');

    expect(await screen.findByRole('button', { name: MATCH_TRIGGER })).toBeInTheDocument();
  });

  it('hides Match manually while a lookup is pending', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    renderView('Pending');

    await screen.findByText('No metadata available yet.');
    expect(screen.queryByRole('button', { name: MATCH_TRIGGER })).toBeNull();
  });

  it('rejects input without an IMDb id, alerts, and sends no request', async () => {
    let putCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', () => {
        putCalls += 1;
        return new HttpResponse(null, { status: 202 });
      }),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    submitMatch('just a title, no id');

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(INVALID_INPUT);
    expect(screen.getByLabelText(MATCH_INPUT_LABEL)).toHaveAttribute(
      'aria-describedby',
      expect.stringContaining(within(dialog).getByRole('alert').id),
    );
    expect(putCalls).toBe(0);
    expect(screen.getByRole('dialog', { name: 'Match manually' })).toBeInTheDocument();
  });

  it('sends the id extracted from a pasted IMDb URL, marks the row Pending, and closes', async () => {
    const bodies: unknown[] = [];
    const idsSeen: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', async ({ request, params }) => {
        idsSeen.push(String(params.id));
        bodies.push(await request.json());
        return new HttpResponse(null, { status: 202, headers: { Location: '/api/movies/1' } });
      }),
    );
    const { libraryClient } = renderView('NotFound');
    await openMatchDialog();

    submitMatch('https://www.imdb.com/title/tt0083658/');

    await waitFor(() =>
      expect(screen.queryByRole('dialog', { name: 'Match manually' })).toBeNull(),
    );
    expect(idsSeen).toEqual(['1']);
    expect(bodies).toEqual([{ imdbId: 'tt0083658' }]);
    const browse = libraryClient.getQueryData<{ items: { enrichmentStatus: string }[] }>(
      browseMoviesQueryKey,
    );
    expect(browse?.items[0].enrichmentStatus).toBe('Pending');
    expect(screen.queryByRole('button', { name: MATCH_TRIGGER })).toBeNull();
  });

  it('refreshes the movie details after a successful match', async () => {
    const getDetails = useNullMetadataDetails();
    server.use(http.put('*/api/movies/:id/imdb', () => new HttpResponse(null, { status: 202 })));
    renderView('NotFound');
    await openMatchDialog();
    const initialGets = getDetails();

    submitMatch('tt0083658');

    await waitFor(() => expect(getDetails()).toBeGreaterThan(initialGets));
  });

  it('shows the not-in-library message and keeps the dialog open on 404', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', () =>
        HttpResponse.json(problemDetails(404), {
          status: 404,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
      ),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    submitMatch('tt0083658');

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(NOT_IN_LIBRARY);
    expect(screen.getByRole('dialog', { name: 'Match manually' })).toBeInTheDocument();
  });

  it('shows the generic message and keeps the dialog open on a server error', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', () =>
        HttpResponse.json(problemDetails(500), {
          status: 500,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
      ),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    submitMatch('tt0083658');

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(GENERIC_MATCH_ERROR);
    expect(screen.getByRole('dialog', { name: 'Match manually' })).toBeInTheDocument();
  });

  it('shows the server detail and keeps the dialog open on a 409 conflict', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', () =>
        HttpResponse.json(
          { ...problemDetails(409), detail: 'IMDb id tt8503618 is already assigned to movie 302.' },
          {
            status: 409,
            headers: { 'Content-Type': 'application/problem+json' },
          },
        ),
      ),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    submitMatch('tt8503618');

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(
      'IMDb id tt8503618 is already assigned to movie 302.',
    );
    expect(screen.getByRole('dialog', { name: 'Match manually' })).toBeInTheDocument();
  });

  it('shows the fallback message on a 409 conflict without a body', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', () => new HttpResponse(null, { status: 409 })),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    submitMatch('tt8503618');

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(ALREADY_ASSIGNED_FALLBACK);
    expect(screen.getByRole('dialog', { name: 'Match manually' })).toBeInTheDocument();
  });

  it('shows the fallback message on a 409 conflict with a non-JSON body', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put(
        '*/api/movies/:id/imdb',
        () => new HttpResponse('<html>Conflict</html>', { status: 409 }),
      ),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    submitMatch('tt8503618');

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(ALREADY_ASSIGNED_FALLBACK);
    expect(screen.getByRole('dialog', { name: 'Match manually' })).toBeInTheDocument();
  });

  it('closes on Cancel and returns focus to the Match manually trigger', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    renderView('NotFound');
    await openMatchDialog();

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(screen.queryByRole('dialog', { name: 'Match manually' })).toBeNull();
    expect(screen.getByRole('button', { name: MATCH_TRIGGER })).toHaveFocus();
  });

  it('closes on Escape without sending a request', async () => {
    let putCalls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)),
      http.put('*/api/movies/:id/imdb', () => {
        putCalls += 1;
        return new HttpResponse(null, { status: 202 });
      }),
    );
    renderView('NotFound');
    const dialog = await openMatchDialog();

    fireEvent.keyDown(dialog, { key: 'Escape' });

    expect(screen.queryByRole('dialog', { name: 'Match manually' })).toBeNull();
    expect(putCalls).toBe(0);
  });
});
