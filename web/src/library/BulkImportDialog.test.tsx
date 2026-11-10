import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import type { components, paths } from '../api/types';
import App from '../App';
import { server } from '../test/server';
import BulkImportDialog from './BulkImportDialog';
import { BULK_IMPORT_REASON_TEXT, GENERIC_BULK_IMPORT_ERROR } from './libraryQueries';

type MovieSummary = components['schemas']['MovieSummaryResponse'];

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

function renderDialog() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<BulkImportDialog open onClose={() => {}} />, { wrapper: Wrapper });
}

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

function failedRow(title: string): MovieSummary {
  return {
    id: 4,
    title,
    posterUrl: null,
    releaseYear: null,
    runtime: null,
    imdbRating: null,
    enrichmentStatus: 'Failed',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    review: 'none',
  };
}

function browseBody(items: MovieSummary[]) {
  return { items, totalCount: items.length };
}

function candidatesHandler(folders: string[]) {
  return http.get('/api/movies/import/candidates', () =>
    HttpResponse.json({ newFolders: folders }, { status: 200 }),
  );
}

describe('BulkImportDialog', () => {
  it('renders candidates with checkboxes and select all', async () => {
    server.use(
      candidatesHandler(['Alpha', 'Bravo']),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    expect(await within(dialog).findByLabelText('Alpha')).toBeInTheDocument();
    expect(within(dialog).getByLabelText('Bravo')).toBeInTheDocument();
    fireEvent.click(within(dialog).getByRole('button', { name: 'Select all' }));
    expect(within(dialog).getByLabelText('Alpha')).toBeChecked();
    expect(within(dialog).getByLabelText('Bravo')).toBeChecked();
  });

  it('shows the failed section from the Statuses=Failed browse', async () => {
    server.use(
      candidatesHandler([]),
      http.get('/api/movies', ({ request }) => {
        const statuses = new URL(request.url).searchParams.getAll('Statuses');
        if (statuses.includes('Failed')) {
          return HttpResponse.json(browseBody([failedRow('Broken Cut')]), { status: 200 });
        }
        return HttpResponse.json(browseBody([]), { status: 200 });
      }),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    expect(
      await within(dialog).findByRole('checkbox', { name: 'Retry failed enrichments' }),
    ).toBeInTheDocument();
  });

  it('shows the summary and invalidates the grid on successful import', async () => {
    let browseCalls = 0;
    server.use(
      candidatesHandler(['Alpha']),
      http.post('/api/movies/import/library', () =>
        HttpResponse.json({ imported: 1, skipped: 0, retried: 0, failures: [] }, { status: 200 }),
      ),
      http.get('/api/movies', () => {
        browseCalls += 1;
        return HttpResponse.json(browseBody([]), { status: 200 });
      }),
    );

    renderApp();

    fireEvent.click(screen.getByRole('button', { name: 'Bulk import' }));
    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const status = await within(dialog).findByRole('status');
    expect(status).toHaveTextContent('Imported 1');
    expect(status).toHaveTextContent('Skipped 0');
    expect(status).toHaveTextContent('Retried 0');
    await waitFor(() => {
      expect(browseCalls).toBeGreaterThan(2);
    });
  });

  it('shows the generic error with fixed reason text on import failure', async () => {
    server.use(
      candidatesHandler(['Alpha']),
      http.post('/api/movies/import/library', () =>
        HttpResponse.json(
          {
            imported: 0,
            skipped: 0,
            retried: 0,
            failures: [{ folder: 'Alpha', reason: 'no-video' }],
          },
          { status: 200 },
        ),
      ),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const status = await within(dialog).findByRole('status');
    expect(status).toHaveTextContent(BULK_IMPORT_REASON_TEXT['no-video']);
  });

  it('shows the generic error line when the import request fails', async () => {
    server.use(
      candidatesHandler(['Alpha']),
      http.post('/api/movies/import/library', () =>
        HttpResponse.json(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500 },
        ),
      ),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const alert = await within(dialog).findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_BULK_IMPORT_ERROR);
  });
});
