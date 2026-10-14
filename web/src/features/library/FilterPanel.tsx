import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { components } from '../../api/types';
import {
  parseFilterQuery,
  toUrlQuery,
  currentYearExtent,
  RUNTIME_MIN_EXTENT,
  RUNTIME_MAX_EXTENT,
  YEAR_MIN_EXTENT,
} from './filterQuery';
import { useDebouncedSearch } from './useDebouncedSearch';

type FilterQuery = ReturnType<typeof parseFilterQuery>;
type GenreDto = components['schemas']['GenreDto'];
type PersonDto = components['schemas']['PersonDto'];

interface FilterPanelBaseProps {
  genres?: GenreDto[];
  genreOptions?: GenreDto[];
  genreFacets?: GenreDto[];
  actors?: PersonDto[];
  people?: PersonDto[];
  personOptions?: PersonDto[];
  cast?: PersonDto[];
  isLoadingGenres?: boolean;
  isLoadingActors?: boolean;
  genreError?: unknown;
  genresError?: unknown;
  genreQueryError?: unknown;
  castError?: unknown;
  actorsError?: unknown;
  peopleError?: unknown;
  personError?: unknown;
  disabled?: boolean;
  onClear?: () => void;
}

interface FilterPanelProps extends FilterPanelBaseProps {
  query?: unknown;
  value?: unknown;
  filterQuery?: unknown;
  filters?: unknown;
  initialQuery?: unknown;
  queryString?: string;
  url?: string;
  search?: string;
  hydration?: unknown;
  onChange?: (value: unknown) => void;
  onQueryChange?: (value: unknown) => void;
  onFilterChange?: (value: unknown) => void;
  onValueChange?: (value: unknown) => void;
  onHydrate?: (value: unknown) => void;
  onPopstate?: (value: unknown) => void;
}

const STATUS_WIRES = [
  { label: 'Pending', wire: 'Pending' },
  { label: 'Enriched', wire: 'Enriched' },
  { label: 'Not found', wire: 'NotFound' },
  { label: 'Failed', wire: 'Failed' },
];

const SORT_OPTIONS = [
  { label: 'Title (A-Z)', sort: 'Title', direction: 'Ascending' },
  { label: 'Title (Z-A)', sort: 'Title', direction: 'Descending' },
  { label: 'Year (oldest)', sort: 'Year', direction: 'Ascending' },
  { label: 'Year (newest)', sort: 'Year', direction: 'Descending' },
  { label: 'IMDb (lowest)', sort: 'Rating', direction: 'Ascending' },
  { label: 'IMDb (highest)', sort: 'Rating', direction: 'Descending' },
  { label: 'Runtime (shortest)', sort: 'Runtime', direction: 'Ascending' },
  { label: 'Runtime (longest)', sort: 'Runtime', direction: 'Descending' },
];

function toQuery(input: unknown): FilterQuery {
  if (input == null) {
    try {
      return parseFilterQuery(window.location.search);
    } catch {
      return parseFilterQuery({});
    }
  }
  if (typeof input === 'string') {
    if (input.trim() === '') {
      try {
        return parseFilterQuery(window.location.search);
      } catch {
        return parseFilterQuery({});
      }
    }
    try {
      return parseFilterQuery(input);
    } catch {
      return parseFilterQuery({});
    }
  }
  try {
    return parseFilterQuery(input);
  } catch {
    return parseFilterQuery({});
  }
}

