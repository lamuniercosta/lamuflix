import type { RefObject } from 'react';
import { DEFAULT_DIRECTION, DEFAULT_SORT, SORT_OPTIONS } from '../features/library/filterQuery';
import { AdjustmentsIcon, GridIcon, ListIcon } from '../ui/icons';
import type { LibraryVariant } from './LibraryTile';

const SORT_LABELS: Record<string, string> = Object.fromEntries(
  SORT_OPTIONS.map((option) => [`${option.sort}|${option.direction}`, option.label]),
);

const DEFAULT_SORT_VALUE = `${DEFAULT_SORT}|${DEFAULT_DIRECTION}`;

function sortValueOf(sort: unknown, direction: unknown): string {
  const value = `${String(sort)}|${String(direction)}`;
  return Object.prototype.hasOwnProperty.call(SORT_LABELS, value) ? value : DEFAULT_SORT_VALUE;
}

function sortLabel(sort: unknown, direction: unknown): string {
  return SORT_LABELS[sortValueOf(sort, direction)];
}

export interface LibraryResultsToolbarProps {
  toggleRef: RefObject<HTMLButtonElement | null>;
  panelOpen: boolean;
  panelId: string;
  groupCount: number;
  countText: string;
  sort: unknown;
  direction: unknown;
  variant: LibraryVariant;
  onTogglePanel: () => void;
  onSortChange: (value: string) => void;
  onVariantChange: (variant: LibraryVariant) => void;
}

/** Filters toggle, result count, sort select and layout switch. */
export function LibraryResultsToolbar({
  toggleRef,
  panelOpen,
  panelId,
  groupCount,
  countText,
  sort,
  direction,
  variant,
  onTogglePanel,
  onSortChange,
  onVariantChange,
}: LibraryResultsToolbarProps) {
  return (
    <div className="library-tools">
      <button
        ref={toggleRef}
        type="button"
        className="filters-toggle"
        aria-expanded={panelOpen}
        aria-controls={panelId}
        onClick={onTogglePanel}
      >
        <AdjustmentsIcon size={16} />
        <span>Filters</span>
        {groupCount > 0 ? (
          <>
            <span className="filters-toggle-badge" aria-hidden="true">
              {groupCount}
            </span>
            <span className="sr-only">
              {' '}
              {groupCount === 1 ? '1 filter group' : `${groupCount} filter groups`} active
            </span>
          </>
        ) : null}
      </button>
      <p className="library-count" role="status">
        {countText}
      </p>
      <label className="library-sort">
        <span className="library-sort-label">Sort by</span>
        <select
          className="library-sort-select"
          value={sortValueOf(sort, direction)}
          onChange={(event) => onSortChange(event.target.value)}
        >
          {SORT_OPTIONS.map((option) => (
            <option
              key={`${option.sort}|${option.direction}`}
              value={`${option.sort}|${option.direction}`}
            >
              {option.label}
            </option>
          ))}
        </select>
      </label>
      <span className="sr-only" aria-live="polite">
        Sort: {sortLabel(sort, direction)}
      </span>
      <div className="view-toggle" role="group" aria-label="Result layout">
        <button
          type="button"
          aria-pressed={variant === 'grid'}
          onClick={() => onVariantChange('grid')}
        >
          <GridIcon size={16} />
          <span>Grid</span>
        </button>
        <button
          type="button"
          aria-pressed={variant === 'list'}
          onClick={() => onVariantChange('list')}
        >
          <ListIcon size={16} />
          <span>List</span>
        </button>
      </div>
    </div>
  );
}

export default LibraryResultsToolbar;
