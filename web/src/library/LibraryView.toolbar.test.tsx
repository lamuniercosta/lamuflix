import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import App from '../App';

function renderApp() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  }
  return render(<App />, { wrapper: Wrapper });
}

async function waitForLibrary() {
  await screen.findByRole('link', { name: 'Dune' }, { timeout: 5000 });
}

function getFiltersToggle() {
  return screen.getByRole('button', { name: /^Filters/ });
}

function openFilters() {
  fireEvent.click(getFiltersToggle());
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
  cleanup();
});

describe('Library results toolbar', () => {
  it('shows an include-unknown-runtime chip and clears only the flag', async () => {
    window.history.replaceState({}, '', '/?runtimeMin=10&runtimeMax=&runtimeIncludeUnknown=true');
    renderApp();
    await waitForLibrary();

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
    await waitForLibrary();

    expect(screen.getAllByText('Status: Pending')).toHaveLength(2);
    fireEvent.click(screen.getAllByRole('button', { name: 'Remove status filter Pending' })[0]);
    expect(new URLSearchParams(window.location.search).getAll('statuses')).toEqual(['Pending']);
    expect(screen.getAllByText('Status: Pending')).toHaveLength(1);
  });

  it('hides range chips for inactive extent defaults', async () => {
    window.history.replaceState({}, '', '/?runtimeMin=0&runtimeMax=240&statuses=Pending');
    renderApp();
    await waitForLibrary();

    expect(screen.queryByText(/Runtime (from|to)/)).not.toBeInTheDocument();
    expect(screen.getByText('Status: Pending')).toBeInTheDocument();
  });

  it('falls back the sort summary for unknown sort values without changing the query', async () => {
    window.history.replaceState({}, '', '/?sort=Bogus&direction=Ascending');
    renderApp();
    await waitForLibrary();

    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
    expect(new URLSearchParams(window.location.search).get('sort')).toBe('Bogus');

    openFilters();
    for (const field of ['Title', 'Year', 'Rating', 'Runtime']) {
      expect(screen.getByRole('button', { name: field })).toHaveAttribute('aria-pressed', 'false');
    }
    expect(screen.getByRole('button', { name: 'Ascending' })).toBeInTheDocument();
  });

  it('shows the result count and the current sort summary', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    expect(screen.getByText('2 movies')).toBeInTheDocument();
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
  });

  it('renders one removable chip per active filter value', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&statuses=Failed');
    renderApp();
    await waitForLibrary();

    expect(screen.getByText('Search: dune')).toBeInTheDocument();
    expect(screen.getByText('Status: Pending')).toBeInTheDocument();
    expect(screen.getByText('Status: Failed')).toBeInTheDocument();
  });

  it('removes only the chosen chip value and resets page', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&statuses=Failed&page=3');
    renderApp();
    await waitForLibrary();

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
    await waitForLibrary();

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
    await waitForLibrary();

    fireEvent.click(screen.getByRole('button', { name: 'Remove status filter Pending' }));
    expect(new URLSearchParams(window.location.search).get('foo')).toBe('bar');
  });

  it('resets page when the sort field changes in the panel', async () => {
    window.history.replaceState({}, '', '/?page=3');
    renderApp();
    await waitForLibrary();

    openFilters();
    fireEvent.click(screen.getByRole('button', { name: 'Year' }));
    let params = new URLSearchParams(window.location.search);
    expect(params.get('sort')).toBe('Year');
    expect(params.get('page') ?? '1').toBe('1');
    expect(screen.getByText('Sort: Year (oldest)')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Ascending' }));
    params = new URLSearchParams(window.location.search);
    expect(params.get('direction')).toBe('Descending');
    expect(screen.getByText('Sort: Year (newest)')).toBeInTheDocument();
  });

  it('restores the query on Back and Forward navigation', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    openFilters();
    fireEvent.click(screen.getByRole('button', { name: 'Year' }));
    expect(new URLSearchParams(window.location.search).get('sort')).toBe('Year');
    expect(screen.getByText('Sort: Year (oldest)')).toBeInTheDocument();

    window.history.replaceState({}, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('sort')).toBeNull();
    });
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
  });
});

