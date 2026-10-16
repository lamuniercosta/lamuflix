import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import App from '../App';

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

const originalMatchMedia = window.matchMedia;

function stubMatchMedia(matches: boolean) {
  const listeners = new Set<(event: { matches: boolean }) => void>();
  const media = {
    matches,
    media: '(min-width: 48rem)',
    addEventListener: (_type: string, listener: (event: { matches: boolean }) => void) => {
      listeners.add(listener);
    },
    removeEventListener: (_type: string, listener: (event: { matches: boolean }) => void) => {
      listeners.delete(listener);
    },
    dispatch: (next: boolean) => {
      for (const listener of listeners) {
        listener({ matches: next });
      }
    },
  };
  window.matchMedia = vi.fn().mockReturnValue(media);
  return media;
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
  document.body.style.overflow = '';
  window.matchMedia = originalMatchMedia;
  cleanup();
});

describe('Library results toolbar', () => {
  it('shows an include-unknown-runtime chip and clears only the flag', async () => {
    window.history.replaceState({}, '', '/?runtimeMin=10&runtimeMax=&runtimeIncludeUnknown=true');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    const chips = screen.getByRole('list', { name: 'Active filters' });
    expect(chips).toHaveTextContent('Include unknown runtime');
    fireEvent.click(screen.getByRole('button', { name: 'Remove include unknown runtime filter' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.get('runtimeIncludeUnknown')).toBe('false');
    expect(params.get('runtimeMin')).toBe('10');
    expect(screen.getByRole('list', { name: 'Active filters' })).not.toHaveTextContent(
      'Include unknown runtime',
    );
    expect(screen.getByText('Runtime from 10 min')).toBeInTheDocument();
  });

  it('renders repeated values with unique chips that remove one occurrence', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending&statuses=Pending');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(screen.getAllByText('Status: Pending')).toHaveLength(2);
    fireEvent.click(screen.getAllByRole('button', { name: 'Remove status filter Pending' })[0]);
    expect(new URLSearchParams(window.location.search).getAll('statuses')).toEqual(['Pending']);
    expect(screen.getAllByText('Status: Pending')).toHaveLength(1);
  });

  it('hides range chips for inactive extent defaults', async () => {
    window.history.replaceState({}, '', '/?runtimeMin=0&runtimeMax=240&statuses=Pending');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(screen.queryByText(/Runtime (from|to)/)).not.toBeInTheDocument();
    expect(screen.getByText('Status: Pending')).toBeInTheDocument();
  });

  it('falls back the sort control for unknown sort values without changing the query', async () => {
    window.history.replaceState({}, '', '/?sort=Bogus&direction=Ascending');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(screen.getByRole('combobox', { name: 'Sort movies' })).toHaveValue('Title|Ascending');
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
    const params = new URLSearchParams(window.location.search);
    expect(params.get('sort')).toBe('Bogus');
  });

  it('shows result count and current sort summary', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(screen.getByText('2 movies')).toBeInTheDocument();
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
  });

  it('renders one removable chip per active filter value', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&statuses=Failed');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(screen.getByText('Search: dune')).toBeInTheDocument();
    expect(screen.getByText('Status: Pending')).toBeInTheDocument();
    expect(screen.getByText('Status: Failed')).toBeInTheDocument();
  });

  it('removes only the chosen chip value and resets page', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&statuses=Failed&page=3');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Remove status filter Pending' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.getAll('statuses')).toEqual(['Failed']);
    expect(params.get('text')).toBe('dune');
    expect(params.get('page') ?? '1').toBe('1');
    expect(screen.queryByText('Status: Pending')).not.toBeInTheDocument();
    expect(screen.getByText('Status: Failed')).toBeInTheDocument();
  });

  it('removes one range endpoint while retaining the other', async () => {
    window.history.replaceState({}, '', '/?yearMin=2000&yearMax=2020');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Remove minimum year 2000' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.get('yearMin') ?? '').toBe('');
    expect(params.get('yearMax')).toBe('2020');
    expect(screen.queryByText('From 2000')).not.toBeInTheDocument();
    expect(screen.getByText('To 2020')).toBeInTheDocument();
  });

  it('preserves unknown query keys across chip removal', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending&foo=bar');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Remove status filter Pending' }));
    expect(new URLSearchParams(window.location.search).get('foo')).toBe('bar');
  });

  it('resets page when sort changes from the toolbar', async () => {
    window.history.replaceState({}, '', '/?page=3');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.change(screen.getByRole('combobox', { name: 'Sort movies' }), {
      target: { value: 'Year|Descending' },
    });
    const params = new URLSearchParams(window.location.search);
    expect(params.get('sort')).toBe('Year');
    expect(params.get('direction')).toBe('Descending');
    expect(params.get('page') ?? '1').toBe('1');
    expect(screen.getByText('Sort: Year (newest)')).toBeInTheDocument();
  });

  it('restores the query on Back and Forward navigation', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.change(screen.getByRole('combobox', { name: 'Sort movies' }), {
      target: { value: 'Year|Descending' },
    });
    expect(new URLSearchParams(window.location.search).get('sort')).toBe('Year');

    window.history.replaceState({}, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('sort')).toBeNull();
    });
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
  });
});

