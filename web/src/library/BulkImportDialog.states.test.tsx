import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import type { paths } from '../api/types';
import { server } from '../test/server';
import BulkImportDialog from './BulkImportDialog';

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

function renderDialog() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<BulkImportDialog open onClose={() => {}} />, { wrapper: Wrapper });
}

function browseBody() {
  return { items: [], totalCount: 0 };
}

function candidatesHandler(folders: string[]) {
  return http.get('/api/movies/import/candidates', () =>
    HttpResponse.json({ newFolders: folders }, { status: 200 }),
  );
}

describe('BulkImportDialog scan states', () => {
  it('shows a scanning state while the candidate scan runs', async () => {
    let resolveScan!: () => void;
    const scanGate = new Promise<void>((resolve) => {
      resolveScan = resolve;
    });
    server.use(
      http.get('/api/movies/import/candidates', async () => {
        await scanGate;
        return HttpResponse.json({ newFolders: [] }, { status: 200 });
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody(), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    expect(await within(dialog).findByText(/scanning library/i)).toBeInTheDocument();
    resolveScan();
  });

  it('reports an empty scan with no new movies', async () => {
    server.use(
      candidatesHandler([]),
      http.get('/api/movies', () => HttpResponse.json(browseBody(), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    expect(await within(dialog).findByText('No new movies found')).toBeInTheDocument();
  });

  it('shows Close instead of Cancel when idle', async () => {
    server.use(
      candidatesHandler(['Alpha']),
      http.get('/api/movies', () => HttpResponse.json(browseBody(), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    await within(dialog).findByLabelText('Alpha');
    expect(within(dialog).getByRole('button', { name: 'Close' })).toBeInTheDocument();
    expect(within(dialog).queryByRole('button', { name: 'Cancel' })).not.toBeInTheDocument();
  });

  it('keeps Cancel while an import runs', async () => {
    let resolveImport!: () => void;
    const importGate = new Promise<void>((resolve) => {
      resolveImport = resolve;
    });
    server.use(
      candidatesHandler(['Alpha']),
      http.post('/api/movies/import/library', async () => {
        await importGate;
        return HttpResponse.json(
          { imported: 1, skipped: 0, retried: 0, failures: [] },
          { status: 200 },
        );
      }),
      http.get('/api/movies', () => HttpResponse.json(browseBody(), { status: 200 })),
    );

    renderDialog();

    const dialog = screen.getByRole('dialog', { name: 'Bulk import' });
    fireEvent.click(await within(dialog).findByLabelText('Alpha'));
    fireEvent.click(within(dialog).getByRole('button', { name: 'Import' }));

    expect(await within(dialog).findByRole('button', { name: 'Cancel' })).toBeInTheDocument();
    expect(within(dialog).queryByRole('button', { name: 'Close' })).not.toBeInTheDocument();
    resolveImport();
    expect(await within(dialog).findByRole('status')).toBeInTheDocument();
  });
});
