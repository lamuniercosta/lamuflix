import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import type { ReactNode } from 'react';
import { fileURLToPath } from 'node:url';
import { http, HttpResponse, delay } from 'msw';
import { afterEach, describe, expect, it } from 'vitest';
import App from '../App';
import { hostileCollections, overlongCollections } from '../mocks/fixtures';
import { server } from '../test/server';

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

function getSortSelect() {
  return screen.getByRole('combobox', { name: /Sort by/ });
}

// Tags render the kind and the value in separate spans; assert on the value span.
function tagValue(value: string | RegExp) {
  return screen.getByText(value, { selector: '.lamu-tag-value' });
}

function tagValues(value: string) {
  return screen.getAllByText(value, { selector: '.lamu-tag-value' });
}

function queryTagValue(value: string | RegExp) {
  return screen.queryByText(value, { selector: '.lamu-tag-value' });
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
  cleanup();
});

describe('Library results toolbar', () => {
  it('shows an include-unknown-runtime tag and clears only the flag', async () => {
    window.history.replaceState({}, '', '/?runtimeMin=10&runtimeMax=&runtimeIncludeUnknown=true');
    renderApp();
    await waitForLibrary();

    const tags = screen.getByRole('list', { name: 'Active filters' });
    expect(tags).toHaveTextContent('include unknown');
    fireEvent.click(screen.getByRole('button', { name: 'Remove include unknown runtime filter' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.get('runtimeIncludeUnknown')).toBe('false');
    expect(params.get('runtimeMin')).toBe('10');
    expect(screen.getByRole('list', { name: 'Active filters' })).not.toHaveTextContent(
      'include unknown',
    );
    expect(tagValue('from 10 min')).toBeInTheDocument();
  });

  it('renders repeated values with unique tags that remove one occurrence', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending&statuses=Pending');
    renderApp();
    await waitForLibrary();

    expect(tagValues('Pending')).toHaveLength(2);
    fireEvent.click(screen.getAllByRole('button', { name: 'Remove status filter Pending' })[0]);
    expect(new URLSearchParams(window.location.search).getAll('statuses')).toEqual(['Pending']);
    expect(tagValues('Pending')).toHaveLength(1);
  });

  it('hides range tags for inactive extent defaults', async () => {
    window.history.replaceState({}, '', '/?runtimeMin=0&runtimeMax=240&statuses=Pending');
    renderApp();
    await waitForLibrary();

    expect(queryTagValue(/^(from|to) \d/)).not.toBeInTheDocument();
    expect(tagValue('Pending')).toBeInTheDocument();
  });

  it('falls back the sort summary for unknown sort values without changing the query', async () => {
    window.history.replaceState({}, '', '/?sort=Bogus&direction=Ascending');
    renderApp();
    await waitForLibrary();

    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
    expect(new URLSearchParams(window.location.search).get('sort')).toBe('Bogus');
    expect(getSortSelect()).toHaveValue('Title|Ascending');
  });

  it('shows the result count and the current sort summary', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    expect(screen.getByText('2 movies')).toBeInTheDocument();
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
  });

  it('renders one removable tag per active filter value', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&statuses=Failed');
    renderApp();
    await waitForLibrary();

    expect(tagValue('Pending')).toBeInTheDocument();
    expect(tagValue('Failed')).toBeInTheDocument();
    expect(queryTagValue('dune')).not.toBeInTheDocument();
  });

  it('removes only the chosen tag value and resets page', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&statuses=Failed&page=3');
    renderApp();
    await waitForLibrary();

    fireEvent.click(screen.getByRole('button', { name: 'Remove status filter Pending' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.getAll('statuses')).toEqual(['Failed']);
    expect(params.get('text')).toBe('dune');
    expect(params.get('page') ?? '1').toBe('1');
    expect(queryTagValue('Pending')).not.toBeInTheDocument();
    expect(tagValue('Failed')).toBeInTheDocument();
  });

  it('removes one range endpoint while retaining the other', async () => {
    window.history.replaceState({}, '', '/?yearMin=2000&yearMax=2020');
    renderApp();
    await waitForLibrary();

    fireEvent.click(screen.getByRole('button', { name: 'Remove minimum year 2000' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.get('yearMin') ?? '').toBe('');
    expect(params.get('yearMax')).toBe('2020');
    expect(queryTagValue('from 2000')).not.toBeInTheDocument();
    expect(tagValue('to 2020')).toBeInTheDocument();
  });

  it('preserves unknown query keys across tag removal', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending&foo=bar');
    renderApp();
    await waitForLibrary();

    fireEvent.click(screen.getByRole('button', { name: 'Remove status filter Pending' }));
    expect(new URLSearchParams(window.location.search).get('foo')).toBe('bar');
  });

  it('resets page when the sort is changed with the Sort by select', async () => {
    window.history.replaceState({}, '', '/?page=3');
    renderApp();
    await waitForLibrary();

    fireEvent.change(getSortSelect(), { target: { value: 'Year|Ascending' } });
    let params = new URLSearchParams(window.location.search);
    expect(params.get('sort')).toBe('Year');
    expect(params.get('page') ?? '1').toBe('1');
    expect(screen.getByText('Sort: Year (oldest)')).toBeInTheDocument();

    fireEvent.change(getSortSelect(), { target: { value: 'Year|Descending' } });
    params = new URLSearchParams(window.location.search);
    expect(params.get('direction')).toBe('Descending');
    expect(screen.getByText('Sort: Year (newest)')).toBeInTheDocument();
  });

  it('sorts by Added from the Sort by select and shows the summary', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    fireEvent.change(getSortSelect(), { target: { value: 'Added|Descending' } });
    let params = new URLSearchParams(window.location.search);
    expect(params.get('sort')).toBe('Added');
    expect(params.get('direction')).toBe('Descending');
    expect(screen.getByText('Sort: Added (newest)')).toBeInTheDocument();

    fireEvent.change(getSortSelect(), { target: { value: 'Added|Ascending' } });
    params = new URLSearchParams(window.location.search);
    expect(params.get('sort')).toBe('Added');
    expect(screen.getByText('Sort: Added (oldest)')).toBeInTheDocument();
  });

  it('restores the query on Back and Forward navigation', async () => {
    window.history.replaceState({}, '', '/');
    renderApp();
    await waitForLibrary();

    fireEvent.change(getSortSelect(), { target: { value: 'Year|Ascending' } });
    expect(new URLSearchParams(window.location.search).get('sort')).toBe('Year');
    expect(screen.getByText('Sort: Year (oldest)')).toBeInTheDocument();

    window.history.replaceState({}, '', '/');
    window.dispatchEvent(new PopStateEvent('popstate'));
    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('sort')).toBeNull();
    });
    expect(screen.getByText('Sort: Title (A-Z)')).toBeInTheDocument();
  });

  it('shows a Director tag with the director name and clears only that filter', async () => {
    window.history.replaceState({}, '', '/?directorId=1&statuses=Pending');
    renderApp();
    await waitForLibrary();

    expect(
      await screen.findByText('Denis Villeneuve', { selector: '.lamu-tag-value' }),
    ).toBeInTheDocument();
    fireEvent.click(
      screen.getByRole('button', { name: 'Remove director filter Denis Villeneuve' }),
    );
    const params = new URLSearchParams(window.location.search);
    expect(params.get('directorId') ?? '').toBe('');
    expect(params.get('statuses')).toBe('Pending');
    expect(queryTagValue('Denis Villeneuve')).not.toBeInTheDocument();
  });

  it('shows an IMDb tag for an active rating range and clears both bounds', async () => {
    window.history.replaceState({}, '', '/?minRating=7.5&maxRating=9&statuses=Pending');
    renderApp();
    await waitForLibrary();

    expect(tagValue('7.5 to 9.0')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Remove IMDb rating filter' }));
    const params = new URLSearchParams(window.location.search);
    expect(params.get('minRating')).toBeNull();
    expect(params.get('maxRating')).toBeNull();
    expect(params.get('statuses')).toBe('Pending');
    expect(queryTagValue('7.5 to 9.0')).not.toBeInTheDocument();
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

  it('opens the panel from the toggle and keeps the tags visible', async () => {
    window.history.replaceState({}, '', '/?statuses=Pending');
    const { container } = renderApp();
    await waitForLibrary();

    expect(screen.getByRole('list', { name: 'Active filters' })).toBeInTheDocument();

    openFilters();
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'true');
    const panel = container.querySelector('.filter-collapse');
    expect(panel).toHaveAttribute('data-open', 'true');
    expect(panel).not.toHaveAttribute('inert');
    expect(screen.getByRole('list', { name: 'Active filters' })).toBeInTheDocument();

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

  it('has no Collapse button and no match-count footer in the rail', async () => {
    window.history.replaceState({}, '', '/');
    const { container } = renderApp();
    await waitForLibrary();

    openFilters();
    expect(screen.queryByRole('button', { name: 'Collapse' })).not.toBeInTheDocument();
    expect(container.querySelector('.lamu-filter-footer')).toBeNull();

    fireEvent.click(getFiltersToggle());
    expect(getFiltersToggle()).toHaveAttribute('aria-expanded', 'false');
    expect(container.querySelector('.filter-collapse')).toHaveAttribute('inert');
  });

  it('clears URL-sourced filters from the tag list with Clear all', async () => {
    window.history.replaceState({}, '', '/?text=dune&statuses=Pending&foo=bar');
    const { container } = renderApp();
    await waitForLibrary();

    const tagRow = container.querySelector('.library-tags');
    expect(tagRow).not.toBeNull();
    fireEvent.click(within(tagRow as HTMLElement).getByRole('button', { name: 'Clear all' }));

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

describe('Library collection filter', () => {
  it('shows a removable Collection tag for a known id and clears the page', async () => {
    window.history.replaceState({}, '', '/?collectionId=1&page=3&sort=Year');
    renderApp();
    await waitForLibrary();

    expect(
      await screen.findByText('Dune Collection', { selector: '.lamu-tag-value' }),
    ).toBeInTheDocument();
    fireEvent.click(
      screen.getByRole('button', { name: 'Remove collection filter Dune Collection' }),
    );

    const params = new URLSearchParams(window.location.search);
    expect(params.get('collectionId')).toBeNull();
    expect(params.get('sort')).toBe('Year');
    expect(params.get('page') ?? '1').toBe('1');
    expect(queryTagValue('Dune Collection')).not.toBeInTheDocument();
  });

  it('selects a collection through the panel, preserving other filters and resetting page', async () => {
    window.history.replaceState({}, '', '/?page=2&sort=Year&text=dune');
    renderApp();
    await waitForLibrary();

    openFilters();
    fireEvent.click(await screen.findByRole('checkbox', { name: /Dune Collection/ }));

    const params = new URLSearchParams(window.location.search);
    expect(params.get('collectionId')).toBe('1');
    expect(params.get('text')).toBe('dune');
    expect(params.get('sort')).toBe('Year');
    expect(params.get('page') ?? '1').toBe('1');
    expect(screen.getByRole('checkbox', { name: /Dune Collection/ })).toBeChecked();
  });

  it('keeps the unknown-id fallback tag during fact loading', async () => {
    server.use(
      http.get('*/api/collections', async () => {
        await delay(100);
        return HttpResponse.json([]);
      }),
    );
    window.history.replaceState({}, '', '/?collectionId=999');
    renderApp();

    expect(tagValue('Collection 999')).toBeInTheDocument();
    await waitForLibrary();
    expect(tagValue('Collection 999')).toBeInTheDocument();
  });

  it('keeps the unknown-id fallback tag on an empty facet and hides the healthy-empty group', async () => {
    server.use(http.get('*/api/collections', () => HttpResponse.json([])));
    window.history.replaceState({}, '', '/?collectionId=999');
    renderApp();
    await waitForLibrary();

    expect(
      await screen.findByText('Collection 999', { selector: '.lamu-tag-value' }),
    ).toBeInTheDocument();

    openFilters();
    expect(tagValue('Collection 999')).toBeInTheDocument();
    expect(screen.queryByRole('group', { name: 'Collection' })).not.toBeInTheDocument();
    expect(new URLSearchParams(window.location.search).get('collectionId')).toBe('999');
  });

  it('keeps the unknown-id fallback tag and removes only the collection key', async () => {
    server.use(http.get('*/api/collections', () => HttpResponse.json([])));
    window.history.replaceState({}, '', '/?collectionId=999&sort=Year');
    renderApp();
    await waitForLibrary();

    fireEvent.click(
      screen.getByRole('button', { name: 'Remove collection filter Collection 999' }),
    );
    const params = new URLSearchParams(window.location.search);
    expect(params.get('collectionId')).toBeNull();
    expect(params.get('sort')).toBe('Year');
  });

  it('keeps the unknown-id fallback tag when the facet fails', async () => {
    server.use(
      http.get('*/api/collections', () =>
        HttpResponse.json({ title: 'HTTP 500', status: 500 }, { status: 500 }),
      ),
    );
    window.history.replaceState({}, '', '/?collectionId=999');
    renderApp();
    await waitForLibrary();

    expect(
      await screen.findByText('Collection 999', { selector: '.lamu-tag-value' }),
    ).toBeInTheDocument();
    openFilters();
    expect(screen.getByText(/Collections could not be loaded/i)).toBeInTheDocument();
  });

  it('clears the collection filter through Clear all while keeping unknown keys', async () => {
    window.history.replaceState({}, '', '/?collectionId=1&foo=bar&page=2');
    const { container } = renderApp();
    await waitForLibrary();
    await screen.findByText('Dune Collection', { selector: '.lamu-tag-value' });

    const tagRow = container.querySelector('.library-tags');
    fireEvent.click(within(tagRow as HTMLElement).getByRole('button', { name: 'Clear all' }));

    const params = new URLSearchParams(window.location.search);
    expect(params.get('collectionId')).toBeNull();
    expect(params.get('foo')).toBe('bar');
    expect(screen.queryByRole('list', { name: 'Active filters' })).not.toBeInTheDocument();
  });

  it('renders hostile collection names as plain text in the toolbar tag', async () => {
    server.use(http.get('*/api/collections', () => HttpResponse.json(hostileCollections)));
    window.history.replaceState({}, '', '/?collectionId=101');
    const { container } = renderApp();
    await waitForLibrary();

    expect(
      await screen.findByText(hostileCollections[0].name, { selector: '.lamu-tag-value' }),
    ).toBeInTheDocument();
    const tagRow = container.querySelector('.library-tags');
    expect(tagRow?.querySelector('img')).toBeNull();
    expect(
      screen.getByRole('button', {
        name: `Remove collection filter ${hostileCollections[0].name}`,
      }),
    ).toBeInTheDocument();
  });

  it('renders an overlong collection name in a single toolbar tag', async () => {
    server.use(http.get('*/api/collections', () => HttpResponse.json(overlongCollections)));
    window.history.replaceState({}, '', '/?collectionId=102');
    const { container } = renderApp();
    await waitForLibrary();

    const tag = await screen.findByText(overlongCollections[0].name, {
      selector: '.lamu-tag-value',
    });
    expect(tag).toBeInTheDocument();
    const tagRow = container.querySelector('.library-tags');
    expect(tagRow?.querySelectorAll('.lamu-tag')).toHaveLength(1);
  });

  it('clips an overlong filter value inside the tag instead of widening the page', async () => {
    // arrange
    const here = dirname(fileURLToPath(import.meta.url));
    const browseCss = readFileSync(join(here, 'browse.css'), 'utf8');
    server.use(http.get('*/api/collections', () => HttpResponse.json(overlongCollections)));
    window.history.replaceState({}, '', '/?collectionId=102');

    // act
    renderApp();
    await waitForLibrary();

    // assert
    const tag = await screen.findByText(overlongCollections[0].name, {
      selector: '.lamu-tag-value',
    });
    expect(tag).toBeInTheDocument();
    expect(browseCss).toMatch(/\.lamu-tag-value\s*\{[^}]*text-overflow:\s*ellipsis/);
    expect(browseCss).toMatch(/\.library-tag-list\s*>\s*li\s*\{[^}]*max-width:\s*100%/);
  });
});