describe('Filters toggle and inline collapsible panel', () => {
  it('starts collapsed and wires aria-expanded and aria-controls to the panel', async () => {
    window.history.replaceState({}, '', '/');
    const { container } = renderApp();
    await waitForLibrary();

    const toggle = getFiltersToggle();
    expect(toggle).toHaveAttribute('aria-expanded', 'false');
    const panel = container.querySelector('.filter-collapse');
    expect(panel).not.toBeNull();
    expect(panel).toHaveAttribute('data-open', 'false');
    expect(panel).toHaveAttribute('inert');
    expect(document.getElementById(toggle.getAttribute('aria-controls') ?? '')).toBe(panel);
  });

  it('opens the panel from the toggle, making it reachable and hiding the chip list', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending');
    const { container } = renderApp();
    await waitForLibrary();

    expect(screen.getByRole('list', { name: 'Active filters' })).toBeInTheDocument();

    openFilters();
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'true');
    const panel = container.querySelector('.filter-collapse');
    expect(panel).toHaveAttribute('data-open', 'true');
    expect(panel).not.toHaveAttribute('inert');
    expect(screen.queryByRole('list', { name: 'Active filters' })).not.toBeInTheDocument();

    openFilters();
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'false');
    expect(screen.getByRole('list', { name: 'Active filters' })).toBeInTheDocument();
  });

  it('shows the active filter group count on the toggle badge', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending&yearMin=2000');
    renderApp();
    await waitForLibrary();

    expect(getFiltersToggle()).toHaveAccessibleName(
      expect.stringMatching(/2 filter groups active/),
    );
  });

  it('closes on Collapse and returns focus to the Filters toggle', async () => {
    window.history.replaceState({}, '', '/');
    const { container } = renderApp();
    await waitForLibrary();

    openFilters();
    fireEvent.click(screen.getByRole('button', { name: 'Collapse' }));

    const toggle = getFiltersToggle();
    expect(toggle).toHaveFocus();
    expect(toggle).toHaveAttribute('aria-expanded', 'false');
    expect(container.querySelector('.filter-collapse')).toHaveAttribute('inert');
  });

  it('clears URL-sourced filters from the collapsed chip list with Clear all', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&foo=bar');
    const { container } = renderApp();
    await waitForLibrary();

    const chipRow = container.querySelector('.library-chips');
    expect(chipRow).not.toBeNull();
    fireEvent.click(within(chipRow as HTMLElement).getByRole('button', { name: 'Clear all' }));

    const params = new URLSearchParams(window.location.search);
    expect(params.get('text') ?? '').toBe('');
    expect(params.getAll('statuses')).toEqual([]);
    expect(params.get('foo')).toBe('bar');
    expect(screen.queryByRole('list', { name: 'Active filters' })).not.toBeInTheDocument();
  });

  it('keeps the same movies when switching between Grid and List', async () => {
    window.history.replaceState({}, '', '/');
    const { container } = renderApp();
    await waitForLibrary();

    const results = screen.getByRole('region', { name: 'Results' });
    expect(results.querySelectorAll('.movie-card')).toHaveLength(2);

    fireEvent.click(screen.getByRole('button', { name: 'List' }));
    expect(screen.getByRole('button', { name: 'List' })).toHaveAttribute('aria-pressed', 'true');
    expect(container.querySelectorAll('.movie-row')).toHaveLength(2);
    expect(screen.getByRole('link', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Arrival' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Grid' }));
    expect(screen.getByRole('button', { name: 'Grid' })).toHaveAttribute('aria-pressed', 'true');
    expect(results.querySelectorAll('.movie-card')).toHaveLength(2);
  });

  it('keeps the panel open across query changes', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    openFilters();
    window.history.replaceState({}, '', '/?text=x');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('text')).toBe('x');
    });
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'true');
  });

  it('always starts collapsed on a fresh load', async () => {
    window.history.replaceState({}, '', '/');
    const first = renderApp();
    await waitForLibrary();

    openFilters();
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'true');
    first.unmount();
    cleanup();

    renderApp();
    await waitForLibrary();
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'false');
  });
});

describe('Library section heading', () => {
  it('names the results after the text search', async () => {
    window.history.replaceState({}, '', '/?text=dune');
    renderApp();
    await waitForLibrary();

    expect(
      screen.getByRole('heading', { level: 2, name: 'Results for “dune”' }),
    ).toBeInTheDocument();
  });

  it('names the results after the selected genres in URL order', async () => {
    window.history.replaceState({}, '', '/?genreIds=2&genreIds=1');
    renderApp();
    await waitForLibrary();

    expect(
      await screen.findByRole('heading', { level: 2, name: 'Sci-Fi & Action' }),
    ).toBeInTheDocument();
  });

  it('prefers the text search over the selected genres', async () => {
    window.history.replaceState({}, '', '/?text=dune&genreIds=1');
    renderApp();
    await waitForLibrary();

    expect(
      screen.getByRole('heading', { level: 2, name: 'Results for “dune”' }),
    ).toBeInTheDocument();
  });

  it('names the watchlist view', async () => {
    window.history.replaceState({}, '', '/?inWatchlist=true');
    renderApp();

    expect(await screen.findByRole('heading', { level: 2, name: 'Watchlist' })).toBeInTheDocument();
  });

  it('names the unfiltered library "All movies"', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    expect(screen.getByRole('heading', { level: 2, name: 'All movies' })).toBeInTheDocument();
  });
});
