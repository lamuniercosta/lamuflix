import { useEffect, useId, useMemo, useRef, useState } from 'react';
import type { components } from '../../api/types';
import { CloseIcon } from '../../ui/icons';
import {
  currentYearExtent,
  parseFilterQuery,
  RUNTIME_MAX_EXTENT,
  RUNTIME_MIN_EXTENT,
  toUrlQuery,
  YEAR_MIN_EXTENT,
  type FilterQuery,
} from './filterQuery';
import { useDebouncedSearch } from './useDebouncedSearch';

type GenreDto = components['schemas']['GenreDto'];
type PersonDto = components['schemas']['PersonDto'];

export interface FilterPanelProps {
  query?: Partial<FilterQuery>;
  onChange?: (next: FilterQuery) => void;
  onQueryChange?: (next: FilterQuery) => void;
  onClear?: () => void;
  onCollapse?: () => void;
  matchCount?: number;
  genres?: GenreDto[];
  actors?: PersonDto[];
  genreError?: unknown;
  genresError?: unknown;
  castError?: unknown;
  actorsError?: unknown;
  integrated?: boolean;
  showTitleSearch?: boolean;
  showSort?: boolean;
  routeKey?: number;
}

const STATUS_OPTIONS = [
  { label: 'Pending', wire: 'Pending' },
  { label: 'Enriched', wire: 'Enriched' },
  { label: 'Not found', wire: 'NotFound' },
  { label: 'Failed', wire: 'Failed' },
] as const;

const SORT_FIELDS = ['Title', 'Year', 'Rating', 'Runtime'] as const;
const CAST_SUGGESTION_LIMIT = 8;

function readQuery(input: Partial<FilterQuery> | undefined): FilterQuery {
  return input === undefined ? parseFilterQuery(window.location.search) : parseFilterQuery(input);
}