function toNumberOrExtent(value: unknown, fallback: number): number {
  if (value == null || value === '') {
    return fallback;
  }
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function normalizeGenres(props: FilterPanelProps): GenreDto[] {
  const list = props.genres ?? props.genreOptions ?? props.genreFacets ?? props.actors ?? [];
  return Array.isArray(list) ? (list as GenreDto[]) : [];
}

function normalizeActors(props: FilterPanelProps): PersonDto[] {
  const list = props.actors ?? props.people ?? props.personOptions ?? props.cast ?? [];
  return Array.isArray(list) ? (list as PersonDto[]) : [];
}

export function FilterPanel(props: FilterPanelProps) {
  const controlledInput =
    props.query ??
    props.value ??
    props.filterQuery ??
    props.filters ??
    props.initialQuery ??
    props.queryString ??
    props.url ??
    props.search ??
    props.hydration;

  const controlledKey = useMemo(
    () => (controlledInput == null ? null : JSON.stringify(controlledInput)),
    [controlledInput],
  );
  const { onHydrate, onPopstate } = props;

  const [internalQuery, setInternalQuery] = useState<FilterQuery>(() => toQuery(controlledInput));
  const currentYear = useState(() => currentYearExtent(new Date()))[0];
  const query = controlledKey == null ? internalQuery : toQuery(controlledInput);

  const genres = normalizeGenres(props);
  const actors = normalizeActors(props);

  const callbacks = useMemo(
    () =>
      [props.onChange, props.onQueryChange, props.onFilterChange, props.onValueChange].filter(
        (fn): fn is (value: unknown) => void => typeof fn === 'function',
      ),
    [props.onChange, props.onQueryChange, props.onFilterChange, props.onValueChange],
  );

  const commit = useCallback(
    (next: FilterQuery) => {
      if (controlledKey == null) {
        setInternalQuery(next);
      }
      try {
        next.page = 1;
        const search = toUrlQuery(next, currentYear);
        const path = window.location.pathname || '/';
        window.history.replaceState(null, '', search ? `${path}?${search}` : path);
      } catch {
        // ignore in environments without history
      }
      callbacks.forEach((cb) => {
        try {
          cb(next);
        } catch {
          // ignore callback failures to keep UI responsive
        }
      });
    },
    [controlledKey, callbacks, currentYear],
  );

  useEffect(() => {
    if (controlledInput != null) {
      return;
    }
    const handler = () => {
      const next = toQuery(window.location.search);
      setInternalQuery(next);
      if (onHydrate) {
        try {
          onHydrate(next);
        } catch {
          // ignore
        }
      }
      if (onPopstate) {
        try {
          onPopstate(next);
        } catch {
          // ignore
        }
      }
    };
    window.addEventListener('popstate', handler);
    return () => window.removeEventListener('popstate', handler);
  }, [controlledInput, onHydrate, onPopstate]);

  const queryRef = useRef(query);
  useEffect(() => {
    queryRef.current = query;
  }, [query]);

  const [searchText, setSearchText] = useState(String(query.text ?? ''));
  const [prevQueryText, setPrevQueryText] = useState(query.text);
  if (query.text !== prevQueryText) {
    setPrevQueryText(query.text);
    setSearchText(String(query.text ?? ''));
  }
  const commitSearchText = useDebouncedSearch((value: string) => {
    const next = parseFilterQuery(queryRef.current);
    next.text = value;
    commit(next);
  });

  const [castFilter, setCastFilter] = useState('');

  const sortedGenres = useMemo(
    () => [...genres].sort((a, b) => String(a.name).localeCompare(String(b.name))),
    [genres],
  );

  const filteredActors = useMemo(() => {
    const needle = castFilter.trim().toLowerCase();
    const selected = new Set((query.actorIds ?? []).map(String));
    const ordered = [...actors].sort((a, b) => String(a.name).localeCompare(String(b.name)));
    if (!needle) {
      return ordered;
    }
    return ordered.filter(
      (actor) =>
        selected.has(String(actor.id)) || String(actor.name).toLowerCase().includes(needle),
    );
  }, [actors, castFilter, query.actorIds]);

  const runtimeMinValue = toNumberOrExtent(query.runtimeMin, RUNTIME_MIN_EXTENT);
  const runtimeMaxValue = toNumberOrExtent(query.runtimeMax, RUNTIME_MAX_EXTENT);
  const includeUnknown = Boolean(query.runtimeIncludeUnknown);
  const yearMinValue = toNumberOrExtent(query.yearMin, YEAR_MIN_EXTENT);
  const yearMaxValue = toNumberOrExtent(query.yearMax, currentYear);

  const handleGenreToggle = (id: unknown, checked: boolean) => {
    const next = parseFilterQuery(query);
    const target = String(id);
    next.genreIds = checked
      ? [...next.genreIds, id as never]
      : next.genreIds.filter((item) => String(item) !== target);
    commit(next);
  };

  const handleActorToggle = (id: unknown, checked: boolean) => {
    const next = parseFilterQuery(query);
    const target = String(id);
    next.actorIds = checked
      ? [...next.actorIds, id as never]
      : next.actorIds.filter((item) => String(item) !== target);
    commit(next);
  };

  const handleRuntimeMin = (value: string) => {
    const next = parseFilterQuery(query);
    next.runtimeMin = value;
    commit(next);
  };

  const handleRuntimeMax = (value: string) => {
    const next = parseFilterQuery(query);
    next.runtimeMax = value;
    commit(next);
  };

  const handleRuntimeUnknown = (checked: boolean) => {
    const next = parseFilterQuery(query);
    next.runtimeIncludeUnknown = checked;
    commit(next);
  };

  const handleYearMin = (value: string) => {
    const next = parseFilterQuery(query);
    next.yearMin = value;
    commit(next);
  };

  const handleYearMax = (value: string) => {
    const next = parseFilterQuery(query);
    next.yearMax = value;
    commit(next);
  };

  const handleStatusToggle = (wire: string, checked: boolean) => {
    const next = parseFilterQuery(query);
    next.statuses = checked
      ? [...next.statuses, wire]
      : next.statuses.filter((item) => item !== wire);
    commit(next);
  };

  const handleSortChange = (label: string) => {
    const option = SORT_OPTIONS.find((item) => item.label === label);
    if (!option) {
      return;
    }
    const next = parseFilterQuery(query);
    next.sort = option.sort;
    next.direction = option.direction;
    commit(next);
  };

  const handleClear = () => {
    const next = parseFilterQuery(query);
    next.text = '';
    next.genreIds = [];
    next.actorIds = [];
    next.runtimeMin = '';
    next.runtimeMax = '';
    next.runtimeIncludeUnknown = false;
    next.yearMin = '';
    next.yearMax = '';
    next.statuses = [];
    next.page = 1;
    commit(next);
    if (props.onClear) {
      try {
        props.onClear();
      } catch {
        // ignore
      }
    }
  };

  const selectedSortLabel =
    SORT_OPTIONS.find((item) => item.sort === query.sort && item.direction === query.direction)
      ?.label ?? SORT_OPTIONS[0].label;

  const selectedStatuses = new Set((query.statuses ?? []).map(String));

  return (
    <section aria-label="Filters">
      <fieldset>
        <legend>Search</legend>
        <label className="sr-only" htmlFor="lamu-search-titles">
          Search titles
          <input
            id="lamu-search-titles"
            type="search"
            placeholder="Search titles"
            value={searchText}
            onChange={(event) => {
              setSearchText(event.target.value);
              (commitSearchText as (value: unknown) => void)(event.target.value);
            }}
          />
        </label>
      </fieldset>

      <fieldset>
        <legend>Genres</legend>
        {props.genreError || props.genresError || props.genreQueryError ? (
          <p role="alert">Genres could not be loaded.</p>
        ) : (
          <ul>
            {sortedGenres.map((genre) => {
              const checked = (query.genreIds ?? []).map(String).includes(String(genre.id));
              return (
                <li key={String(genre.id)}>
                  <label>
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(event) => handleGenreToggle(genre.id, event.target.checked)}
                    />
                    <span>{genre.name}</span>
                    <span className="inline-block w-10 tabular-nums text-right" aria-hidden="true">
                      {genre.movieCount}
                    </span>
                    <span className="sr-only">, {genre.movieCount} movies</span>
                  </label>
                </li>
              );
            })}
          </ul>
        )}
      </fieldset>

      <fieldset>
        <legend>Cast</legend>
        <label className="sr-only" htmlFor="lamu-filter-cast">
          Filter cast
          <input
            id="lamu-filter-cast"
            type="search"
            placeholder="Filter cast"
            value={castFilter}
            onChange={(event) => setCastFilter(event.target.value)}
          />
        </label>
        {props.castError || props.actorsError || props.peopleError || props.personError ? (
          <p role="alert">Cast could not be loaded.</p>
        ) : (
          <ul>
            {filteredActors.map((actor) => {
              const checked = (query.actorIds ?? []).map(String).includes(String(actor.id));
              return (
                <li key={String(actor.id)}>
                  <label>
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(event) => handleActorToggle(actor.id, event.target.checked)}
                    />
                    <span>{actor.name}</span>
                  </label>
                </li>
              );
            })}
          </ul>
        )}
      </fieldset>

      <fieldset>
        <legend>Runtime</legend>
        <label className="sr-only" htmlFor="lamu-runtime-min">
          Runtime minimum (minutes)
          <input
            id="lamu-runtime-min"
            type="range"
            min={RUNTIME_MIN_EXTENT}
            max={RUNTIME_MAX_EXTENT}
            step={5}
            value={runtimeMinValue}
            onChange={(event) => handleRuntimeMin(event.target.value)}
          />
        </label>
        <label className="sr-only" htmlFor="lamu-runtime-max">
          Runtime maximum (minutes)
          <input
            id="lamu-runtime-max"
            type="range"
            min={RUNTIME_MIN_EXTENT}
            max={RUNTIME_MAX_EXTENT}
            step={5}
            value={runtimeMaxValue}
            onChange={(event) => handleRuntimeMax(event.target.value)}
          />
        </label>
        <span aria-hidden="true">
          {runtimeMinValue} – {runtimeMaxValue >= RUNTIME_MAX_EXTENT ? '240+' : runtimeMaxValue} min
        </span>
        <label>
          <input
            type="checkbox"
            checked={includeUnknown}
            onChange={(event) => handleRuntimeUnknown(event.target.checked)}
          />
          Include unknown runtime
        </label>
      </fieldset>

      <fieldset>
        <legend>Release year</legend>
        <label className="sr-only" htmlFor="lamu-year-min">
          Release year minimum
          <input
            id="lamu-year-min"
            type="range"
            min={YEAR_MIN_EXTENT}
            max={currentYear}
            step={1}
            value={yearMinValue}
            onChange={(event) => handleYearMin(event.target.value)}
          />
        </label>
        <label className="sr-only" htmlFor="lamu-year-max">
          Release year maximum
          <input
            id="lamu-year-max"
            type="range"
            min={YEAR_MIN_EXTENT}
            max={currentYear}
            step={1}
            value={yearMaxValue}
            onChange={(event) => handleYearMax(event.target.value)}
          />
        </label>
        <span aria-hidden="true">
          {yearMinValue} – {yearMaxValue}
        </span>
      </fieldset>

      <hr />

      <fieldset>
        <legend>Sort</legend>
        <label>
          Sort
          <select
            value={selectedSortLabel}
            onChange={(event) => handleSortChange(event.target.value)}
          >
            {SORT_OPTIONS.map((option) => (
              <option key={option.label} value={option.label}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </fieldset>

      <fieldset>
        <legend>Enrichment status</legend>
        {STATUS_WIRES.map((item) => (
          <label key={item.wire}>
            <input
              type="checkbox"
              checked={selectedStatuses.has(item.wire)}
              onChange={(event) => handleStatusToggle(item.wire, event.target.checked)}
            />
            {item.label}
          </label>
        ))}
      </fieldset>

      <button type="button" onClick={handleClear}>
        Clear filters
      </button>
    </section>
  );
}

export default FilterPanel;
