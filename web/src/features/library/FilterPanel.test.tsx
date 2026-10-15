import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { act } from '@testing-library/react';
import type { components } from '../../api/types';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import FilterPanel from './FilterPanel';
import { currentYearExtent, parseFilterQuery, YEAR_MIN_EXTENT } from './filterQuery';

type GenreDto = components['schemas']['GenreDto'];
type PersonDto = components['schemas']['PersonDto'];

function genre(value: Partial<GenreDto>): GenreDto {
  return { id: 1, name: 'Genre', movieCount: 0, ...value } as GenreDto;
}

function person(value: Partial<PersonDto>): PersonDto {
  return { id: 1, name: 'Person', ...value } as PersonDto;
}

describe('FilterPanel', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/');
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders search placeholder and clear button', () => {
    render(<FilterPanel />);

    expect(screen.getByPlaceholderText('Search titles')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear filters/i })).toBeInTheDocument();
  });

  it('renders genre counts in a fixed badge and accessible suffix', () => {
    render(
      <FilterPanel
        genres={[{ id: 1, name: 'Action', movieCount: 42 } as components['schemas']['GenreDto']]}
      />,
    );

    expect(screen.getByText('Action')).toBeInTheDocument();
    expect(screen.getByText(/, 42 movies/i)).toBeInTheDocument();
  });

  it('sorts genre options by name', () => {
    render(
      <FilterPanel
        genres={[
          genre({ id: 2, name: 'Zoo', movieCount: 1 }),
          genre({ id: 1, name: 'Action', movieCount: 2 }),
        ]}
      />,
    );

    const action = screen.getByText('Action');
    const zoo = screen.getByText('Zoo');
    expect(action.compareDocumentPosition(zoo) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('sorts cast options by name and keeps local filtering', () => {
    render(
      <FilterPanel
        genres={[]}
        actors={[person({ id: 2, name: 'Zoe' }), person({ id: 1, name: 'Aaron' })]}
      />,
    );

    const aaron = screen.getByText('Aaron');
    const zoe = screen.getByText('Zoe');
    expect(aaron.compareDocumentPosition(zoe) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    fireEvent.change(screen.getByPlaceholderText('Filter cast'), { target: { value: 'Zo' } });
    expect(screen.queryByText('Aaron')).not.toBeInTheDocument();
    expect(screen.getByText('Zoe')).toBeInTheDocument();
  });

  it('persists cast selections when filter hides them from view', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={{ actorIds: [2] }}
        genres={[]}
        actors={[person({ id: 1, name: 'Aaron' }), person({ id: 2, name: 'Zoe' })]}
        onChange={onChange}
      />,
    );

    const zoeCheckbox = screen.getByRole('checkbox', { name: /Zoe/i });
    expect(zoeCheckbox).toBeChecked();

    fireEvent.change(screen.getByPlaceholderText('Filter cast'), { target: { value: 'Aar' } });
    // selected actors persist in the list even when filtered from view
    expect(screen.getByText('Zoe')).toBeInTheDocument();
    const zoe = screen.getByRole('checkbox', { name: /Zoe/i });
    expect(zoe).toBeChecked();

    const aaron = screen.getByRole('checkbox', { name: /Aaron/i });
    fireEvent.click(aaron);
    expect(onChange).toHaveBeenCalled();
  });

  it('hydrates controls from the URL on mount', () => {
    window.history.replaceState({}, '', '/?text=hello&sort=Year&direction=Descending&page=3');

    render(<FilterPanel />);

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('hello');
    expect((screen.getByRole('combobox') as HTMLSelectElement).value).toBe('Year (newest)');
  });

  it('restores controls on popstate', async () => {
    render(<FilterPanel />);

    window.history.replaceState({}, '', '/?text=updated');
    window.dispatchEvent(new PopStateEvent('popstate'));

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Search titles')).toHaveValue('updated');
    });
  });

  it('clears every panel key except sort and direction', async () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={parseFilterQuery(
          'text=x&genreIds=1&actorIds=2&runtimeMin=60&runtimeMax=120&runtimeIncludeUnknown=true&yearMin=2000&yearMax=2020&statuses=Pending&sort=Year&direction=Descending&page=3',
        )}
        genres={[genre({ id: 1, name: 'Action', movieCount: 2 })]}
        actors={[person({ id: 2, name: 'Zoe' })]}
        onChange={onChange}
      />,
    );

    const clearButton = screen.getByRole('button', { name: /clear filters/i });
    fireEvent.click(clearButton);

    expect(onChange).toHaveBeenCalled();
    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.text).toBe('');
    expect(last.genreIds).toEqual([]);
    expect(last.actorIds).toEqual([]);
    expect(last.runtimeMin === '' || last.runtimeMin == null).toBe(true);
    expect(last.runtimeMax === '' || last.runtimeMax == null).toBe(true);
    expect(last.runtimeIncludeUnknown).toBeFalsy();
    expect(last.yearMin === '' || last.yearMin == null).toBe(true);
    expect(last.yearMax === '' || last.yearMax == null).toBe(true);
    expect(last.statuses).toEqual([]);
    expect(last.page).toBe(1);
    expect(last.sort).toBe('Year');
    expect(last.direction).toBe('Descending');
  });

  it('preserves sort and direction selector after clear', async () => {
    render(
      <FilterPanel
        query={parseFilterQuery('sort=Year&direction=Descending&text=x')}
        onChange={() => {}}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: /clear filters/i }));
    await waitFor(() => {
      expect((screen.getByRole('combobox') as HTMLSelectElement).value).toBe('Year (newest)');
    });
  });

  it('exposes slider steps and extents', () => {
    render(<FilterPanel />);

    const runtimeMin = screen.getByLabelText('Runtime minimum (minutes)');
    const runtimeMax = screen.getByLabelText('Runtime maximum (minutes)');
    const yearMin = screen.getByLabelText('Release year minimum');
    const yearMax = screen.getByLabelText('Release year maximum');

    expect(runtimeMin).toHaveAttribute('min', '0');
    expect(runtimeMin).toHaveAttribute('max', '240');
    expect(runtimeMin).toHaveAttribute('step', '5');
    expect(runtimeMax).toHaveAttribute('min', '0');
    expect(runtimeMax).toHaveAttribute('max', '240');
    expect(runtimeMax).toHaveAttribute('step', '5');

    expect(yearMin).toHaveAttribute('min', '1900');
    expect(yearMax).toHaveAttribute('max', String(currentYearExtent()));
    expect(yearMax).toHaveAttribute('step', '1');
  });

  it('activates the runtime group when include-unknown is checked', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={{}} onChange={onChange} />);
    fireEvent.click(screen.getByLabelText(/Include unknown runtime/i));
    expect(onChange).toHaveBeenCalled();
    expect(onChange.mock.calls.at(-1)?.[0]?.runtimeIncludeUnknown).toBeTruthy();
  });

  it('sends all three runtime keys when a handle moves', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={{}} onChange={onChange} />);
    fireEvent.change(screen.getByLabelText('Runtime minimum (minutes)'), {
      target: { value: '60' },
    });
    expect(onChange).toHaveBeenCalled();
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.runtimeMin).toBe('60');
    expect(typeof last.runtimeIncludeUnknown).toBe('boolean');
  });

  it('keeps a valid 240+ runtime max with moved minimum', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={{ runtimeMax: 240 }} onChange={onChange} />);
    fireEvent.change(screen.getByLabelText('Runtime minimum (minutes)'), {
      target: { value: '60' },
    });
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.runtimeMax === 240 || last.runtimeMax === '240').toBe(true);
  });

  it('activates the runtime group for one-sided year ranges', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={{}} onChange={onChange} />);
    fireEvent.change(screen.getByLabelText('Release year minimum'), { target: { value: '2000' } });
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.yearMin).toBe('2000');
  });

  it('hydrates range controls from the URL', () => {
    window.history.replaceState(
      {},
      '',
      '/?runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false&yearMin=2000&yearMax=',
    );
    render(<FilterPanel />);
    expect((screen.getByLabelText('Runtime minimum (minutes)') as HTMLInputElement).value).toBe(
      '60',
    );
    expect((screen.getByLabelText('Release year minimum') as HTMLInputElement).value).toBe('2000');
  });

  it('captures the year extent at mount and keeps it while mounted', () => {
    vi.setSystemTime(new Date());
    const baseYear = currentYearExtent(new Date());
    const { unmount } = render(<FilterPanel />);
    const before = Number(
      (screen.getByLabelText('Release year maximum') as HTMLInputElement).getAttribute('max'),
    );
    expect(before).toBe(baseYear);

    vi.setSystemTime(new Date(baseYear + 1, 0, 2));
    const after = Number(
      (screen.getByLabelText('Release year maximum') as HTMLInputElement).getAttribute('max'),
    );
    expect(after).toBe(baseYear);
    unmount();
    vi.useRealTimers();
    vi.setSystemTime(new Date());
  });

  it('uses a new year extent on remount', () => {
    vi.setSystemTime(new Date());
    const baseYear = currentYearExtent();
    vi.setSystemTime(new Date(baseYear + 1, 0, 2));
    render(<FilterPanel />);
    const after = Number(
      (screen.getByLabelText('Release year maximum') as HTMLInputElement).getAttribute('max'),
    );
    expect(after).toBe(baseYear + 1);
    vi.useRealTimers();
    vi.setSystemTime(new Date());
  });

  it('maps all eight sort choices to matching sort and direction', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={{}} onChange={onChange} />);

    const select = screen.getByRole('combobox');
    const expected: Record<string, [string, string]> = {
      'Title (A-Z)': ['Title', 'Ascending'],
      'Title (Z-A)': ['Title', 'Descending'],
      'Year (oldest)': ['Year', 'Ascending'],
      'Year (newest)': ['Year', 'Descending'],
      'IMDb (lowest)': ['Rating', 'Ascending'],
      'IMDb (highest)': ['Rating', 'Descending'],
      'Runtime (shortest)': ['Runtime', 'Ascending'],
      'Runtime (longest)': ['Runtime', 'Descending'],
    };

    for (const [label, [sort, direction]] of Object.entries(expected)) {
      fireEvent.change(select, { target: { value: label } });
      const last = onChange.mock.calls.at(-1)?.[0];
      expect(last.sort).toBe(sort);
      expect(last.direction).toBe(direction);
    }
    expect(onChange).toHaveBeenCalledTimes(8);
  });

  it('toggles status chips and omits statuses when none selected', () => {
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    fireEvent.click(screen.getByLabelText(/^Pending$/i));
    let last = onChange.mock.calls.at(-1)?.[0];
    expect(last.statuses).toContain('Pending');

    fireEvent.click(screen.getByLabelText(/^Pending$/i));
    last = onChange.mock.calls.at(-1)?.[0];
    expect(last.statuses).not.toContain('Pending');
  });

  it('activates the runtime group with include-unknown alone', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={{ runtimeMin: 0, runtimeMax: 240, runtimeIncludeUnknown: false }}
        onChange={onChange}
      />,
    );
    fireEvent.click(screen.getByLabelText(/Include unknown runtime/i));
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.runtimeIncludeUnknown).toBeTruthy();
  });

  it('shows independent genre error and keeps cast usable', () => {
    render(
      <FilterPanel
        genreError={new Error('genres failed')}
        actors={[person({ id: 1, name: 'Zoe' })]}
      />,
    );
    expect(screen.getByText(/Genres could not be loaded/i)).toBeInTheDocument();
    expect(screen.getByText('Zoe')).toBeInTheDocument();
  });

  it('shows independent cast error and keeps genres usable', () => {
    render(
      <FilterPanel
        castError={new Error('cast failed')}
        genres={[genre({ id: 1, name: 'Action', movieCount: 4 })]}
      />,
    );
    expect(screen.getByText(/Cast could not be loaded/i)).toBeInTheDocument();
    expect(screen.getByText('Action')).toBeInTheDocument();
  });

  it('debounced text commit preserves a newer discrete filter selection', () => {
    vi.useFakeTimers();
    const commits: ReturnType<typeof parseFilterQuery>[] = [];
    function Host() {
      const [q, setQ] = React.useState(parseFilterQuery(''));
      return (
        <FilterPanel
          query={q}
          genres={[genre({ id: 1, name: 'Action', movieCount: 4 })]}
          onChange={(next) => {
            commits.push(next as ReturnType<typeof parseFilterQuery>);
            setQ(next as ReturnType<typeof parseFilterQuery>);
          }}
        />
      );
    }

    render(<Host />);
    const search = screen.getByPlaceholderText('Search titles');
    fireEvent.change(search, { target: { value: 'd' } });

    act(() => {
      vi.advanceTimersByTime(100);
    });

    fireEvent.click(screen.getByRole('checkbox', { name: /Action/i }));

    act(() => {
      vi.advanceTimersByTime(300);
    });

    const searchCommit = commits.find((c) => c.text === 'd');
    expect(searchCommit).toBeTruthy();
    expect(searchCommit!.genreIds.map(String)).toContain('1');
  });

  it('resets page to 1 on a discrete filter change', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={parseFilterQuery('page=3&text=dune')}
        genres={[genre({ id: 1, name: 'Action', movieCount: 4 })]}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole('checkbox', { name: /Action/i }));
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.page).toBe(1);
  });

  function ControlledHost({ onChange }: { onChange: (value: unknown) => void }) {
    const [q, setQ] = React.useState(parseFilterQuery(''));
    React.useEffect(() => {
      const handler = () => setQ(parseFilterQuery(window.location.search));
      window.addEventListener('popstate', handler);
      return () => window.removeEventListener('popstate', handler);
    }, []);
    return (
      <FilterPanel
        query={q}
        onChange={(next) => {
          onChange(next);
          setQ(next as ReturnType<typeof parseFilterQuery>);
        }}
      />
    );
  }

  it('cancels a pending text commit when Clear filters is clicked', () => {
    vi.useFakeTimers();
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'dune' } });
    act(() => {
      vi.advanceTimersByTime(100);
    });

    const callsBeforeClear = onChange.mock.calls.length;
    fireEvent.click(screen.getByRole('button', { name: /clear filters/i }));
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('');
    expect(window.location.search).toBe('');
    const lateCommits = onChange.mock.calls.slice(callsBeforeClear);
    expect(lateCommits.every((call) => (call[0] as { text?: string }).text !== 'dune')).toBe(true);
  });

  it('cancels a pending text commit on popstate navigation to a new text', () => {
    vi.useFakeTimers();
    const onChange = vi.fn();
    render(<ControlledHost onChange={onChange} />);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'stale' } });
    act(() => {
      vi.advanceTimersByTime(100);
    });

    window.history.replaceState({}, '', '/?text=restored');
    act(() => {
      window.dispatchEvent(new PopStateEvent('popstate'));
    });
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('restored');
    expect(window.location.search).toBe('?text=restored');
    expect(
      onChange.mock.calls.every((call) => (call[0] as { text?: string }).text !== 'stale'),
    ).toBe(true);
  });

  it('cancels a pending text commit on popstate navigation that leaves text unchanged', () => {
    vi.useFakeTimers();
    const onChange = vi.fn();
    render(<ControlledHost onChange={onChange} />);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'stale' } });
    act(() => {
      vi.advanceTimersByTime(100);
    });

    window.history.replaceState({}, '', '/?genreIds=1');
    act(() => {
      window.dispatchEvent(new PopStateEvent('popstate'));
    });
    act(() => {
      vi.advanceTimersByTime(300);
    });

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('');
    expect(window.location.search).toBe('?genreIds=1');
    expect(
      onChange.mock.calls.every((call) => (call[0] as { text?: string }).text !== 'stale'),
    ).toBe(true);
  });

  it('sends the complete runtime group and omits it back at the default extent', () => {
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    const runtimeMin = screen.getByLabelText('Runtime minimum (minutes)');
    fireEvent.change(runtimeMin, { target: { value: '60' } });
    let last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.runtimeMin).toBe('60');
    expect(last.runtimeMax).toBe('');
    expect(last.runtimeIncludeUnknown).toBe(false);
    expect(window.location.search).toBe('?runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false');

    fireEvent.change(runtimeMin, { target: { value: '0' } });
    last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.runtimeMin).toBe('0');
    expect(window.location.search).toBe('');
  });

  it('sends the runtime max key and omits the group back at the max extent', () => {
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    const runtimeMax = screen.getByLabelText('Runtime maximum (minutes)');
    fireEvent.change(runtimeMax, { target: { value: '180' } });
    expect(window.location.search).toBe('?runtimeMin=&runtimeMax=180&runtimeIncludeUnknown=false');

    fireEvent.change(runtimeMax, { target: { value: '240' } });
    expect(window.location.search).toBe('');
  });

  it('omits the runtime group when include-unknown is unchecked at default extents', () => {
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    fireEvent.click(screen.getByLabelText(/Include unknown runtime/i));
    expect(window.location.search).toBe('?runtimeMin=&runtimeMax=&runtimeIncludeUnknown=true');

    fireEvent.click(screen.getByLabelText(/Include unknown runtime/i));
    expect(window.location.search).toBe('');
  });

  it('sends both year keys and omits the group back at the year extents', () => {
    vi.setSystemTime(new Date());
    const baseYear = currentYearExtent(new Date());
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    const yearMin = screen.getByLabelText('Release year minimum');
    const yearMax = screen.getByLabelText('Release year maximum');

    fireEvent.change(yearMin, { target: { value: '2000' } });
    expect(window.location.search).toBe('?yearMin=2000&yearMax=');

    fireEvent.change(yearMax, { target: { value: '2010' } });
    expect(window.location.search).toBe('?yearMin=2000&yearMax=2010');

    fireEvent.change(yearMin, { target: { value: String(YEAR_MIN_EXTENT) } });
    expect(window.location.search).toBe('?yearMin=&yearMax=2010');

    fireEvent.change(yearMax, { target: { value: String(baseYear) } });
    expect(window.location.search).toBe('');

    vi.useRealTimers();
    vi.setSystemTime(new Date());
  });

  it('tracks all four status chips as ordered statuses keys and omits them when cleared', () => {
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    fireEvent.click(screen.getByLabelText(/^Not found$/i));
    fireEvent.click(screen.getByLabelText(/^Failed$/i));
    fireEvent.click(screen.getByLabelText(/^Pending$/i));
    fireEvent.click(screen.getByLabelText(/^Enriched$/i));

    let last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.statuses).toEqual(['NotFound', 'Failed', 'Pending', 'Enriched']);
    expect(window.location.search).toBe(
      '?statuses=NotFound&statuses=Failed&statuses=Pending&statuses=Enriched',
    );

    fireEvent.click(screen.getByLabelText(/^Not found$/i));
    fireEvent.click(screen.getByLabelText(/^Failed$/i));
    fireEvent.click(screen.getByLabelText(/^Pending$/i));
    fireEvent.click(screen.getByLabelText(/^Enriched$/i));

    last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.statuses).toEqual([]);
    expect(window.location.search).toBe('');
  });
});
