import type { LibraryDirection, LibrarySort } from './filterQuery';

export interface FilterSortControlsProps {
  sort: string;
  direction: LibraryDirection;
  onSortChange: (field: LibrarySort) => void;
  onDirectionToggle: () => void;
}

const SORT_FIELDS: readonly LibrarySort[] = ['Title', 'Year', 'Rating', 'Runtime'];

/** Sort field and sort direction only; ranges and every other facet live elsewhere. */
export function FilterSortControls({
  sort,
  direction,
  onSortChange,
  onDirectionToggle,
}: FilterSortControlsProps) {
  return (
    <fieldset className="lamu-filter-group">
      <legend className="lamu-filter-legend">Sort</legend>
      <div className="lamu-chip-row">
        {SORT_FIELDS.map((field) => (
          <button
            key={field}
            type="button"
            className="lamu-chip"
            aria-pressed={sort === field}
            onClick={() => onSortChange(field)}
          >
            {field}
          </button>
        ))}
        <button type="button" className="lamu-chip" onClick={onDirectionToggle}>
          {direction}
        </button>
      </div>
      <p className="lamu-filter-note">Titles with no value for the chosen sort are listed last.</p>
    </fieldset>
  );
}

export default FilterSortControls;
