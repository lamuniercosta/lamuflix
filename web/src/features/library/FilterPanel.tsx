import { useEffect, useId, useMemo, useRef, useState } from 'react';
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
  type LibraryDirection,
  type LibrarySort,
} from './filterQuery';
import { DualRangeField } from './DualRangeField';
import { FilterFacetCombobox, type FacetOption } from './FilterFacetCombobox';
import { FilterSearchField } from './FilterSearchField';
import { FilterSortControls } from './FilterSortControls';
import { subscribeToRouteChange } from '../app-navigation/appNavigation';
import './filterPanel.css';

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
  showFooter?: boolean;
  routeKey?: number;
}

const STATUS_OPTIONS = [
  { label: 'Pending', wire: 'Pending' },
  { label: 'Enriched', wire: 'Enriched' },
  { label: 'Not found', wire: 'NotFound' },
  { label: 'Failed', wire: 'Failed' },
] as const;

const CAST_SUGGESTION_LIMIT = 8;
const DIRECTOR_SUGGESTION_LIMIT = 8;
const COLLECTION_FIND_THRESHOLD = 8;

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

/**
 * Orchestration shell: owns query mutation and URL synchronization, the
 * current-year snapshot, normalized facets, the rating/runtime/release-year
 * fieldsets and the public query/callback interface. Cohesive sections live in
 * the sibling modules and are remounted (via `key`) when the shell resyncs.
 */
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
    showFooter = true,
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
  const collectionFindId = `${ids}-collection-find`;
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

  const queryRef = useRef(query);
  useEffect(() => {
    queryRef.current = query;
  }, [query]);

  // Bumped on every resync and on Clear all. The search sibling takes it as its
  // key, so remounting drops its local draft and any pending debounced commit.
  const [draftEpoch, setDraftEpoch] = useState(0);
  // Clear all also empties the facet drafts; navigation leaves them alone.
  const [clearEpoch, setClearEpoch] = useState(0);
  // The draft seed the search sibling mounts with, so a resync shows the URL text
  // even when the shell is controlled and its query has not caught up yet.
  const [searchSeed, setSearchSeed] = useState(() => String(readQuery(queryProp).text ?? ''));
  const queryText = String(query.text ?? '');
  const [prevQueryText, setPrevQueryText] = useState(queryText);
  if (queryText !== prevQueryText) {
    setPrevQueryText(queryText);
    setSearchSeed(queryText);
  }

  const sortedGenres = useMemo(() => sortByName(props.genres ?? []), [props.genres]);
  const sortedCollections = useMemo(
    () => sortCollections(props.collections ?? []),
    [props.collections],
  );
  const sortedActors = useMemo(() => sortByName(props.actors ?? []), [props.actors]);
  const sortedDirectors = useMemo(() => sortCollections(props.directors ?? []), [props.directors]);
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

  useEffect(() => {
    if (integrated) {
      return;
    }
    const resync = () => {
      const fromUrl = parseFilterQuery(window.location.search);
      if (!isControlled) {
        setInternalQuery(fromUrl);
      }
      setSearchSeed(String(fromUrl.text ?? ''));
      setDraftEpoch((epoch) => epoch + 1);
    };
    return subscribeToRouteChange(resync);
  }, [integrated, isControlled]);

  const [collectionText, setCollectionText] = useState('');
  const [castFocusSignal, setCastFocusSignal] = useState(0);
  const [directorFocusSignal, setDirectorFocusSignal] = useState(0);

  const selectedDirectorId =
    query.directorId == null || String(query.directorId) === '' ? '' : String(query.directorId);
  const selectedDirector = sortedDirectors.find((item) => String(item.id) === selectedDirectorId);
  const directorFailed = Boolean(props.directorError ?? props.directorsError);
  const directorOptions: FacetOption[] =
    props.directorLoading || directorFailed
      ? []
      : sortedDirectors.map((item) => ({
          id: item.id,
          name: String(item.name ?? ''),
          count: item.movieCount,
        }));

  const selectedGenreIds = new Set(query.genreIds.map(String));
  const selectedActorIds = new Set(query.actorIds.map(String));
  const selectedCast = query.actorIds.map((id) => ({
    id,
    name: actorNameById.get(String(id)) ?? `Unknown cast member ${String(id)}`,
  }));
  const castOptions: FacetOption[] = sortedActors
    .filter((actor) => !selectedActorIds.has(String(actor.id)))
    .map((actor) => ({ id: actor.id, name: String(actor.name ?? '') }));

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
  const direction: LibraryDirection = query.direction === 'Descending' ? 'Descending' : 'Ascending';
  const genreFailed = Boolean(props.genreError ?? props.genresError);
  const collectionFailed = Boolean(props.collectionError ?? props.collectionsError);
  const collectionHealthyEmpty = sortedCollections.length === 0 && !collectionFailed;
  const collectionNeedle = collectionText.trim().toLowerCase();
  const visibleCollections = sortedCollections.filter(
    (item) =>
      collectionNeedle === '' ||
      String(item.name ?? '')
        .toLowerCase()
        .includes(collectionNeedle) ||
      String(item.id) === String(query.collectionId),
  );
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
    update((next) => {
      next.actorIds = [...next.actorIds, id];
    });
  };

  const removeActor = (id: string | number) => {
    update((next) => {
      next.actorIds = next.actorIds.filter((item) => String(item) !== String(id));
    });
    // The focused remove button unmounts with its chip; keep focus in the cast group.
    setCastFocusSignal((signal) => signal + 1);
  };

  const selectDirector = (id: string | number) => {
    update((next) => {
      next.directorId = String(id);
    });
    setDirectorFocusSignal((signal) => signal + 1);
  };

  const clearDirector = () => {
    update((next) => {
      next.directorId = '';
    });
    setDirectorFocusSignal((signal) => signal + 1);
  };

  const toggleStatus = (wire: string, on: boolean) =>
    update((next) => {
      next.statuses = on ? [...next.statuses, wire] : next.statuses.filter((item) => item !== wire);
    });

  const setSort = (field: LibrarySort) =>
    update((next) => {
      next.sort = field;
    });

  const toggleDirection = () =>
    update((next) => {
      next.direction = direction === 'Descending' ? 'Ascending' : 'Descending';
    });

  const handleClear = () => {
    setSearchSeed('');
    setDraftEpoch((epoch) => epoch + 1);
    setClearEpoch((epoch) => epoch + 1);
    setCollectionText('');
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

  const commitSearchText = (value: string) => {
    const next = parseFilterQuery(queryRef.current);
    next.text = value;
    commit(next);
  };

  return (
    <section className="lamu-filter-panel" aria-label="Filters">
      {showTitleSearch ? (
        <FilterSearchField
          key={`search-${routeKey ?? 'none'}-${draftEpoch}`}
          inputId={searchId}
          text={searchSeed}
          onCommit={commitSearchText}
        />
      ) : null}

      <fieldset className="lamu-filter-group">
        <legend className="lamu-filter-legend">Genres</legend>
        {genreFailed ? (
          <p role="alert" className="lamu-filter-error">
            Genres could not be loaded.
          </p>
        ) : (
          <div className="lamu-filter-rows">
            {sortedGenres.map((genre) => {
              const checked = selectedGenreIds.has(String(genre.id));
              return (
                <label
                  key={String(genre.id)}
                  className={
                    checked ? 'lamu-filter-row lamu-filter-row-selected' : 'lamu-filter-row'
                  }
                >
                  <input
                    type="checkbox"
                    className="sr-only"
                    checked={checked}
                    onChange={(event) => toggleGenre(genre.id, event.target.checked)}
                  />
                  <span className="lamu-filter-row-name">{genre.name}</span>
                  <span className="lamu-filter-row-count" aria-hidden="true">
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
        <legend className="lamu-filter-legend">Director</legend>
        {selectedDirectorId !== '' ? (
          <div className="lamu-filter-selected">
            <span className="lamu-filter-selected-item">
              <span className="lamu-filter-row-name">
                {selectedDirector
                  ? String(selectedDirector.name)
                  : `Director ${selectedDirectorId}`}
              </span>
              {selectedDirector ? (
                <>
                  <span className="lamu-filter-row-count" aria-hidden="true">
                    {selectedDirector.movieCount}
                  </span>
                  <span className="sr-only">, {selectedDirector.movieCount} movies</span>
                </>
              ) : null}
              <button
                type="button"
                className="lamu-tag-remove"
                aria-label="Clear Director"
                onClick={clearDirector}
              >
                <CloseIcon size={14} />
              </button>
            </span>
          </div>
        ) : null}
        <FilterFacetCombobox
          key={`director-${clearEpoch}`}
          kind="combobox"
          inputId={directorInputId}
          label="Director"
          placeholder="Search directors"
          listId={directorListId}
          optionId={directorOptionId}
          options={directorOptions}
          limit={DIRECTOR_SUGGESTION_LIMIT}
          onSelect={(option) => selectDirector(option.id)}
          focusSignal={directorFocusSignal}
          emptyMessage={props.directorLoading ? undefined : 'No directors found'}
          status={
            props.directorLoading ? (
              <p className="lamu-filter-note">Loading directors</p>
            ) : directorFailed ? (
              <div className="lamu-filter-note">
                <p className="lamu-filter-error">Could not load directors</p>
                <button type="button" className="lamu-link" onClick={props.onRetryDirectors}>
                  Retry
                </button>
              </div>
            ) : null
          }
        />
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
              <ul className="lamu-filter-selected" aria-label="Selected cast">
                {selectedCast.map((member, index) => (
                  <li key={`${String(member.id)}-${index}`} className="lamu-filter-selected-item">
                    <span className="lamu-filter-row-name">{member.name}</span>
                    <button
                      type="button"
                      className="lamu-tag-remove"
                      aria-label={`Remove ${member.name} from cast filter`}
                      onClick={() => removeActor(member.id)}
                    >
                      <CloseIcon size={14} />
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
            <FilterFacetCombobox
              key={`cast-${clearEpoch}`}
              kind="suggestions"
              inputId={castInputId}
              label="Add a cast member"
              placeholder="Type a name"
              listId={castListId}
              listLabel="Cast suggestions"
              options={castOptions}
              limit={CAST_SUGGESTION_LIMIT}
              onSelect={(option) => addActor(option.id)}
              focusSignal={castFocusSignal}
              emptyMessage="No matching cast members."
            />
          </>
        )}
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
        <legend className="lamu-filter-legend">Enrichment status</legend>
        <div className="lamu-filter-rows">
          {STATUS_OPTIONS.map((item) => {
            const checked = selectedStatuses.has(item.wire);
            return (
              <label
                key={item.wire}
                className={checked ? 'lamu-filter-row lamu-filter-row-selected' : 'lamu-filter-row'}
              >
                <input
                  type="checkbox"
                  className="sr-only"
                  checked={checked}
                  onChange={(event) => toggleStatus(item.wire, event.target.checked)}
                />
                <span className="lamu-filter-row-name">{item.label}</span>
              </label>
            );
          })}
        </div>
      </fieldset>

      {collectionHealthyEmpty ? null : (
        <fieldset className="lamu-filter-group">
          <legend className="lamu-filter-legend">Collection</legend>
          {collectionFailed ? (
            <p role="alert" className="lamu-filter-error">
              Collections could not be loaded.
            </p>
          ) : (
            <>
              {sortedCollections.length > COLLECTION_FIND_THRESHOLD ? (
                <FilterFacetCombobox
                  key={`collection-${clearEpoch}`}
                  kind="filter"
                  inputId={collectionFindId}
                  label="Find a collection"
                  placeholder="Find a collection"
                  options={[]}
                  onSelect={() => undefined}
                  onDraftChange={setCollectionText}
                />
              ) : null}
              <div className="lamu-filter-rows">
                {visibleCollections.map((item) => {
                  const checked = String(query.collectionId) === String(item.id);
                  return (
                    <label
                      key={String(item.id)}
                      className={
                        checked ? 'lamu-filter-row lamu-filter-row-selected' : 'lamu-filter-row'
                      }
                    >
                      <input
                        type="checkbox"
                        className="sr-only"
                        checked={checked}
                        onChange={(event) => toggleCollection(item.id, event.target.checked)}
                      />
                      <span className="lamu-filter-row-name">{item.name}</span>
                      <span className="lamu-filter-row-count" aria-hidden="true">
                        {item.movieCount}
                      </span>
                      <span className="sr-only">, {item.movieCount} movies</span>
                    </label>
                  );
                })}
              </div>
            </>
          )}
        </fieldset>
      )}

      {showSort ? (
        <FilterSortControls
          sort={String(query.sort)}
          direction={direction}
          onSortChange={setSort}
          onDirectionToggle={toggleDirection}
        />
      ) : null}

      {showFooter ? (
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
      ) : null}
    </section>
  );
}

export default FilterPanel;
