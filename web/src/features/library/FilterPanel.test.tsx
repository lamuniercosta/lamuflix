import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
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

  it('renders search placeholder and clear all button', () => {
    render(<FilterPanel />);

    expect(screen.getByPlaceholderText('Search titles')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear all/i })).toBeInTheDocument();
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

  it('filters cast suggestions by typed text and sorts them by name', () => {
    render(
      <FilterPanel
        genres={[]}
        actors={[person({ id: 2, name: 'Zoe' }), person({ id: 1, name: 'Zack' })]}
      />,
    );

    fireEvent.change(screen.getByLabelText('Add a cast member'), { target: { value: 'z' } });
    const zack = screen.getByRole('button', { name: 'Zack' });
    const zoe = screen.getByRole('button', { name: 'Zoe' });
    expect(zack.compareDocumentPosition(zoe) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    fireEvent.change(screen.getByLabelText('Add a cast member'), { target: { value: 'Zo' } });
    expect(screen.queryByRole('button', { name: 'Zack' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Zoe' })).toBeInTheDocument();
  });

  it('keeps selected cast members as chips while the search text changes', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={{ actorIds: [2] }}
        genres={[]}
        actors={[person({ id: 1, name: 'Aaron' }), person({ id: 2, name: 'Zoe' })]}
        onChange={onChange}
      />,
    );

    const selected = screen.getByRole('list', { name: 'Selected cast' });
    expect(within(selected).getByText('Zoe')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Add a cast member'), { target: { value: 'Aar' } });
    // selected actors persist in the list even when filtered from view
    expect(within(selected).getByText('Zoe')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Aaron' }));
    expect(onChange).toHaveBeenCalled();
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.actorIds.map(String)).toEqual(['2', '1']);
  });

  it('removes a selected cast member through its chip remove button', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={{ actorIds: [2] }}
        genres={[]}
        actors={[person({ id: 2, name: 'Zoe' })]}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Remove Zoe from cast filter' }));
    const last = onChange.mock.calls.at(-1)?.[0];
    expect(last.actorIds.map(String)).not.toContain('2');
  });

  it('hydrates controls from the URL on mount', () => {
    window.history.replaceState({}, '', '/?text=hello&sort=Year&direction=Descending&page=3');

    render(<FilterPanel />);

    expect(screen.getByPlaceholderText('Search titles')).toHaveValue('hello');
    expect(screen.getByRole('button', { name: 'Year' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Descending' })).toBeInTheDocument();
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

    const clearButton = screen.getByRole('button', { name: /clear all/i });
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

  it('preserves sort and direction after clear all', async () => {
    render(
      <FilterPanel
        query={parseFilterQuery('sort=Year&direction=Descending&text=x')}
        onChange={() => {}}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: /clear all/i }));
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Year' })).toHaveAttribute('aria-pressed', 'true');
    });
    expect(screen.getByRole('button', { name: 'Descending' })).toBeInTheDocument();
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

  it('sets the sort field from its chip and toggles direction with the direction button', () => {
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    for (const field of ['Title', 'Year', 'Rating', 'Runtime']) {
      fireEvent.click(screen.getByRole('button', { name: field }));
      const last = onChange.mock.calls.at(-1)?.[0];
      expect(last.sort).toBe(field);
      expect(screen.getByRole('button', { name: field })).toHaveAttribute('aria-pressed', 'true');
    }
    expect(onChange).toHaveBeenCalledTimes(4);

    fireEvent.click(screen.getByRole('button', { name: 'Ascending' }));
    expect(onChange.mock.calls.at(-1)?.[0].direction).toBe('Descending');
    expect(onChange.mock.calls.at(-1)?.[0].sort).toBe('Runtime');

    fireEvent.click(screen.getByRole('button', { name: 'Descending' }));
    expect(onChange.mock.calls.at(-1)?.[0].direction).toBe('Ascending');
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
    expect(screen.getByLabelText('Add a cast member')).toBeInTheDocument();
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

  it('shows Collapse only when a handler is given and calls it', () => {
    const onCollapse = vi.fn();
    const { rerender } = render(<FilterPanel />);
    expect(screen.queryByRole('button', { name: 'Collapse' })).not.toBeInTheDocument();

    rerender(<FilterPanel onCollapse={onCollapse} />);
    fireEvent.click(screen.getByRole('button', { name: 'Collapse' }));
    expect(onCollapse).toHaveBeenCalledTimes(1);
  });

  it('reports the match count in the footer', () => {
    render(<FilterPanel matchCount={1} />);
    expect(screen.getByText('1 match')).toBeInTheDocument();
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

  it('cancels a pending text commit when Clear all is clicked', () => {
    vi.useFakeTimers();
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    fireEvent.change(screen.getByPlaceholderText('Search titles'), { target: { value: 'dune' } });
    act(() => {
      vi.advanceTimersByTime(100);
    });

    const callsBeforeClear = onChange.mock.calls.length;
    fireEvent.click(screen.getByRole('button', { name: /clear all/i }));
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

  it('renders the IMDb rating group with extent 0..10 step 0.1 and one-decimal captions', () => {
    render(<FilterPanel />);

    expect(screen.getByRole('group', { name: 'IMDb rating' })).toBeInTheDocument();
    const ratingMin = screen.getByLabelText('IMDb rating minimum');
    const ratingMax = screen.getByLabelText('IMDb rating maximum');

    expect(ratingMin).toHaveAttribute('type', 'range');
    expect(ratingMax).toHaveAttribute('type', 'range');
    expect(ratingMin).toHaveAttribute('min', '0');
    expect(ratingMin).toHaveAttribute('max', '10');
    expect(ratingMin).toHaveAttribute('step', '0.1');
    expect(ratingMax).toHaveAttribute('min', '0');
    expect(ratingMax).toHaveAttribute('max', '10');
    expect(ratingMax).toHaveAttribute('step', '0.1');
    expect(screen.getByText('0.0')).toBeInTheDocument();
    expect(screen.getByText('10.0')).toBeInTheDocument();
  });

  it('activates the rating group on the first thumb move and sends both bounds', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={{}} onChange={onChange} />);

    fireEvent.change(screen.getByLabelText('IMDb rating minimum'), {
      target: { value: '7.3' },
    });

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.minRating).toBe('7.3');
    expect(last.maxRating).toBe('10');
    expect(window.location.search).toBe('?minRating=7.3&maxRating=10');
  });

  it('normalizes slider float noise when a rating thumb moves', () => {
    render(<FilterPanel query={{}} />);

    fireEvent.change(screen.getByLabelText('IMDb rating minimum'), {
      target: { value: '7.300000000000001' },
    });

    expect(window.location.search).toBe('?minRating=7.3&maxRating=10');
  });

  it('clamps the rating thumbs so they cannot cross', () => {
    const onChange = vi.fn();
    render(<FilterPanel query={parseFilterQuery('minRating=3&maxRating=5')} onChange={onChange} />);

    fireEvent.change(screen.getByLabelText('IMDb rating minimum'), { target: { value: '9' } });

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.minRating).toBe('5');
    expect(last.maxRating).toBe('5');
  });

  function RatingHost({ onChange }: { onChange: (value: unknown) => void }) {
    const [q, setQ] = React.useState(parseFilterQuery('minRating=3&maxRating=8'));
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

  it('keeps the rating group active after returning to the full span', () => {
    const onChange = vi.fn();
    render(<RatingHost onChange={onChange} />);

    fireEvent.change(screen.getByLabelText('IMDb rating minimum'), { target: { value: '0' } });
    fireEvent.change(screen.getByLabelText('IMDb rating maximum'), { target: { value: '10' } });

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.minRating).toBe('0');
    expect(last.maxRating).toBe('10');
    expect(window.location.search).toBe('?minRating=0&maxRating=10');
  });

  it('hydrates an open-bound rating group without rewriting the URL', () => {
    window.history.replaceState({}, '', '/?minRating=7&maxRating=');
    render(<FilterPanel />);

    expect((screen.getByLabelText('IMDb rating minimum') as HTMLInputElement).value).toBe('7');
    expect((screen.getByLabelText('IMDb rating maximum') as HTMLInputElement).value).toBe('10');
    expect(window.location.search).toBe('?minRating=7&maxRating=');
  });

  it('materializes both finite bounds on an open-bound group after interaction', () => {
    window.history.replaceState({}, '', '/?minRating=7&maxRating=');
    render(<FilterPanel />);

    fireEvent.change(screen.getByLabelText('IMDb rating maximum'), { target: { value: '9' } });

    expect(window.location.search).toBe('?minRating=7&maxRating=9');
  });

  it.each([
    '?minRating=abc&maxRating=5',
    '?minRating=7',
    '?minRating=11&maxRating=10',
    '?minRating=7.00&maxRating=9',
    '?minRating=8&maxRating=7',
  ])('shows a safe full span for invalid rating input %s without URL rewrite', (search) => {
    window.history.replaceState({}, '', '/' + search);
    render(<FilterPanel />);

    expect((screen.getByLabelText('IMDb rating minimum') as HTMLInputElement).value).toBe('0');
    expect((screen.getByLabelText('IMDb rating maximum') as HTMLInputElement).value).toBe('10');
    expect(window.location.search).toBe(search);
  });

  it('resets page to 1 and writes the URL when a rating thumb moves', () => {
    window.history.replaceState({}, '', '/?page=3');
    const onChange = vi.fn();
    render(<FilterPanel onChange={onChange} />);

    fireEvent.change(screen.getByLabelText('IMDb rating minimum'), { target: { value: '7' } });

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.page).toBe(1);
    expect(window.location.search).toBe('?minRating=7&maxRating=10');
  });

  it('clears the rating group on Clear all', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={parseFilterQuery('text=dune&minRating=7&maxRating=9')}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /clear all/i }));

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.minRating).toBeNull();
    expect(last.maxRating).toBeNull();
    expect(window.location.search).toBe('');
  });
});

