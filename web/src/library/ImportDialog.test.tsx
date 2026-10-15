import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import App from '../App';
import {
  importBadRequestHandler,
  importNetworkErrorHandler,
  importUnprocessableHandler,
} from '../test/handlers';
import { server } from '../test/server';
import { GENERIC_IMPORT_ERROR } from './libraryQueries';

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

async function openImport() {
  renderApp();
  fireEvent.click(screen.getByRole('button', { name: 'Import folder' }));
  expect(screen.getByRole('dialog', { name: 'Import a movie folder' })).toBeInTheDocument();
  expect(screen.getByLabelText('Folder path')).toHaveFocus();
  expect(screen.getByRole('button', { name: 'Import' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
}

describe('ImportDialog', () => {
  it('opens from Import folder showing title, label, and actions', async () => {
    await openImport();
  });

  it('submits on Enter and prepends a Pending row from Location on 202', async () => {
    await openImport();
    const input = screen.getByLabelText('Folder path');
    fireEvent.change(input, { target: { value: 'F:/library/NewArrival' } });
    fireEvent.submit(input.closest('form')!);

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: 'Import folder' })).toHaveFocus();
    expect(screen.getByText('NewArrival')).toBeInTheDocument();
    const rows = screen.getAllByRole('listitem');
    expect(rows[0]).toHaveTextContent('NewArrival');
    expect(rows[0]).toHaveTextContent('Pending');
  });

  it('keeps the dialog open and describes a 422 folderPath problem', async () => {
    server.use(importUnprocessableHandler);
    await openImport();
    fireEvent.change(screen.getByLabelText('Folder path'), { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: 'Import' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Folder path is required.');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByLabelText('Folder path')).toHaveAttribute('aria-describedby', alert.id);
  });

  it('keeps the dialog open and describes a generic 400 error', async () => {
    server.use(importBadRequestHandler);
    await openImport();
    fireEvent.change(screen.getByLabelText('Folder path'), {
      target: { value: 'F:/library/NewArrival' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Import' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_IMPORT_ERROR);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByLabelText('Folder path')).toHaveAttribute('aria-describedby', alert.id);
  });

  it('keeps the dialog open and describes a generic network error', async () => {
    server.use(importNetworkErrorHandler);
    await openImport();
    fireEvent.change(screen.getByLabelText('Folder path'), {
      target: { value: 'F:/library/NewArrival' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Import' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent(GENERIC_IMPORT_ERROR);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByLabelText('Folder path')).toHaveAttribute('aria-describedby', alert.id);
  });

  it('returns focus to Import folder on Escape and Cancel', async () => {
    await openImport();
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Import folder' })).toHaveFocus();

    fireEvent.click(screen.getByRole('button', { name: 'Import folder' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Import folder' })).toHaveFocus();
  });
});