function numberOr(value: unknown, fallback: number): number {
  if (value === null || value === undefined || value === '') {
    return fallback;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function sortByName<T extends { name?: string | null }>(items: readonly T[]): T[] {
  return [...items].sort((a, b) => String(a.name ?? '').localeCompare(String(b.name ?? '')));
}

interface RangeFieldProps {
  id: string;
  caption: string;
  accessibleName: string;
  valueText: string;
  min: number;
  max: number;
  step: number;
  value: number;
  onValueChange: (value: string) => void;
}

function RangeField({
  id,
  caption,
  accessibleName,
  valueText,
  min,
  max,
  step,
  value,
  onValueChange,
}: RangeFieldProps) {
  return (
    <div className="lamu-range-field">
      <div className="lamu-range-caption">
        <label htmlFor={id}>{caption}</label>
        <span aria-hidden="true">{valueText}</span>
      </div>
      <input
        id={id}
        type="range"
        className="lamu-range"
        aria-label={accessibleName}
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={(event) => onValueChange(event.target.value)}
      />
    </div>
  );
}

export function FilterPanel(props: FilterPanelProps) {
  const {
    query: queryProp,
    onChange,
    onQueryChange,
    onClear,
    onCollapse,
    matchCount,
    integrated = false,
    showTitleSearch = true,
    showSort = true,
    routeKey,
  } = props;
  const isControlled = queryProp !== undefined;
  const ids = useId();
  const searchId = `${ids}-search`;
  const castInputId = `${ids}-cast`;
  const castListId = `${ids}-cast-list`;
  const runtimeMinId = `${ids}-runtime-min`;
  const runtimeMaxId = `${ids}-runtime-max`;
  const yearMinId = `${ids}-year-min`;
  const yearMaxId = `${ids}-year-max`;
  const unknownId = `${ids}-unknown`;

  const [internalQuery, setInternalQuery] = useState<FilterQuery>(() => readQuery(queryProp));
  const [currentYear] = useState(() => currentYearExtent(new Date()));
  const query = isControlled ? readQuery(queryProp) : internalQuery;

  const sortedGenres = useMemo(() => sortByName(props.genres ?? []), [props.genres]);
  const sortedActors = useMemo(() => sortByName(props.actors ?? []), [props.actors]);
  const actorNameById = useMemo(
    () => new Map(sortedActors.map((actor) => [String(actor.id), String(actor.name ?? '')])),
    [sortedActors],
  );

  const commit = (next: FilterQuery) => {
    next.page = 1;
    if (!isControlled) {
      setInternalQuery(next);
    }
    if (!integrated) {
      try {
        const search = toUrlQuery(next, currentYear);
        const path = window.location.pathname || '/';
        window.history.replaceState(null, '', search ? `${path}?${search}` : path);
      } catch {
        // History is unavailable in some embedded environments; filtering still works.
      }
    }
    onChange?.(next);
    onQueryChange?.(next);
  };

  const update = (mutate: (next: FilterQuery) => void) => {
    const next = parseFilterQuery(query);
    mutate(next);
    commit(next);
  };

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
  const cancelSearchText = commitSearchText.cancel;

  useEffect(() => {
    cancelSearchText();
  }, [routeKey, cancelSearchText]);

  useEffect(() => {
    if (integrated) {
      return;
    }
    const handler = () => {
      cancelSearchText();
      const fromUrl = parseFilterQuery(window.location.search);
      setSearchText(String(fromUrl.text ?? ''));
      if (!isControlled) {
        setInternalQuery(fromUrl);
      }
    };
    window.addEventListener('popstate', handler);
    return () => window.removeEventListener('popstate', handler);
  }, [integrated, isControlled, cancelSearchText]);

  const [castText, setCastText] = useState('');

  const selectedGenreIds = new Set(query.genreIds.map(String));
  const selectedActorIds = new Set(query.actorIds.map(String));
  const selectedCast = query.actorIds.map((id) => ({
    id,
    name: actorNameById.get(String(id)) ?? `Unknown cast member ${String(id)}`,
  }));
  const castNeedle = castText.trim().toLowerCase();
  const castSuggestions =
    castNeedle === ''
      ? []
      : sortedActors
          .filter(
            (actor) =>
              !selectedActorIds.has(String(actor.id)) &&
              String(actor.name ?? '')
                .toLowerCase()
                .includes(castNeedle),
          )
          .slice(0, CAST_SUGGESTION_LIMIT);

  const runtimeMinValue = numberOr(query.runtimeMin, RUNTIME_MIN_EXTENT);
  const runtimeMaxValue = numberOr(query.runtimeMax, RUNTIME_MAX_EXTENT);
  const yearMinValue = numberOr(query.yearMin, YEAR_MIN_EXTENT);
  const yearMaxValue = numberOr(query.yearMax, currentYear);
  const selectedStatuses = new Set(query.statuses.map(String));
  const direction = query.direction === 'Descending' ? 'Descending' : 'Ascending';
  const genreFailed = Boolean(props.genreError ?? props.genresError);
  const castFailed = Boolean(props.castError ?? props.actorsError);
  const runtimeMaxText = runtimeMaxValue >= RUNTIME_MAX_EXTENT ? '240+' : String(runtimeMaxValue);

  const toggleGenre = (id: string | number, on: boolean) =>
    update((next) => {
      next.genreIds = on
        ? [...next.genreIds, id]
        : next.genreIds.filter((item) => String(item) !== String(id));
    });

  const addActor = (id: string | number) => {
    setCastText('');
    update((next) => {
      next.actorIds = [...next.actorIds, id];
    });
  };

  const removeActor = (id: string | number) =>
    update((next) => {
      next.actorIds = next.actorIds.filter((item) => String(item) !== String(id));
    });

  const toggleStatus = (wire: string, on: boolean) =>
    update((next) => {
      next.statuses = on ? [...next.statuses, wire] : next.statuses.filter((item) => item !== wire);
    });

  const setSort = (field: string) =>
    update((next) => {
      next.sort = field;
    });

  const toggleDirection = () =>
    update((next) => {
      next.direction = direction === 'Descending' ? 'Ascending' : 'Descending';
    });

  const handleClear = () => {
    cancelSearchText();
    setSearchText('');
    setCastText('');
    update((next) => {
      next.text = '';
      next.genreIds = [];
      next.actorIds = [];
      next.runtimeMin = '';
      next.runtimeMax = '';
      next.runtimeIncludeUnknown = false;
      next.yearMin = '';
      next.yearMax = '';
      next.statuses = [];
    });
    onClear?.();
  };

  return (
    <section className="lamu-filter-panel" aria-label="Filters">
      {showTitleSearch ? (
        <div className="lamu-filter-group">
          <label htmlFor={searchId} className="lamu-filter-sublabel">
            Search titles
          </label>
          <input
            id={searchId}
            type="search"
            className="lamu-field-input"
            placeholder="Search titles"
            value={searchText}
            onChange={(event) => {
              setSearchText(event.target.value);
              commitSearchText.schedule(event.target.value);
            }}
          />
        </div>
      ) : null}

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Genres</legend>
        {genreFailed ? (
          <p role="alert" className="lamu-filter-error">
            Genres could not be loaded.
          </p>
        ) : (
          <div className="lamu-chip-row">
            {sortedGenres.map((genre) => {
              const checked = selectedGenreIds.has(String(genre.id));
              return (
                <label
                  key={String(genre.id)}
                  className={checked ? 'lamu-chip lamu-chip-selected' : 'lamu-chip'}
                >
                  <input
                    type="checkbox"
                    className="sr-only"
                    checked={checked}
                    onChange={(event) => toggleGenre(genre.id, event.target.checked)}
                  />
                  <span>{genre.name}</span>
                  <span className="lamu-chip-count" aria-hidden="true">
                    {genre.movieCount}
                  </span>
                  <span className="sr-only">, {genre.movieCount} movies</span>
                </label>
              );
            })}
          </div>
        )}
      </fieldset>

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Cast</legend>
        {castFailed ? (
          <p role="alert" className="lamu-filter-error">
            Cast could not be loaded.
          </p>
        ) : (
          <>
            {selectedCast.length > 0 ? (
              <ul className="lamu-chip-row" aria-label="Selected cast">
                {selectedCast.map((member, index) => (
                  <li
                    key={`${String(member.id)}-${index}`}
                    className="lamu-chip lamu-chip-selected"
                  >
                    <span>{member.name}</span>
                    <button
                      type="button"
                      className="lamu-chip-remove"
                      aria-label={`Remove ${member.name} from cast filter`}
                      onClick={() => removeActor(member.id)}
                    >
                      <CloseIcon />
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
            <label htmlFor={castInputId} className="lamu-filter-sublabel">
              Add a cast member
            </label>
            <input
              id={castInputId}
              type="search"
              className="lamu-field-input"
              placeholder="Type a name"
              autoComplete="off"
              value={castText}
              aria-controls={castListId}
              onChange={(event) => setCastText(event.target.value)}
            />
            <div id={castListId} aria-live="polite">
              {castNeedle === '' ? null : castSuggestions.length > 0 ? (
                <ul className="lamu-chip-row" aria-label="Cast suggestions">
                  {castSuggestions.map((actor) => (
                    <li key={String(actor.id)}>
                      <button
                        type="button"
                        className="lamu-chip"
                        onClick={() => addActor(actor.id)}
                      >
                        {actor.name}
                      </button>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="lamu-filter-note">No matching cast members.</p>
              )}
            </div>
          </>
        )}
      </fieldset>

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Runtime</legend>
        <RangeField
          id={runtimeMinId}
          caption="Minimum"
          accessibleName="Runtime minimum (minutes)"
          valueText={`${runtimeMinValue} min`}
          min={RUNTIME_MIN_EXTENT}
          max={RUNTIME_MAX_EXTENT}
          step={5}
          value={runtimeMinValue}
          onValueChange={(value) =>
            update((next) => {
              next.runtimeMin = value;
            })
          }
        />
        <RangeField
          id={runtimeMaxId}
          caption="Maximum"
          accessibleName="Runtime maximum (minutes)"
          valueText={`${runtimeMaxText} min`}
          min={RUNTIME_MIN_EXTENT}
          max={RUNTIME_MAX_EXTENT}
          step={5}
          value={runtimeMaxValue}
          onValueChange={(value) =>
            update((next) => {
              next.runtimeMax = value;
            })
          }
        />
        <label htmlFor={unknownId} className="lamu-switch-row">
          <input
            id={unknownId}
            type="checkbox"
            className="sr-only"
            checked={Boolean(query.runtimeIncludeUnknown)}
            onChange={(event) =>
              update((next) => {
                next.runtimeIncludeUnknown = event.target.checked;
              })
            }
          />
          <span className="lamu-switch" aria-hidden="true" />
          <span>Include unknown runtime</span>
        </label>
      </fieldset>

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Release year</legend>
        <RangeField
          id={yearMinId}
          caption="From"
          accessibleName="Release year minimum"
          valueText={String(yearMinValue)}
          min={YEAR_MIN_EXTENT}
          max={currentYear}
          step={1}
          value={yearMinValue}
          onValueChange={(value) =>
            update((next) => {
              next.yearMin = value;
            })
          }
        />
        <RangeField
          id={yearMaxId}
          caption="To"
          accessibleName="Release year maximum"
          valueText={String(yearMaxValue)}
          min={YEAR_MIN_EXTENT}
          max={currentYear}
          step={1}
          value={yearMaxValue}
          onValueChange={(value) =>
            update((next) => {
              next.yearMax = value;
            })
          }
        />
      </fieldset>

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Enrichment status</legend>
        <div className="lamu-chip-row">
          {STATUS_OPTIONS.map((item) => {
            const checked = selectedStatuses.has(item.wire);
            return (
              <label
                key={item.wire}
                className={checked ? 'lamu-chip lamu-chip-selected' : 'lamu-chip'}
              >
                <input
                  type="checkbox"
                  className="sr-only"
                  checked={checked}
                  onChange={(event) => toggleStatus(item.wire, event.target.checked)}
                />
                <span>{item.label}</span>
              </label>
            );
          })}
        </div>
      </fieldset>

      {showSort ? (
        <fieldset className="lamu-filter-group">
          <legend className="lamu-filter-legend">Sort</legend>
          <div className="lamu-chip-row">
            {SORT_FIELDS.map((field) => (
              <button
                key={field}
                type="button"
                className="lamu-chip"
                aria-pressed={query.sort === field}
                onClick={() => setSort(field)}
              >
                {field}
              </button>
            ))}
            <button type="button" className="lamu-chip" onClick={toggleDirection}>
              {direction}
            </button>
          </div>
          <p className="lamu-filter-note">
            Titles with no value for the chosen sort are listed last.
          </p>
        </fieldset>
      ) : null}

      <footer className="lamu-filter-footer">
        {matchCount === undefined ? (
          <span />
        ) : (
          <p className="lamu-filter-count">
            {matchCount} {matchCount === 1 ? 'match' : 'matches'}
          </p>
        )}
        <div className="lamu-filter-footer-actions">
          <button type="button" className="lamu-link" onClick={handleClear}>
            Clear all
          </button>
          {onCollapse ? (
            <button type="button" className="lamu-link" onClick={onCollapse}>
              Collapse
            </button>
          ) : null}
        </div>
      </footer>
    </section>
  );
}

export default FilterPanel;