describe('FilterPanel collection section', () => {
  type CollectionDto = components['schemas']['CollectionDto'];

  function collection(value: Partial<CollectionDto>): CollectionDto {
    return { id: 1, name: 'Collection', movieCount: 0, ...value } as CollectionDto;
  }

  function CollectionHost({ onChange }: { onChange: (value: unknown) => void }) {
    const [q, setQ] = React.useState(parseFilterQuery(''));
    return (
      <FilterPanel
        query={q}
        collections={[
          collection({ id: 3, name: 'Dune Collection', movieCount: 2 }),
          collection({ id: 4, name: 'Arrival Collection', movieCount: 1 }),
        ]}
        genres={[genre({ id: 1, name: 'Action', movieCount: 4 })]}
        onChange={(next) => {
          onChange(next);
          setQ(next as ReturnType<typeof parseFilterQuery>);
        }}
      />
    );
  }

  it('renders the Collection fieldset with sorted sr-only checkboxes and count suffixes', () => {
    render(
      <FilterPanel
        collections={[
          collection({ id: 4, name: 'Zeta', movieCount: 1 }),
          collection({ id: 3, name: 'Alpha', movieCount: 2 }),
        ]}
      />,
    );

    const group = screen.getByRole('group', { name: 'Collection' });
    const alpha = within(group).getByRole('checkbox', { name: /Alpha/ });
    const zeta = within(group).getByRole('checkbox', { name: /Zeta/ });
    expect(alpha).toHaveAttribute('type', 'checkbox');
    expect(alpha).not.toHaveAttribute('checked');
    expect(alpha.compareDocumentPosition(zeta) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(group).getByText(', 2 movies')).toBeInTheDocument();
    expect(alpha.parentElement).toHaveClass('lamu-chip');
  });

  it('breaks equal names by id', () => {
    render(
      <FilterPanel
        collections={[
          collection({ id: 9, name: 'Same', movieCount: 1 }),
          collection({ id: 2, name: 'Same', movieCount: 2 }),
        ]}
      />,
    );

    const group = screen.getByRole('group', { name: 'Collection' });
    const boxes = within(group).getAllByRole('checkbox');
    expect(boxes[0]).toHaveAccessibleName(/2 movies/);
    expect(boxes[1]).toHaveAccessibleName(/1 movies/);
  });

  it('selects a single collection, writes the URL key and resets page', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={parseFilterQuery('page=3&text=dune')}
        collections={[collection({ id: 3, name: 'Dune Collection', movieCount: 2 })]}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole('checkbox', { name: /Dune Collection/ }));

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(String(last.collectionId)).toBe('3');
    expect(last.page).toBe(1);
    expect(last.text).toBe('dune');
    expect(new URLSearchParams(window.location.search).get('collectionId')).toBe('3');
    expect(new URLSearchParams(window.location.search).get('page')).toBeNull();
  });

  it('replaces the selection when another chip is checked', () => {
    const onChange = vi.fn();
    render(<CollectionHost onChange={onChange} />);

    fireEvent.click(screen.getByRole('checkbox', { name: /Dune Collection/ }));
    fireEvent.click(screen.getByRole('checkbox', { name: /Arrival Collection/ }));

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(String(last.collectionId)).toBe('4');
    expect(screen.getByRole('checkbox', { name: /Dune Collection/ })).not.toBeChecked();
    expect(screen.getByRole('checkbox', { name: /Arrival Collection/ })).toBeChecked();
    expect(new URLSearchParams(window.location.search).getAll('collectionId')).toEqual(['4']);
  });

  it('clears the selection when the active chip is unchecked', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={parseFilterQuery('collectionId=3&page=2')}
        collections={[collection({ id: 3, name: 'Dune Collection', movieCount: 2 })]}
        onChange={onChange}
      />,
    );

    const box = screen.getByRole('checkbox', { name: /Dune Collection/ });
    expect(box).toBeChecked();
    fireEvent.click(box);

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.collectionId).toBeNull();
    expect(last.page).toBe(1);
    expect(new URLSearchParams(window.location.search).get('collectionId')).toBeNull();
  });

  it('clears the collection selection on Clear all while preserving sort', () => {
    const onChange = vi.fn();
    render(
      <FilterPanel
        query={parseFilterQuery('collectionId=3&sort=Year&direction=Descending')}
        collections={[collection({ id: 3, name: 'Dune Collection', movieCount: 2 })]}
        onChange={onChange}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /clear all/i }));

    const last = onChange.mock.calls.at(-1)?.[0] as ReturnType<typeof parseFilterQuery>;
    expect(last.collectionId).toBeNull();
    expect(last.sort).toBe('Year');
    expect(new URLSearchParams(window.location.search).get('collectionId')).toBeNull();
  });

  it('renders legend only for an empty facet without helper copy or busy state', () => {
    const { container } = render(<FilterPanel collections={[]} />);

    const group = screen.getByRole('group', { name: 'Collection' });
    expect(within(group).queryAllByRole('checkbox')).toHaveLength(0);
    expect(group.querySelector('p')).toBeNull();
    expect(group).toHaveTextContent(/^Collection$/);
    expect(container.querySelector('[aria-busy="true"]')).toBeNull();
    expect(container.querySelector('.lamu-skeleton')).toBeNull();
  });

  it('shows an independent collection error and keeps genres usable', () => {
    render(
      <FilterPanel
        collectionsError={new Error('collections failed')}
        genres={[genre({ id: 1, name: 'Action', movieCount: 4 })]}
      />,
    );

    expect(screen.getByText(/Collections could not be loaded/i)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /Action/ })).toBeInTheDocument();
  });

  it('renders hostile collection names as plain text', () => {
    const hostile = '<img src=x onerror=alert(1)> & "Dune"';
    const { container } = render(
      <FilterPanel collections={[collection({ id: 3, name: hostile, movieCount: 1 })]} />,
    );

    expect(screen.getByText(hostile)).toBeInTheDocument();
    expect(container.querySelector('img')).toBeNull();
    expect(container.querySelector('script')).toBeNull();
  });

  it('renders an overlong name inside a single chip', () => {
    const overlong = 'C'.repeat(180);
    render(<FilterPanel collections={[collection({ id: 3, name: overlong, movieCount: 1 })]} />);

    const group = screen.getByRole('group', { name: 'Collection' });
    expect(within(group).getAllByRole('checkbox')).toHaveLength(1);
    expect(group.querySelectorAll('.lamu-chip-row .lamu-chip')).toHaveLength(1);
    expect(within(group).getByText(overlong)).toBeInTheDocument();
  });
});