describe('Filter sidebar and drawer', () => {
  it('selects the sidebar at 768px and wider, expanded by default', async () => {
    stubMatchMedia(true);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(window.matchMedia).toHaveBeenCalledWith('(min-width: 48rem)');
    expect(screen.queryByRole('button', { name: 'Filters' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Collapse filters' })).toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'Sort movies' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Collapse filters' }));
    expect(screen.getByRole('button', { name: 'Show filters' })).toBeInTheDocument();
  });

  it('selects the closed drawer below 768px and applies edits live', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeInTheDocument();

    fireEvent.click(screen.getByLabelText(/^Pending$/i));
    expect(new URLSearchParams(window.location.search).getAll('statuses')).toContain('Pending');

    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
    expect(new URLSearchParams(window.location.search).getAll('statuses')).toContain('Pending');
    expect(screen.getByText('Status: Pending')).toBeInTheDocument();
  });

  it('marks the background inert while the drawer is open', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    const { container } = renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    const browse = container.querySelector('.lamu-browse');
    expect(browse).not.toBeNull();
    expect(browse).not.toHaveAttribute('inert');

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeInTheDocument();
    expect(browse).toHaveAttribute('inert');

    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(browse).not.toHaveAttribute('inert');
  });

  it('returns focus to the opener and restores body scroll on Done', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    expect(document.body.style.overflow).toBe('hidden');

    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.getByRole('button', { name: 'Filters' })).toHaveFocus();
    expect(document.body.style.overflow).toBe('');
  });

  it('closes the drawer on Escape and on backdrop with scroll cleanup', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
    expect(document.body.style.overflow).toBe('');

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    fireEvent.click(screen.getByRole('button', { name: 'Close filters' }));
    expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
    expect(document.body.style.overflow).toBe('');
  });

  it('traps focus inside the open drawer', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    const dialog = screen.getByRole('dialog', { name: 'Filters' });
    const focusables = dialog.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea',
    );
    const first = focusables[0];
    const last = focusables[focusables.length - 1];

    last.focus();
    fireEvent.keyDown(document, { key: 'Tab' });
    expect(document.activeElement).toBe(first);

    first.focus();
    fireEvent.keyDown(document, { key: 'Tab', shiftKey: true });
    expect(document.activeElement).toBe(last);
  });

  it('keeps the drawer across query changes but closes it on route change', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeInTheDocument();

    window.history.replaceState({}, '', '/?text=x');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('text')).toBe('x');
    });
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeInTheDocument();

    window.history.replaceState({}, '', '/movies/1');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
    });
    expect(document.body.style.overflow).toBe('');
  });

  it('closes the drawer on viewport transition to desktop', async () => {
    const media = stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeInTheDocument();
    media.dispatch(true);
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
    });
    expect(document.body.style.overflow).toBe('');
  });

  it('does not persist panel visibility across reloads', async () => {
    stubMatchMedia(false);
    window.history.replaceState({}, '', '/');
    const first = renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeInTheDocument();
    first.unmount();
    cleanup();

    renderApp();
    await screen.findByRole('link', { name: 'Open details for Dune' }, { timeout: 5000 });
    expect(screen.queryByRole('dialog', { name: 'Filters' })).not.toBeInTheDocument();
  });
});
