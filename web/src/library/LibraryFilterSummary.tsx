import { CloseIcon } from '../ui/icons';
import {
  isRatingActive,
  RATING_MIN_EXTENT,
  RATING_MAX_EXTENT,
  RUNTIME_MAX_EXTENT,
  RUNTIME_MIN_EXTENT,
  YEAR_MIN_EXTENT,
  type FilterQuery,
} from '../features/library/filterQuery';

export interface FilterChipClear {
  /** Applies this chip's removal to a copy of the current query. */
  clear: (next: FilterQuery) => void;
}

export interface FilterChip extends FilterChipClear {
  key: string;
  kind: string;
  value: string;
  removeLabel: string;
}

export interface FilterNameLookups {
  genreNameById: ReadonlyMap<string, string>;
  actorNameById: ReadonlyMap<string, string>;
  directorNameById: ReadonlyMap<string, string>;
  collectionNameById: ReadonlyMap<string, string>;
}

// Q12 requires the pure chip builder to live in this ratified module, so the
// file intentionally shares a component and pure helpers.
// eslint-disable-next-line react-refresh/only-export-components -- Q12 chip builder
export function hasDirector(query: FilterQuery): boolean {
  return query.directorId != null && String(query.directorId) !== '';
}

function boundActive(raw: unknown, extent: number): string | null {
  if (raw == null || String(raw).trim() === '') {
    return null;
  }
  const number = Number(String(raw).trim());
  if (Number.isFinite(number) && number === extent) {
    return null;
  }
  return String(raw).trim();
}

function ratingBoundText(raw: unknown, fallback: number): string {
  const number = raw == null || String(raw).trim() === '' ? fallback : Number(raw);
  return (Number.isFinite(number) ? number : fallback).toFixed(1);
}

function isUnknownRuntimeIncluded(raw: unknown): boolean {
  return String(raw ?? '').toLowerCase() === 'true';
}

interface MultiValueSpec {
  kind: string;
  field: 'genreIds' | 'actorIds' | 'statuses';
  nameById: ReadonlyMap<string, string> | null;
  noun: string;
  /** Applies removal for one value; the shared factory supplies the match/rebuild. */
  clear: (next: FilterQuery, value: string) => void;
}

function multiValueChips(
  chips: FilterChip[],
  values: Array<string | number>,
  spec: MultiValueSpec,
) {
  for (const [index, raw] of values.entries()) {
    const value = String(raw);
    const name = spec.nameById?.get(value) || value;
    chips.push({
      key: `${spec.field === 'statuses' ? 'status' : spec.field.replace('Ids', '')}-${value}-${index}`,
      kind: spec.kind,
      value: name,
      removeLabel: `Remove ${spec.noun} filter ${name}`,
      clear: (next) => spec.clear(next, value),
    });
  }
}

function withoutFirst(list: Array<string | number>, value: string): Array<string | number> {
  const position = list.map(String).indexOf(value);
  if (position < 0) {
    return list;
  }
  return list.filter((_, index) => index !== position);
}

function present(chips: Array<FilterChip | null>): FilterChip[] {
  return chips.filter((chip): chip is FilterChip => chip !== null);
}

interface BoundChipSpec {
  key: string;
  kind: string;
  raw: unknown;
  extent: number;
  text: (value: string) => string;
  removeLabel: (value: string) => string;
  clear: (next: FilterQuery) => void;
}

function pushBoundChip(chips: FilterChip[], spec: BoundChipSpec): void {
  const value = boundActive(spec.raw, spec.extent);
  if (value === null) {
    return;
  }
  chips.push({
    key: spec.key,
    kind: spec.kind,
    value: spec.text(value),
    removeLabel: spec.removeLabel(value),
    clear: spec.clear,
  });
}

function flagChip(
  chips: FilterChip[],
  enabled: boolean,
  chip: Omit<FilterChip, 'clear'>,
  clear: (next: FilterQuery) => void,
): void {
  if (!enabled) {
    return;
  }
  chips.push({ ...chip, clear });
}

function directorChip(query: FilterQuery, names: FilterNameLookups): FilterChip | null {
  if (!hasDirector(query)) {
    return null;
  }
  const value = String(query.directorId);
  const name = names.directorNameById.get(value) || `Director ${value}`;
  return {
    key: 'director',
    kind: 'Director',
    value: name,
    removeLabel: `Remove director filter ${name}`,
    clear: (next) => {
      next.directorId = '';
    },
  };
}

function collectionChip(query: FilterQuery, names: FilterNameLookups): FilterChip | null {
  if (query.collectionId == null) {
    return null;
  }
  const value = String(query.collectionId);
  const name = names.collectionNameById.get(value) ?? `Collection ${value}`;
  return {
    key: 'collection',
    kind: 'Collection',
    value: name,
    removeLabel: `Remove collection filter ${name}`,
    clear: (next) => {
      next.collectionId = null;
    },
  };
}

