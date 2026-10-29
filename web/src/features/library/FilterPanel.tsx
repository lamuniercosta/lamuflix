import {
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from 'react';
import type { components } from '../../api/types';
import { CloseIcon } from '../../ui/icons';
import {
  currentYearExtent,
  isRatingActive,
  normalizeRatingValue,
  parseFilterQuery,
  RATING_MAX_EXTENT,
  RATING_MIN_EXTENT,
  RUNTIME_MAX_EXTENT,
  RUNTIME_MIN_EXTENT,
  toUrlQuery,
  YEAR_MIN_EXTENT,
  type FilterQuery,
} from './filterQuery';
import { useDebouncedSearch } from './useDebouncedSearch';

type GenreDto = components['schemas']['GenreDto'];
type CollectionDto = components['schemas']['CollectionDto'];
type PersonDto = components['schemas']['PersonDto'];
type DirectorDto = components['schemas']['DirectorDto'];

export interface FilterPanelProps {
  query?: Partial<FilterQuery>;
  onChange?: (next: FilterQuery) => void;
  onQueryChange?: (next: FilterQuery) => void;
  onClear?: () => void;
  onCollapse?: () => void;
  matchCount?: number;
  genres?: GenreDto[];
  collections?: CollectionDto[];
  actors?: PersonDto[];
  directors?: DirectorDto[];
  genreError?: unknown;
  genresError?: unknown;
  collectionError?: unknown;
  collectionsError?: unknown;
  castError?: unknown;
  actorsError?: unknown;
  directorError?: unknown;
  directorsError?: unknown;
  directorLoading?: boolean;
  onRetryDirectors?: () => void;
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
const DIRECTOR_SUGGESTION_LIMIT = 8;

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

function sortCollections<T extends { id?: string | number; name?: string | null }>(
  items: readonly T[],
): T[] {
  return [...items].sort((a, b) => {
    const byName = String(a.name ?? '').localeCompare(String(b.name ?? ''));
    return byName !== 0 ? byName : Number(a.id) - Number(b.id);
  });
}

interface RangeThumb {
  id: string;
  accessibleName: string;
  valueText: string;
  value: number;
  onValueChange: (value: string) => void;
}

interface DualRangeFieldProps {
  min: number;
  max: number;
  step: number;
  low: RangeThumb;
  high: RangeThumb;
}

// Two overlapping range inputs share one track; each thumb is clamped so the
// pair can never cross, and the amber fill spans the selected interval.
function DualRangeField({ min, max, step, low, high }: DualRangeFieldProps) {
  const span = max - min || 1;
  const fill = {
    '--range-from': (low.value - min) / span,
    '--range-to': (high.value - min) / span,
  } as CSSProperties;
  // Keep the low thumb reachable when both thumbs meet near the top end.
  const lowOnTop = low.value > min + span / 2;
  return (
    <div className="lamu-range-field">
      <div className="lamu-range-caption" aria-hidden="true">
        <span>{low.valueText}</span>
        <span>{high.valueText}</span>
      </div>
      <div className="lamu-dual-range" style={fill}>
        <input
          id={low.id}
          type="range"
          className="lamu-range"
          aria-label={low.accessibleName}
          min={min}
          max={max}
          step={step}
          value={low.value}
          style={lowOnTop ? { zIndex: 2 } : undefined}
          onChange={(event) =>
            low.onValueChange(String(Math.min(Number(event.target.value), high.value)))
          }
        />
        <input
          id={high.id}
          type="range"
          className="lamu-range"
          aria-label={high.accessibleName}
          min={min}
          max={max}
          step={step}
          value={high.value}
          onChange={(event) =>
            high.onValueChange(String(Math.max(Number(event.target.value), low.value)))
          }
        />
      </div>
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
  const directorInputId = `${ids}-director`;
  const directorListId = `${ids}-director-list`;
  const directorOptionId = (index: number) => `${ids}-director-option-${index}`;
  const runtimeMinId = `${ids}-runtime-min`;
  const runtimeMaxId = `${ids}-runtime-max`;
  const yearMinId = `${ids}-year-min`;
  const yearMaxId = `${ids}-year-max`;
  const ratingMinId = `${ids}-rating-min`;
  const ratingMaxId = `${ids}-rating-max`;
  const unknownId = `${ids}-unknown`;

  const [internalQuery, setInternalQuery] = useState<FilterQuery>(() => readQuery(queryProp));
  const [currentYear] = useState(() => currentYearExtent(new Date()));
  const query = isControlled ? readQuery(queryProp) : internalQuery;

  const sortedGenres = useMemo(() => sortByName(props.genres ?? []), [props.genres]);
  const sortedCollections = useMemo(
    () => sortCollections(props.collections ?? []),
    [props.collections],
  );
  const sortedActors = useMemo(() => sortByName(props.actors ?? []), [props.actors]);
  const sortedDirectors = useMemo(() => {
    const items = [...(props.directors ?? [])];
    return items.sort((a, b) => {
      const byName = String(a.name ?? '').localeCompare(String(b.name ?? ''));
      if (byName !== 0) {
        return byName;
      }
      return Number(a.id) - Number(b.id);
    });
  }, [props.directors]);
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
  const castInputRef = useRef<HTMLInputElement>(null);
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
  const [directorText, setDirectorText] = useState('');
  const [directorActiveIndex, setDirectorActiveIndex] = useState(-1);
  const directorInputRef = useRef<HTMLInputElement>(null);

  const selectedDirectorId =
    query.directorId == null || String(query.directorId) === '' ? '' : String(query.directorId);
  const selectedDirector = sortedDirectors.find((item) => String(item.id) === selectedDirectorId);
  const directorFailed = Boolean(props.directorError ?? props.directorsError);
  const directorNeedle = directorText.trim().toLowerCase();
  const directorSuggestions =
    directorNeedle === '' || props.directorLoading || directorFailed
      ? []
      : sortedDirectors
          .filter((item) =>
            String(item.name ?? '')
              .toLowerCase()
              .includes(directorNeedle),
          )
          .slice(0, DIRECTOR_SUGGESTION_LIMIT);
  const directorListOpen = directorSuggestions.length > 0;
  const activeDirectorId =
    directorListOpen && directorActiveIndex >= 0 && directorActiveIndex < directorSuggestions.length
      ? directorOptionId(directorActiveIndex)
      : undefined;

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
  const ratingActive = isRatingActive(query.minRating, query.maxRating);
  const ratingMinValue = ratingActive
    ? numberOr(query.minRating, RATING_MIN_EXTENT)
    : RATING_MIN_EXTENT;
  const ratingMaxValue = ratingActive
    ? numberOr(query.maxRating, RATING_MAX_EXTENT)
    : RATING_MAX_EXTENT;
  const ratingMinText = ratingMinValue.toFixed(1);
  const ratingMaxText = ratingMaxValue.toFixed(1);
  const selectedStatuses = new Set(query.statuses.map(String));
  const direction = query.direction === 'Descending' ? 'Descending' : 'Ascending';
  const genreFailed = Boolean(props.genreError ?? props.genresError);
  const collectionFailed = Boolean(props.collectionError ?? props.collectionsError);
  const castFailed = Boolean(props.castError ?? props.actorsError);
  const runtimeMaxText = runtimeMaxValue >= RUNTIME_MAX_EXTENT ? '240+' : String(runtimeMaxValue);

  const toggleGenre = (id: string | number, on: boolean) =>
    update((next) => {
      next.genreIds = on
        ? [...next.genreIds, id]
        : next.genreIds.filter((item) => String(item) !== String(id));
    });

  const toggleCollection = (id: string | number, on: boolean) =>
    update((next) => {
      next.collectionId = on ? id : null;
    });

  const addActor = (id: string | number) => {
    setCastText('');
    update((next) => {
      next.actorIds = [...next.actorIds, id];
    });
  };

  const removeActor = (id: string | number) => {
    update((next) => {
      next.actorIds = next.actorIds.filter((item) => String(item) !== String(id));
    });
    // The focused remove button unmounts with its chip; keep focus in the cast group.
    castInputRef.current?.focus();
  };

  const selectDirector = (id: string | number) => {
    setDirectorText('');
    setDirectorActiveIndex(-1);
    update((next) => {
      next.directorId = String(id);
    });
    directorInputRef.current?.focus();
  };

  const clearDirector = () => {
    setDirectorText('');
    setDirectorActiveIndex(-1);
    update((next) => {
      next.directorId = '';
    });
    directorInputRef.current?.focus();
  };

  const handleDirectorKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      if (directorSuggestions.length === 0) {
        return;
      }
      event.preventDefault();
      setDirectorActiveIndex((current) => {
        if (current < 0) {
          return 0;
        }
        if (event.key === 'ArrowDown') {
          return Math.min(current + 1, directorSuggestions.length - 1);
        }
        return Math.max(current - 1, 0);
      });
      return;
    }

    if (event.key === 'Enter') {
      if (activeDirectorId === undefined) {
        return;
      }
      event.preventDefault();
      const suggestion = directorSuggestions[directorActiveIndex];
      if (suggestion) {
        selectDirector(suggestion.id);
      }
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      setDirectorText('');
      setDirectorActiveIndex(-1);
      directorInputRef.current?.focus();
    }
  };

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
    setDirectorText('');
    setDirectorActiveIndex(-1);
    update((next) => {
      next.text = '';
      next.genreIds = [];
      next.collectionId = null;
      next.actorIds = [];
      next.directorId = '';
      next.runtimeMin = '';
      next.runtimeMax = '';
      next.runtimeIncludeUnknown = false;
      next.yearMin = '';
      next.yearMax = '';
      next.minRating = null;
      next.maxRating = null;
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
        <legend className="lamu-filter-legend">Collection</legend>
        {collectionFailed ? (
          <p role="alert" className="lamu-filter-error">
            Collections could not be loaded.
          </p>
        ) : (
          <div className="lamu-chip-row">
            {sortedCollections.map((item) => {
              const checked = String(query.collectionId) === String(item.id);
              return (
                <label
                  key={String(item.id)}
                  className={checked ? 'lamu-chip lamu-chip-selected' : 'lamu-chip'}
                >
                  <input
                    type="checkbox"
                    className="sr-only"
                    checked={checked}
                    onChange={(event) => toggleCollection(item.id, event.target.checked)}
                  />
                  <span>{item.name}</span>
                  <span className="lamu-chip-count" aria-hidden="true">
                    {item.movieCount}
                  </span>
                  <span className="sr-only">, {item.movieCount} movies</span>
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
              ref={castInputRef}
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
        <legend className="lamu-filter-legend">Director</legend>
        {selectedDirectorId !== '' ? (
          <div className="lamu-chip-row">
            <span className="lamu-chip lamu-chip-selected">
              <span>
                {selectedDirector
                  ? String(selectedDirector.name)
                  : `Director ${selectedDirectorId}`}
              </span>
              {selectedDirector ? (
                <>
                  <span className="lamu-chip-count" aria-hidden="true">
                    {selectedDirector.movieCount}
                  </span>
                  <span className="sr-only">, {selectedDirector.movieCount} movies</span>
                </>
              ) : null}
              <button
                type="button"
                className="lamu-chip-remove"
                aria-label="Clear Director"
                onClick={clearDirector}
              >
                <CloseIcon />
              </button>
            </span>
          </div>
        ) : null}
        <label htmlFor={directorInputId} className="lamu-filter-sublabel">
          Director
        </label>
        <input
          ref={directorInputRef}
          id={directorInputId}
          type="search"
          role="combobox"
          className="lamu-field-input"
          placeholder="Search directors"
          autoComplete="off"
          aria-autocomplete="list"
          aria-expanded={directorListOpen}
          aria-controls={directorListOpen ? directorListId : undefined}
          aria-activedescendant={activeDirectorId}
          value={directorText}
          onChange={(event) => {
            setDirectorText(event.target.value);
            setDirectorActiveIndex(-1);
          }}
          onKeyDown={handleDirectorKeyDown}
          onBlur={(event) => {
            const group = event.currentTarget.closest('fieldset');
            if (
              group &&
              event.relatedTarget instanceof Node &&
              group.contains(event.relatedTarget)
            ) {
              return;
            }
            setDirectorText('');
            setDirectorActiveIndex(-1);
          }}
        />
        <div aria-live="polite">
          {props.directorLoading ? (
            <p className="lamu-filter-note">Loading directors</p>
          ) : directorFailed ? (
            <div className="lamu-filter-note">
              <p className="lamu-filter-error">Could not load directors</p>
              <button type="button" className="lamu-link" onClick={props.onRetryDirectors}>
                Retry
              </button>
            </div>
          ) : directorListOpen ? (
            <ul id={directorListId} role="listbox" className="lamu-chip-row">
              {directorSuggestions.map((item, index) => (
                <li
                  key={String(item.id)}
                  id={directorOptionId(index)}
                  role="option"
                  aria-selected={directorActiveIndex === index}
                  className={
                    directorActiveIndex === index ? 'lamu-chip lamu-chip-selected' : 'lamu-chip'
                  }
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => selectDirector(item.id)}
                >
                  <span>{item.name}</span>
                  <span className="lamu-chip-count" aria-hidden="true">
                    {item.movieCount}
                  </span>
                  <span className="sr-only">, {item.movieCount} movies</span>
                </li>
              ))}
            </ul>
          ) : directorNeedle !== '' ? (
            <p className="lamu-filter-note">No directors found</p>
          ) : null}
        </div>
      </fieldset>

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Runtime</legend>
        <DualRangeField
          min={RUNTIME_MIN_EXTENT}
          max={RUNTIME_MAX_EXTENT}
          step={5}
          low={{
            id: runtimeMinId,
            accessibleName: 'Runtime minimum (minutes)',
            valueText: `${runtimeMinValue} min`,
            value: runtimeMinValue,
            onValueChange: (value) =>
              update((next) => {
                next.runtimeMin = value;
              }),
          }}
          high={{
            id: runtimeMaxId,
            accessibleName: 'Runtime maximum (minutes)',
            valueText: `${runtimeMaxText} min`,
            value: runtimeMaxValue,
            onValueChange: (value) =>
              update((next) => {
                next.runtimeMax = value;
              }),
          }}
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
        <DualRangeField
          min={YEAR_MIN_EXTENT}
          max={currentYear}
          step={1}
          low={{
            id: yearMinId,
            accessibleName: 'Release year minimum',
            valueText: String(yearMinValue),
            value: yearMinValue,
            onValueChange: (value) =>
              update((next) => {
                next.yearMin = value;
              }),
          }}
          high={{
            id: yearMaxId,
            accessibleName: 'Release year maximum',
            valueText: String(yearMaxValue),
            value: yearMaxValue,
            onValueChange: (value) =>
              update((next) => {
                next.yearMax = value;
              }),
          }}
        />
      </fieldset>

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">IMDb rating</legend>
        <DualRangeField
          min={RATING_MIN_EXTENT}
          max={RATING_MAX_EXTENT}
          step={0.1}
          low={{
            id: ratingMinId,
            accessibleName: 'IMDb rating minimum',
            valueText: ratingMinText,
            value: ratingMinValue,
            onValueChange: (value) =>
              update((next) => {
                next.minRating = normalizeRatingValue(value);
                next.maxRating = normalizeRatingValue(ratingMaxValue);
              }),
          }}
          high={{
            id: ratingMaxId,
            accessibleName: 'IMDb rating maximum',
            valueText: ratingMaxText,
            value: ratingMaxValue,
            onValueChange: (value) =>
              update((next) => {
                next.minRating = normalizeRatingValue(ratingMinValue);
                next.maxRating = normalizeRatingValue(value);
              }),
          }}
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
