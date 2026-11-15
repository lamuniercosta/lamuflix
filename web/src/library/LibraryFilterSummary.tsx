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

  for (const [index, id] of (query.genreIds ?? []).entries()) {
    const value = String(id);
    const name = names.genreNameById.get(value) || value;
    chips.push({
      key: `genre-${value}-${index}`,
      kind: 'Genre',
      value: name,
      removeLabel: `Remove genre filter ${name}`,
      clear: (next) => {
        const position = next.genreIds.map(String).indexOf(value);
        if (position >= 0) {
          next.genreIds = next.genreIds.filter((_, itemIndex) => itemIndex !== position);
        }
      },
    });
  }

  for (const [index, id] of (query.actorIds ?? []).entries()) {
    const value = String(id);
    const name = names.actorNameById.get(value) || value;
    chips.push({
      key: `actor-${value}-${index}`,
      kind: 'Cast',
      value: name,
      removeLabel: `Remove cast filter ${name}`,
      clear: (next) => {
        const position = next.actorIds.map(String).indexOf(value);
        if (position >= 0) {
          next.actorIds = next.actorIds.filter((_, itemIndex) => itemIndex !== position);
        }
      },
    });
  }

  if (hasDirector(query)) {
    const value = String(query.directorId);
    const name = names.directorNameById.get(value) || `Director ${value}`;
    chips.push({
      key: 'director',
      kind: 'Director',
      value: name,
      removeLabel: `Remove director filter ${name}`,
      clear: (next) => {
        next.directorId = '';
      },
    });
  }

  if (query.collectionId != null) {
    const value = String(query.collectionId);
    const name = names.collectionNameById.get(value) ?? `Collection ${value}`;
    chips.push({
      key: 'collection',
      kind: 'Collection',
      value: name,
      removeLabel: `Remove collection filter ${name}`,
      clear: (next) => {
        next.collectionId = null;
      },
    });
  }

  const runtimeMin = boundActive(query.runtimeMin, RUNTIME_MIN_EXTENT);
  if (runtimeMin !== null) {
    chips.push({
      key: 'runtimeMin',
      kind: 'Runtime',
      value: `from ${runtimeMin} min`,
      removeLabel: `Remove minimum runtime ${runtimeMin} minutes`,
      clear: (next) => {
        next.runtimeMin = '';
      },
    });
  }

  const runtimeMax = boundActive(query.runtimeMax, RUNTIME_MAX_EXTENT);
  if (runtimeMax !== null) {
    chips.push({
      key: 'runtimeMax',
      kind: 'Runtime',
      value: `to ${runtimeMax} min`,
      removeLabel: `Remove maximum runtime ${runtimeMax} minutes`,
      clear: (next) => {
        next.runtimeMax = '';
      },
    });
  }

  if (isUnknownRuntimeIncluded(query.runtimeIncludeUnknown)) {
    chips.push({
      key: 'runtimeIncludeUnknown',
      kind: 'Runtime',
      value: 'include unknown',
      removeLabel: 'Remove include unknown runtime filter',
      clear: (next) => {
        next.runtimeIncludeUnknown = false;
      },
    });
  }

  const yearMin = boundActive(query.yearMin, YEAR_MIN_EXTENT);
  if (yearMin !== null) {
    chips.push({
      key: 'yearMin',
      kind: 'Year',
      value: `from ${yearMin}`,
      removeLabel: `Remove minimum year ${yearMin}`,
      clear: (next) => {
        next.yearMin = '';
      },
    });
  }

  const yearMax = boundActive(query.yearMax, currentYear);
  if (yearMax !== null) {
    chips.push({
      key: 'yearMax',
      kind: 'Year',
      value: `to ${yearMax}`,
      removeLabel: `Remove maximum year ${yearMax}`,
      clear: (next) => {
        next.yearMax = '';
      },
    });
  }

  if (isRatingActive(query.minRating, query.maxRating)) {
    const range = `${ratingBoundText(query.minRating, RATING_MIN_EXTENT)} to ${ratingBoundText(query.maxRating, RATING_MAX_EXTENT)}`;
    chips.push({
      key: 'imdbRating',
      kind: 'IMDb',
      value: range,
      removeLabel: 'Remove IMDb rating filter',
      clear: (next) => {
        next.minRating = null;
        next.maxRating = null;
      },
    });
  }

  for (const [index, status] of (query.statuses ?? []).entries()) {
    const value = String(status);
    chips.push({
      key: `status-${value}-${index}`,
      kind: 'Status',
      value,
      removeLabel: `Remove status filter ${value}`,
      clear: (next) => {
        const position = next.statuses.indexOf(status);
        if (position >= 0) {
          next.statuses = next.statuses.filter((_, itemIndex) => itemIndex !== position);
        }
      },
    });
  }

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