function ratingChip(query: FilterQuery): FilterChip | null {
  if (!isRatingActive(query.minRating, query.maxRating)) {
    return null;
  }
  const range = `${ratingBoundText(query.minRating, RATING_MIN_EXTENT)} to ${ratingBoundText(query.maxRating, RATING_MAX_EXTENT)}`;
  return {
    key: 'imdbRating',
    kind: 'IMDb',
    value: range,
    removeLabel: 'Remove IMDb rating filter',
    clear: (next) => {
      next.minRating = null;
      next.maxRating = null;
    },
  };
}

/**
 * Pure chip construction: given the current query, the year snapshot and the
 * resolved facet names, returns one descriptor per removable filter value in a
 * stable order. Each descriptor carries the mutation that removes just that
 * value, so nothing here touches React state, history or the URL.
 */
// eslint-disable-next-line react-refresh/only-export-components -- Q12 chip builder
export function buildFilterChips(
  query: FilterQuery,
  currentYear: number,
  names: FilterNameLookups,
): FilterChip[] {
  const chips: FilterChip[] = [];

  multiValueChips(chips, query.genreIds ?? [], {
    kind: 'Genre',
    field: 'genreIds',
    nameById: names.genreNameById,
    noun: 'genre',
    clear: (next, value) => {
      next.genreIds = withoutFirst(next.genreIds, value);
    },
  });

  multiValueChips(chips, query.actorIds ?? [], {
    kind: 'Cast',
    field: 'actorIds',
    nameById: names.actorNameById,
    noun: 'cast',
    clear: (next, value) => {
      next.actorIds = withoutFirst(next.actorIds, value);
    },
  });

  chips.push(...present([directorChip(query, names), collectionChip(query, names)]));

  pushBoundChip(chips, {
    key: 'runtimeMin',
    kind: 'Runtime',
    raw: query.runtimeMin,
    extent: RUNTIME_MIN_EXTENT,
    text: (value) => `from ${value} min`,
    removeLabel: (value) => `Remove minimum runtime ${value} minutes`,
    clear: (next) => {
      next.runtimeMin = '';
    },
  });
  pushBoundChip(chips, {
    key: 'runtimeMax',
    kind: 'Runtime',
    raw: query.runtimeMax,
    extent: RUNTIME_MAX_EXTENT,
    text: (value) => `to ${value} min`,
    removeLabel: (value) => `Remove maximum runtime ${value} minutes`,
    clear: (next) => {
      next.runtimeMax = '';
    },
  });
  flagChip(
    chips,
    isUnknownRuntimeIncluded(query.runtimeIncludeUnknown),
    {
      key: 'runtimeIncludeUnknown',
      kind: 'Runtime',
      value: 'include unknown',
      removeLabel: 'Remove include unknown runtime filter',
    },
    (next) => {
      next.runtimeIncludeUnknown = false;
    },
  );
  pushBoundChip(chips, {
    key: 'yearMin',
    kind: 'Year',
    raw: query.yearMin,
    extent: YEAR_MIN_EXTENT,
    text: (value) => `from ${value}`,
    removeLabel: (value) => `Remove minimum year ${value}`,
    clear: (next) => {
      next.yearMin = '';
    },
  });
  pushBoundChip(chips, {
    key: 'yearMax',
    kind: 'Year',
    raw: query.yearMax,
    extent: currentYear,
    text: (value) => `to ${value}`,
    removeLabel: (value) => `Remove maximum year ${value}`,
    clear: (next) => {
      next.yearMax = '';
    },
  });

  chips.push(...present([ratingChip(query)]));

  multiValueChips(chips, query.statuses ?? [], {
    kind: 'Status',
    field: 'statuses',
    nameById: null,
    noun: 'status',
    clear: (next, value) => {
      next.statuses = withoutFirst(next.statuses, value) as string[];
    },
  });

  return chips;
}

export interface LibraryFilterSummaryProps {
  chips: readonly FilterChip[];
  onRemove: (chip: FilterChip) => void;
  onClearAll: () => void;
}

/** Presentation of the active-filter chip row; construction lives in the pure builder. */
export function LibraryFilterSummary({ chips, onRemove, onClearAll }: LibraryFilterSummaryProps) {
  if (chips.length === 0) {
    return null;
  }
  return (
    <div className="library-tags">
      <ul className="library-tag-list" aria-label="Active filters">
        {chips.map((chip) => (
          <li key={chip.key}>
            <span className="lamu-tag">
              <span className="lamu-tag-kind">{chip.kind}</span>
              <span className="lamu-tag-value">{chip.value}</span>
              <button
                type="button"
                className="lamu-tag-remove"
                aria-label={chip.removeLabel}
                onClick={() => onRemove(chip)}
              >
                <CloseIcon size={14} />
              </button>
            </span>
          </li>
        ))}
      </ul>
      <button type="button" className="lamu-link" onClick={onClearAll}>
        Clear all
      </button>
    </div>
  );
}

export default LibraryFilterSummary;
