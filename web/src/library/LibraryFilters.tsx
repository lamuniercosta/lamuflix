import { useState } from 'react';
import { LIBRARY_DEFAULTS } from './libraryUrl';

type AnyRecord = Record<string, unknown>;
type LibraryUrlStateLike = Record<string, unknown>;

function isRecord(value: unknown): value is AnyRecord {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function pick(props: AnyRecord, names: readonly string[]): unknown {
  for (const name of names) {
    if (Object.prototype.hasOwnProperty.call(props, name) && props[name] !== undefined) {
      return props[name];
    }
  }
  return undefined;
}

function asArray(value: unknown): unknown[] {
  if (Array.isArray(value)) return value;
  if (value === null || value === undefined) return [];
  return [value];
}

function toNumberOrNull(value: unknown): number | null {
  if (value === null || value === undefined || value === '') return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

function parseIdListInput(raw: string): number[] {
  return raw
    .split(/[,\s]+/)
    .map((s) => s.trim())
    .filter((s) => s !== '' && /^-?\d+$/.test(s))
    .map((s) => Number(s));
}

function stringifyIds(value: unknown): string {
  if (Array.isArray(value)) return value.join(', ');
  if (value === null || value === undefined) return '';
  return String(value);
}

function coerceFunctions(value: unknown): ((...args: unknown[]) => void)[] {
  if (typeof value === 'function') return [value as (...args: unknown[]) => void];
  if (Array.isArray(value))
    return value.filter((item): item is (...args: unknown[]) => void => typeof item === 'function');
  return [];
}

function collectFunctions(
  props: AnyRecord,
  names: readonly string[],
): ((...args: unknown[]) => void)[] {
  const result: ((...args: unknown[]) => void)[] = [];
  for (const name of names) {
    result.push(...coerceFunctions(pick(props, [name])));
  }
  return result;
}

const STATE_ALIASES = [
  'state',
  'value',
  'values',
  'filters',
  'urlState',
  'browseState',
  'normalizedState',
  'initial',
  'initialState',
  'defaults',
];

function extractState(props: AnyRecord): LibraryUrlStateLike {
  let base: LibraryUrlStateLike = { ...LIBRARY_DEFAULTS };
  for (const alias of STATE_ALIASES) {
    const v = pick(props, [alias]);
    if (isRecord(v)) {
      base = { ...base, ...v };
    }
  }
  const mapping: [string[], string][] = [
    [['text', 'search', 'query', 'q', 'searchText', 'searchQuery'], 'text'],
    [['genreIds', 'genres', 'genre'], 'genreIds'],
    [['actorIds', 'actors', 'people', 'cast', 'personIds'], 'actorIds'],
    [['runtimeMin', 'minRuntime', 'runtime_min', 'runtimeMinimum'], 'runtimeMin'],
    [['runtimeMax', 'maxRuntime', 'runtime_max', 'runtimeMaximum'], 'runtimeMax'],
    [
      ['runtimeIncludeUnknown', 'includeUnknownRuntime', 'includeRuntimeUnknown', 'runtimeUnknown'],
      'runtimeIncludeUnknown',
    ],
    [['yearMin', 'minYear', 'year_min', 'fromYear', 'yearFrom'], 'yearMin'],
    [['yearMax', 'maxYear', 'year_max', 'toYear', 'yearTo'], 'yearMax'],
    [['statuses', 'status', 'enrichmentStatuses', 'enrichmentStatus'], 'statuses'],
    [['inWatchlist', 'watchlist', 'in_watchlist', 'watchlistState'], 'inWatchlist'],
    [['sort', 'sortBy', 'orderBy'], 'sort'],
    [['direction', 'sortDirection', 'orderDirection'], 'direction'],
    [['page', 'currentPage', 'pageNumber'], 'page'],
    [['pageSize', 'size', 'perPage', 'limit'], 'pageSize'],
  ];
  for (const [aliases, key] of mapping) {
    const v = pick(props, aliases);
    if (v !== undefined) {
      base[key] = v;
    }
  }
  return base;
}

export interface LibraryFiltersProps {
  [key: string]: unknown;
}

export default function LibraryFilters(props: LibraryFiltersProps) {
  const record = props as AnyRecord;
  const state = extractState(record);

  const applyHandlers = collectFunctions(record, [
    'applyFilters',
    'apply',
    'setFilters',
    'set',
    'onChange',
    'onApply',
    'onSubmit',
    'updateFilters',
    'onFiltersChange',
    'onSearch',
    'changeFilters',
    'handleApply',
  ]);
  const clearHandlers = collectFunctions(record, [
    'clearFilters',
    'clear',
    'resetFilters',
    'reset',
    'onClear',
    'onClearFilters',
    'handleClear',
  ]);

  const statuses = asArray(state.statuses).map(String) as string[];
  const toggleStatus = (name: string, checked: boolean) => {
    const next = checked ? [...statuses, name] : statuses.filter((s) => s !== name);
    const patch = { statuses: next, page: 1 };
    applyHandlers.forEach((fn) => {
      try {
        fn(patch);
      } catch {
        /* ignore */
      }
    });
  };

  function applyPatch(patch: AnyRecord) {
    applyHandlers.forEach((fn) => {
      try {
        fn({ ...patch, page: patch.page ?? 1 });
      } catch {
        /* ignore */
      }
    });
  }

  const [textDraft, setTextDraft] = useState(String(state.text ?? ''));

  const handleTextApply = () => {
    const value = textDraft.trim() === '' ? null : textDraft.trim();
    applyPatch({ text: value, page: 1 });
  };

  const statusNames: string[] = ['Pending', 'Enriched', 'NotFound', 'Failed'];

  return (
    <form
      className="library-filters"
      onSubmit={(e) => {
        e.preventDefault();
        handleTextApply();
      }}
    >
      <label>
        Search
        <input
          type="search"
          aria-label="Search"
          value={textDraft}
          onChange={(e) => setTextDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              handleTextApply();
            }
          }}
        />
      </label>
      <button type="submit">Search</button>
      <button type="button" onClick={handleTextApply}>
        Apply
      </button>

      <label>
        Sort
        <select
          aria-label="Sort"
          value={String(state.sort ?? 'Title')}
          onChange={(e) => applyPatch({ sort: e.target.value, page: 1 })}
        >
          {['Title', 'Year', 'Rating', 'Runtime'].map((v) => (
            <option key={v} value={v}>
              {v}
            </option>
          ))}
        </select>
      </label>

      <label>
        Sort direction
        <select
          aria-label="Direction"
          value={String(state.direction ?? 'Ascending')}
          onChange={(e) => applyPatch({ direction: e.target.value, page: 1 })}
        >
          <option value="Ascending">Ascending</option>
          <option value="Descending">Descending</option>
        </select>
      </label>

      <fieldset className="status-group">
        <legend>Enrichment status</legend>
        {statusNames.map((name) => (
          <label key={name}>
            <input
              type="checkbox"
              aria-label={name}
              checked={statuses.includes(name)}
              onChange={(e) => toggleStatus(name, e.target.checked)}
            />
            {name}
          </label>
        ))}
      </fieldset>

      <label>
        Watchlist
        <select
          aria-label="Watchlist"
          value={
            state.inWatchlist === null || state.inWatchlist === undefined
              ? ''
              : String(state.inWatchlist)
          }
          onChange={(e) => {
            const raw = e.target.value;
            const value = raw === '' ? null : raw === 'true';
            applyPatch({ inWatchlist: value, page: 1 });
          }}
        >
          <option value="">All</option>
          <option value="true">In watchlist</option>
          <option value="false">Not in watchlist</option>
        </select>
      </label>

      <label>
        Runtime min
        <input
          type="number"
          aria-label="Runtime min"
          value={
            state.runtimeMin === null || state.runtimeMin === undefined
              ? ''
              : String(state.runtimeMin)
          }
          onChange={(e) => applyPatch({ runtimeMin: toNumberOrNull(e.target.value), page: 1 })}
        />
      </label>
      <label>
        Runtime max
        <input
          type="number"
          aria-label="Runtime max"
          value={
            state.runtimeMax === null || state.runtimeMax === undefined
              ? ''
              : String(state.runtimeMax)
          }
          onChange={(e) => applyPatch({ runtimeMax: toNumberOrNull(e.target.value), page: 1 })}
        />
      </label>
      <label>
        <input
          type="checkbox"
          aria-label="Include unknown runtime"
          checked={Boolean(state.runtimeIncludeUnknown)}
          onChange={(e) => applyPatch({ runtimeIncludeUnknown: e.target.checked, page: 1 })}
        />
        Include unknown runtimes
      </label>

      <label>
        Year min
        <input
          type="number"
          aria-label="Year min"
          value={state.yearMin === null || state.yearMin === undefined ? '' : String(state.yearMin)}
          onChange={(e) => applyPatch({ yearMin: toNumberOrNull(e.target.value), page: 1 })}
        />
      </label>
      <label>
        Year max
        <input
          type="number"
          aria-label="Year max"
          value={state.yearMax === null || state.yearMax === undefined ? '' : String(state.yearMax)}
          onChange={(e) => applyPatch({ yearMax: toNumberOrNull(e.target.value), page: 1 })}
        />
      </label>

      <label>
        Genres
        <input
          type="text"
          aria-label="Genres"
          value={stringifyIds(state.genreIds)}
          onChange={(e) => applyPatch({ genreIds: parseIdListInput(e.target.value), page: 1 })}
        />
      </label>
      <label>
        Actors
        <input
          type="text"
          aria-label="Actors"
          value={stringifyIds(state.actorIds)}
          onChange={(e) => applyPatch({ actorIds: parseIdListInput(e.target.value), page: 1 })}
        />
      </label>

      <button
        type="button"
        onClick={() => {
          clearHandlers.forEach((fn) => {
            try {
              fn();
            } catch {
              /* ignore */
            }
          });
        }}
      >
        Clear filters
      </button>
    </form>
  );
}
