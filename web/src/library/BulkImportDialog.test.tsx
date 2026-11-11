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

  it('refreshes the candidate and failed lists after a successful import', async () => {
    let folders = ['Alpha'];
    let failedRows = [failedRow('Broken Cut')];
    server.use(
      http.get('/api/movies/import/candidates', () =>
        HttpResponse.json({ newFolders: folders }, { status: 200 }),
      ),
      http.post('/api/movies/import/library', async ({ request }) => {
        const body = (await request.json()) as { folders: string[]; retryFailed: boolean };
        folders = folders.filter((folder) => !body.folders.includes(folder));
        if (body.retryFailed) {
          failedRows = [];
        }
        return HttpResponse.json(
          { imported: body.folders.length, skipped: 0, retried: 0, failures: [] },
          { status: 200 },
        );
      }),
      http.get('/api/movies', ({ request }) => {
        const statuses = new URL(request.url).searchParams.getAll('Statuses');
        return HttpResponse.json(browseBody(statuses.includes('Failed') ? failedRows : []), {
          status: 200,
        });
      }),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(
      await within(dialog).findByRole('checkbox', { name: 'Retry failed enrichments' }),
    );
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    await waitFor(() => {
      expect(within(dialog).queryByLabelText('Alpha')).not.toBeInTheDocument();
      expect(
        within(dialog).queryByRole('checkbox', { name: 'Retry failed enrichments' }),
      ).not.toBeInTheDocument();
    });
    expect(within(dialog).getByText('No movies are waiting in Failed enrichment.')).toBeVisible();
  });

  it('shows per-folder progress while the pass runs', async () => {
    let release: (() => void) | undefined;
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      candidatesHandler(['Alpha', 'Bravo']),
      http.post('/api/movies/import/library', async () => {
        await gate;
        return HttpResponse.json(
          { imported: 1, skipped: 0, retried: 0, failures: [] },
          { status: 200 },
        );
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByLabelText('Bravo'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const bar = await within(dialog).findByRole('progressbar');
    expect(bar).toHaveTextContent('Importing 1 of 2');
    expect(bar).toHaveTextContent('Alpha');

    release?.();

    const status = await within(dialog).findByRole('status');
    expect(status).toHaveTextContent('Imported 2');
    expect(within(dialog).queryByRole('progressbar')).not.toBeInTheDocument();
  });

  it('stops issuing folder requests when the dialog is closed mid-pass', async () => {
    let posts = 0;
    let release: (() => void) | undefined;
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    server.use(
      candidatesHandler(['Alpha', 'Bravo']),
      http.post('/api/movies/import/library', async () => {
        posts += 1;
        await gate;
        return HttpResponse.json(
          { imported: 1, skipped: 0, retried: 0, failures: [] },
          { status: 200 },
        );
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByLabelText('Bravo'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));
    await within(dialog).findByRole('progressbar');

    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }));
    release?.();

    await waitFor(() => {
      expect(posts).toBe(1);
    });
    expect(posts).toBe(1);
  });

  it('aggregates counts and failures across the per-folder requests', async () => {
    let call = 0;
    server.use(
      candidatesHandler(['Alpha', 'Bravo']),
      http.post('/api/movies/import/library', () => {
        call += 1;
        if (call === 1) {
          return HttpResponse.json(
            { imported: 1, skipped: 0, retried: 0, failures: [] },
            { status: 200 },
          );
        }
        return HttpResponse.json(
          {
            imported: 0,
            skipped: 1,
            retried: 0,
            failures: [{ folder: 'Bravo', reason: 'no-video' }],
          },
          { status: 200 },
        );
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByLabelText('Bravo'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const status = await within(dialog).findByRole('status');
    expect(status).toHaveTextContent('Imported 1');
    expect(status).toHaveTextContent('Skipped 1');
    expect(status).toHaveTextContent(BULK_IMPORT_REASON_TEXT['no-video']);
  });

  it('keeps the partial summary and reports an error when a folder fails mid-pass', async () => {
    let call = 0;
    server.use(
      candidatesHandler(['Alpha', 'Bravo']),
      http.post('/api/movies/import/library', () => {
        call += 1;
        if (call === 1) {
          return HttpResponse.json(
            { imported: 1, skipped: 0, retried: 0, failures: [] },
            { status: 200 },
          );
        }
        return HttpResponse.json(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500 },
        );
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByLabelText('Bravo'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const alert = await within(dialog).findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_BULK_IMPORT_ERROR);
    expect(within(dialog).getByRole('status')).toHaveTextContent('Imported 1');
  });

  it('keeps recorded per-folder failures when a later request errors', async () => {
    let call = 0;
    server.use(
      candidatesHandler(['Alpha', 'Bravo']),
      http.post('/api/movies/import/library', () => {
        call += 1;
        if (call === 1) {
          return HttpResponse.json(
            {
              imported: 0,
              skipped: 0,
              retried: 0,
              failures: [{ folder: 'Alpha', reason: 'no-video' }],
            },
            { status: 200 },
          );
        }
        return HttpResponse.json(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500 },
        );
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByLabelText('Bravo'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    const alert = await within(dialog).findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_BULK_IMPORT_ERROR);
    const status = within(dialog).getByRole('status');
    expect(status).toHaveTextContent('Imported 0');
    expect(status).toHaveTextContent(BULK_IMPORT_REASON_TEXT['no-video']);
  });

  it('shows a scan error with retry when the candidate query fails', async () => {
    let calls = 0;
    server.use(
      http.get('/api/movies/import/candidates', () => {
        calls += 1;
        if (calls === 1) {
          return HttpResponse.json(
            { type: 'about:blank', title: 'Internal Server Error', status: 500 },
            { status: 500 },
          );
        }
        return HttpResponse.json({ newFolders: ['Alpha'] }, { status: 200 });
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody([]), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    const alert = await within(dialog).findByRole('alert');
    expect(alert).toHaveTextContent('Could not scan the library.');

    fireEvent.click(within(dialog).getByRole('button', { name: 'Retry scan' }));

    expect(await within(dialog).findByLabelText('Alpha')).toBeInTheDocument();
    expect(within(dialog).queryByRole('alert')).not.toBeInTheDocument();
  });

  it('shows an error when the failed-enrichment query fails', async () => {
    server.use(
      candidatesHandler([]),
      http.get('/api/movies', () =>
        HttpResponse.json(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500 },
        ),
      ),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    const alert = await within(dialog).findByRole('alert');
    expect(alert).toHaveTextContent('Could not load failed enrichments.');
    expect(
      within(dialog).queryByText('No movies are waiting in Failed enrichment.'),
    ).not.toBeInTheDocument();
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
