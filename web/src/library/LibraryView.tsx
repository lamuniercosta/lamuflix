import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { client } from '../api/client';
import type { components } from '../api/types';
import FilterPanel from '../features/library/FilterPanel';
import {
  currentYearExtent,
  isRuntimeActive,
  isYearActive,
  parseFilterQuery,
  RUNTIME_MAX_EXTENT,
  RUNTIME_MIN_EXTENT,
  SORT_OPTIONS,
  toCanonicalApiQuery,
  toUrlQuery,
  YEAR_MIN_EXTENT,
} from '../features/library/filterQuery';
import type { FilterQuery } from '../features/library/filterQuery';
import { buildBrowseParams } from './browseParams';
import LibraryErrorBoundary from './LibraryErrorBoundary';
import { useBrowseMoviesQuery } from './libraryQueries';
import type { BrowseMoviesQueryError } from './libraryQueries';
import LibraryTile from './LibraryTile';
import type { LibraryVariant } from './LibraryTile';
import MovieGrid from './MovieGrid';
import Pagination from './Pagination';
import './library.css';
import { useAmbientPosters } from '../ui/ambient';
import { genreIcon } from '../ui/genreIcon';
import { ChevronDownIcon, GridIcon, ListIcon } from '../ui/icons';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type GenreDto = components['schemas']['GenreDto'];

const lastGoodQueryKey = ['libraryLastGoodBrowse'];

function GenreDtoList(value: unknown): GenreDto[] {
  return Array.isArray(value) ? (value as GenreDto[]) : [];
}

function PersonDtoList(value: unknown): components['schemas']['PersonDto'][] {
  return Array.isArray(value) ? (value as components['schemas']['PersonDto'][]) : [];
}

function isWatchlistFlag(value: unknown): boolean {
  if (typeof value === 'boolean') {
    return value;
  }
  if (value == null) {
    return false;
  }
  return String(value).toLowerCase() === 'true';
}

function replaceQuery(next: FilterQuery, currentYear: number): void {
  try {
    const search = toUrlQuery(next, currentYear);
    const path = window.location.pathname || '/';
    window.history.replaceState(null, '', search ? `${path}?${search}` : path);
  } catch {
    /* history is unavailable in this environment */
  }
}

function activeFilterGroups(query: FilterQuery, currentYear: number): number {
  const groups = [
    (query.genreIds ?? []).length > 0,
    (query.actorIds ?? []).length > 0,
    isRuntimeActive(query.runtimeMin, query.runtimeMax, query.runtimeIncludeUnknown),
    isYearActive(query.yearMin, query.yearMax, currentYear),
    (query.statuses ?? []).length > 0,
  ];
  return groups.filter(Boolean).length;
}

const GENRE_HEADING_FALLBACK = 'Selected genres';

function libraryTitle(query: FilterQuery, genres: GenreDto[], watchlistActive: boolean): string {
  const text = String(query.text ?? '').trim();
  if (text !== '') {
    return `Results for “${text}”`;
  }
  const genreIds = query.genreIds ?? [];
  if (genreIds.length > 0) {
    const namesById = new Map(
      genres.map((genre): [string, string] => [String(genre.id), genre.name]),
    );
    const names = genreIds
      .map((id) => namesById.get(String(id)))
      .filter((name): name is string => name !== undefined);
    return names.length > 0 ? names.join(' & ') : GENRE_HEADING_FALLBACK;
  }
  return watchlistActive ? 'Watchlist' : 'All movies';
}

type ResultsMode = 'grid' | 'watchlistEmpty' | 'libraryEmpty';

function resultsMode(
  hasItems: boolean,
  pastEnd: boolean,
  watchlist: boolean,
  filtered: boolean,
): ResultsMode {
  if (hasItems || pastEnd || filtered) {
    return 'grid';
  }
  if (watchlist) {
    return 'watchlistEmpty';
  }
  return 'libraryEmpty';
}

interface GenreTilesProps {
  genres: GenreDto[];
  selectedIds: ReadonlyArray<string | number>;
  onToggle: (id: string | number) => void;
}

function GenreTiles({ genres, selectedIds, onToggle }: GenreTilesProps) {
  if (genres.length === 0) {
    return null;
  }
  const selected = new Set(selectedIds.map(String));

  return (
    <div className="genre-tiles" role="group" aria-label="Browse by genre">
      {genres.map((genre) => {
        const Icon = genreIcon(genre.name);
        const isSelected = selected.has(String(genre.id));
        return (
          <button
            key={String(genre.id)}
            type="button"
            className="genre-tile"
            aria-pressed={isSelected}
            onClick={() => onToggle(genre.id)}
          >
            <Icon size={20} className="genre-tile-icon" />
            <span>{genre.name}</span>
          </button>
        );
      })}
    </div>
  );
}

interface ResultsBodyProps {
  loading: boolean;
  mode: ResultsMode;
  hasItems: boolean;
  tiles: ReactNode;
  variant: LibraryVariant;
  focusSignal: number;
  totalCount: number;
  page: number;
  pageSize: number;
  filtersActive: boolean;
  onClearFilters: () => void;
  onFirstPage: () => void;
  onPageChange: (page: number) => void;
}

function ResultsBody(props: ResultsBodyProps) {
  if (props.loading) {
    return <MovieGrid isLoading pageSize={props.pageSize} variant={props.variant} />;
  }
  if (props.mode === 'watchlistEmpty') {
    return <p>Your watchlist is empty.</p>;
  }
  if (props.mode === 'libraryEmpty') {
    return <p>No movies yet. Import a folder to get started.</p>;
  }
  return (
    <>
      <MovieGrid
        tiles={props.hasItems ? props.tiles : undefined}
        variant={props.variant}
        focusSignal={props.focusSignal}
        totalCount={props.totalCount}
        page={props.page}
        pageSize={props.pageSize}
        filtersActive={props.filtersActive}
        onClearFilters={props.onClearFilters}
        onGoToFirstPage={props.onFirstPage}
      />
      {props.hasItems ? (
        <Pagination
          page={props.page}
          pageSize={props.pageSize}
          totalCount={props.totalCount}
          setPage={props.onPageChange}
        />
      ) : null}
    </>
  );
}

interface LibraryBrowseViewProps {
  query: FilterQuery;
  currentYear: number;
  onQueryChange: (value: unknown) => void;
  onClearFilters: () => void;
  onPageChange: (page: number) => void;
  leadingItem?: MovieSummary | null;
  inWatchlist?: boolean;
  routeKey?: number;
  routePath?: string;
  integrated?: boolean;
  onOpenMovie?: (id: string | number) => void;
}

interface Chip {
  key: string;
  label: string;
  removeLabel: string;
  onRemove: () => void;
}

const SORT_LABELS: Record<string, string> = Object.fromEntries(
  SORT_OPTIONS.map((option) => [`${option.sort}|${option.direction}`, option.label]),
);

const DEFAULT_SORT_VALUE = 'Title|Ascending';

function sortValueOf(sort: unknown, direction: unknown): string {
  const value = `${String(sort)}|${String(direction)}`;
  return Object.prototype.hasOwnProperty.call(SORT_LABELS, value) ? value : DEFAULT_SORT_VALUE;
}

function sortLabel(sort: unknown, direction: unknown): string {
  return SORT_LABELS[sortValueOf(sort, direction)];
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

function isUnknownRuntimeIncluded(raw: unknown): boolean {
  return String(raw ?? '').toLowerCase() === 'true';
}

function pushChip(
  list: Chip[],
  key: string,
  label: string,
  removeLabel: string,
  onRemove: () => void,
): void {
  list.push({ key, label, removeLabel, onRemove });
}

function LibraryBrowseView({
  query,
  currentYear,
  onQueryChange,
  onClearFilters,
  onPageChange,
  leadingItem,
  inWatchlist = false,
  routeKey,
  routePath,
  integrated = false,
  onOpenMovie,
}: LibraryBrowseViewProps) {
  const filters = useMemo(() => buildBrowseParams(query, currentYear), [query, currentYear]);
  const watchlistActive = inWatchlist || isWatchlistFlag(query.inWatchlist);
  const browse = useBrowseMoviesQuery(leadingItem != null, watchlistActive, filters);

  const genresQuery = useQuery({
    queryKey: ['genres'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/genres');
      if (error) {
        throw new Error('Genres could not be loaded.');
      }
      return data;
    },
  });

  const actorsQuery = useQuery({
    queryKey: ['people', 'actor'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/people', {
        params: { query: { role: 'actor' } as unknown as Record<string, unknown> },
      });
      if (error) {
        throw new Error('Cast could not be loaded.');
      }
      return data;
    },
  });

  const queryClient = useQueryClient();
  useEffect(() => {
    if (browse.data) {
      queryClient.setQueryData(lastGoodQueryKey, browse.data);
    }
  }, [browse.data, queryClient]);
  const lastGood = queryClient.getQueryData<BrowseMoviesResponse>(lastGoodQueryKey) ?? null;
  const [hiddenIds, setHiddenIds] = useState<ReadonlySet<string>>(() => new Set());
  const wasWatchlistActive = useRef(watchlistActive);
  useEffect(() => {
    if (wasWatchlistActive.current && !watchlistActive) {
      setHiddenIds(new Set());
    }
    wasWatchlistActive.current = watchlistActive;
  }, [watchlistActive]);
  const [focusSignal, setFocusSignal] = useState(0);
  const focusGrid = useCallback(() => {
    setFocusSignal((signal) => signal + 1);
  }, []);
  const setTileHidden = useCallback((id: string, hidden: boolean) => {
    setHiddenIds((prev) => {
      if (prev.has(id) === hidden) {
        return prev;
      }
      const next = new Set(prev);
      if (hidden) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }, []);

  const [variant, setVariant] = useState<LibraryVariant>('grid');
  const [panelOpen, setPanelOpen] = useState(false);
  const panelId = useId();
  const toggleRef = useRef<HTMLButtonElement>(null);

  const collapsePanel = useCallback(() => {
    toggleRef.current?.focus();
    setPanelOpen(false);
  }, []);

  const browseError = browse.error as BrowseMoviesQueryError | null;
  if (browseError && (browseError.invalidFilter === true || browseError.status === 422)) {
    throw browseError;
  }

  const data = browse.data ?? lastGood;
  const fetched = data?.items ?? [];
  const prepended =
    leadingItem != null && !fetched.some((item) => String(item.id) === String(leadingItem.id));
  const items: MovieSummary[] = prepended && leadingItem ? [leadingItem, ...fetched] : fetched;
  const visibleItems = items.filter((item) => !hiddenIds.has(String(item.id)));
  const totalCount = Number(data?.totalCount ?? 0) + (prepended ? 1 : 0);
  const totalPages = Math.max(1, Math.ceil(totalCount / filters.pageSize));
  const pastEnd = visibleItems.length === 0 && filters.page > 1 && filters.page > totalPages;
  const activeFilters = toUrlQuery({ ...query, unknown: {} }, currentYear) !== '';
  const loading = browse.isPending && browse.data == null && leadingItem == null;
  const refreshFailed = browse.error != null;
  const mode = resultsMode(visibleItems.length > 0, pastEnd, watchlistActive, activeFilters);
  const openMovie = onOpenMovie;
  const tiles = useMemo(
    () =>
      visibleItems.map((movie) => (
        <LibraryTile
          key={String(movie.id)}
          item={movie}
          variant={variant}
          filterActive={watchlistActive}
          onRemoved={focusGrid}
          onHiddenChange={setTileHidden}
          onOpenMovie={openMovie}
        />
      )),
    [visibleItems, variant, watchlistActive, focusGrid, setTileHidden, openMovie],
  );

  useAmbientPosters(visibleItems.map((item) => item.posterUrl));

  const removeValue = useCallback(
    (mutate: (next: FilterQuery) => void) => {
      const next = parseFilterQuery(query);
      mutate(next);
      next.page = 1;
      onQueryChange(next);
    },
    [query, onQueryChange],
  );

  const toggleGenre = useCallback(
    (id: string | number) => {
      const key = String(id);
      const next = parseFilterQuery(query);
      const selected = next.genreIds.map(String).includes(key);
      next.genreIds = selected
        ? next.genreIds.filter((value) => String(value) !== key)
        : [...next.genreIds, id];
      next.page = 1;
      onQueryChange(next);
    },
    [query, onQueryChange],
  );

  const genreNameById = useMemo(
    () => new Map(GenreDtoList(genresQuery.data).map((genre) => [String(genre.id), genre.name])),
    [genresQuery.data],
  );
  const actorNameById = useMemo(
    () =>
      new Map(
        PersonDtoList(actorsQuery.data).map((actor) => [
          String(actor.id),
          String(actor.name ?? ''),
        ]),
      ),
    [actorsQuery.data],
  );

  const chips: Chip[] = useMemo(() => {
    const list: Chip[] = [];
    if (query.text) {
      const text = String(query.text);
      pushChip(list, 'text', `Search: ${text}`, `Remove search filter ${text}`, () =>
        removeValue((next) => {
          next.text = '';
        }),
      );
    }
    for (const [index, id] of (query.genreIds ?? []).entries()) {
      const value = String(id);
      const name = genreNameById.get(value) || value;
      pushChip(
        list,
        `genre-${value}-${index}`,
        `Genre: ${name}`,
        `Remove genre filter ${name}`,
        () =>
          removeValue((next) => {
            const position = next.genreIds.map(String).indexOf(value);
            if (position >= 0) {
              next.genreIds = next.genreIds.filter((_, itemIndex) => itemIndex !== position);
            }
          }),
      );
    }
    for (const [index, id] of (query.actorIds ?? []).entries()) {
      const value = String(id);
      const name = actorNameById.get(value) || value;
      pushChip(list, `actor-${value}-${index}`, `Cast: ${name}`, `Remove cast filter ${name}`, () =>
        removeValue((next) => {
          const position = next.actorIds.map(String).indexOf(value);
          if (position >= 0) {
            next.actorIds = next.actorIds.filter((_, itemIndex) => itemIndex !== position);
          }
        }),
      );
    }
    const runtimeMin = boundActive(query.runtimeMin, RUNTIME_MIN_EXTENT);
    if (runtimeMin !== null) {
      pushChip(
        list,
        'runtimeMin',
        `Runtime from ${runtimeMin} min`,
        `Remove minimum runtime ${runtimeMin} minutes`,
        () =>
          removeValue((next) => {
            next.runtimeMin = '';
          }),
      );
    }
    const runtimeMax = boundActive(query.runtimeMax, RUNTIME_MAX_EXTENT);
    if (runtimeMax !== null) {
      pushChip(
        list,
        'runtimeMax',
        `Runtime to ${runtimeMax} min`,
        `Remove maximum runtime ${runtimeMax} minutes`,
        () =>
          removeValue((next) => {
            next.runtimeMax = '';
          }),
      );
    }
    if (isUnknownRuntimeIncluded(query.runtimeIncludeUnknown)) {
      pushChip(
        list,
        'runtimeIncludeUnknown',
        'Include unknown runtime',
        'Remove include unknown runtime filter',
        () =>
          removeValue((next) => {
            next.runtimeIncludeUnknown = false;
          }),
      );
    }
    const yearMin = boundActive(query.yearMin, YEAR_MIN_EXTENT);
    if (yearMin !== null) {
      pushChip(list, 'yearMin', `From ${yearMin}`, `Remove minimum year ${yearMin}`, () =>
        removeValue((next) => {
          next.yearMin = '';
        }),
      );
    }
    const yearMax = boundActive(query.yearMax, currentYear);
    if (yearMax !== null) {
      pushChip(list, 'yearMax', `To ${yearMax}`, `Remove maximum year ${yearMax}`, () =>
        removeValue((next) => {
          next.yearMax = '';
        }),
      );
    }
    for (const [index, status] of (query.statuses ?? []).entries()) {
      const value = String(status);
      pushChip(
        list,
        `status-${value}-${index}`,
        `Status: ${value}`,
        `Remove status filter ${value}`,
        () =>
          removeValue((next) => {
            const position = next.statuses.indexOf(status);
            if (position >= 0) {
              next.statuses = next.statuses.filter((_, itemIndex) => itemIndex !== position);
            }
          }),
      );
    }
    return list;
  }, [query, removeValue, currentYear, genreNameById, actorNameById]);

  const prevRoutePathRef = useRef(routePath);
  useEffect(() => {
    if (routePath == null || prevRoutePathRef.current === routePath) {
      prevRoutePathRef.current = routePath;
      return;
    }
    prevRoutePathRef.current = routePath;
    setPanelOpen(false);
  }, [routePath]);

  const groupCount = activeFilterGroups(query, currentYear);
  const countText = `${totalCount} ${totalCount === 1 ? 'movie' : 'movies'}`;
  const headingText = libraryTitle(query, GenreDtoList(genresQuery.data), watchlistActive);

  return (
    <div className="library-view">
      <GenreTiles
        genres={GenreDtoList(genresQuery.data)}
        selectedIds={query.genreIds ?? []}
        onToggle={toggleGenre}
      />

      <div className="library-heading">
        <div className="library-heading-main">
          <h2 className="library-title lamu-section-heading">{headingText}</h2>
          <p className="library-count" role="status">
            {countText}
          </p>
          <span className="sr-only" aria-live="polite">
            Sort: {sortLabel(query.sort, query.direction)}
          </span>
        </div>
        <div className="library-controls">
          <div className="view-toggle" role="group" aria-label="Result layout">
            <button
              type="button"
              aria-pressed={variant === 'grid'}
              onClick={() => setVariant('grid')}
            >
              <GridIcon size={16} />
              <span>Grid</span>
            </button>
            <button
              type="button"
              aria-pressed={variant === 'list'}
              onClick={() => setVariant('list')}
            >
              <ListIcon size={16} />
              <span>List</span>
            </button>
          </div>
          <button
            ref={toggleRef}
            type="button"
            className="filters-toggle"
            aria-expanded={panelOpen}
            aria-controls={panelId}
            onClick={() => setPanelOpen((open) => !open)}
          >
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
            <ChevronDownIcon size={18} className="filters-toggle-chevron" />
          </button>
        </div>
      </div>

      {!panelOpen && chips.length > 0 ? (
        <div className="library-chips">
          <ul className="library-chip-list" aria-label="Active filters">
            {chips.map((chip) => (
              <li key={chip.key}>
                <span className="lamu-chip">
                  {chip.label}
                  <button
                    type="button"
                    className="lamu-chip-remove"
                    aria-label={chip.removeLabel}
                    onClick={chip.onRemove}
                  >
                    ×
                  </button>
                </span>
              </li>
            ))}
          </ul>
          <button type="button" className="lamu-link" onClick={onClearFilters}>
            Clear all
          </button>
        </div>
      ) : null}

      <div
        id={panelId}
        className="filter-collapse"
        data-open={String(panelOpen)}
        inert={!panelOpen}
      >
        <div className="filter-collapse-inner">
          <FilterPanel
            query={query}
            genres={GenreDtoList(genresQuery.data)}
            actors={PersonDtoList(actorsQuery.data)}
            onChange={onQueryChange}
            genresError={genresQuery.error}
            actorsError={actorsQuery.error}
            integrated={integrated}
            showTitleSearch={!integrated}
            matchCount={totalCount}
            onCollapse={collapsePanel}
            routeKey={routeKey}
          />
        </div>
      </div>

      <section aria-label="Results">
        {refreshFailed ? <p role="alert">Movies could not be refreshed.</p> : null}
        <ResultsBody
          loading={loading}
          mode={mode}
          hasItems={visibleItems.length > 0}
          totalCount={totalCount}
          page={filters.page}
          pageSize={filters.pageSize}
          filtersActive={activeFilters}
          tiles={tiles}
          variant={variant}
          focusSignal={focusSignal}
          onClearFilters={onClearFilters}
          onFirstPage={() => onPageChange(1)}
          onPageChange={onPageChange}
        />
      </section>
    </div>
  );
}

export interface LibraryViewProps {
  leadingItem?: MovieSummary | null;
  inWatchlist?: boolean;
  query?: FilterQuery;
  routeKey?: number;
  routePath?: string;
  onQueryChange?: (next: FilterQuery, options?: { replace?: boolean }) => void;
  onOpenMovie?: (id: string | number) => void;
}

export default function LibraryView({
  leadingItem,
  inWatchlist = false,
  query: controlledQuery,
  routeKey,
  routePath,
  onQueryChange,
  onOpenMovie,
}: LibraryViewProps) {
  const [internalQuery, setInternalQuery] = useState<FilterQuery>(() =>
    parseFilterQuery(window.location.search),
  );
  const [currentYear] = useState(() => currentYearExtent(new Date()));
  const integrated = controlledQuery != null;

  const syncFromUrl = useCallback(() => {
    if (!integrated) {
      setInternalQuery(parseFilterQuery(window.location.search));
    }
  }, [integrated]);

  useEffect(() => {
    if (integrated) {
      return;
    }
    window.addEventListener('popstate', syncFromUrl);
    return () => window.removeEventListener('popstate', syncFromUrl);
  }, [syncFromUrl, integrated]);

  const prevWatchlist = useRef(inWatchlist);
  useEffect(() => {
    if (prevWatchlist.current !== inWatchlist) {
      prevWatchlist.current = inWatchlist;
      syncFromUrl();
    }
  }, [inWatchlist, syncFromUrl]);

  const query = controlledQuery ?? internalQuery;

  const handleQueryChange = useCallback(
    (value: unknown) => {
      const next = parseFilterQuery(value);
      if (integrated) {
        onQueryChange?.(next);
      } else {
        setInternalQuery(next);
      }
    },
    [integrated, onQueryChange],
  );

  const handlePageChange = useCallback(
    (page: number) => {
      const next = parseFilterQuery(query);
      next.page = page;
      if (integrated) {
        onQueryChange?.(next);
      } else {
        replaceQuery(next, currentYear);
        setInternalQuery(next);
      }
    },
    [query, currentYear, integrated, onQueryChange],
  );

  const handleClearFilters = useCallback(() => {
    const next = parseFilterQuery({});
    next.unknown = { ...query.unknown };
    if (integrated) {
      onQueryChange?.(next);
    } else {
      replaceQuery(next, currentYear);
      setInternalQuery(next);
    }
  }, [query, currentYear, integrated, onQueryChange]);

  const apiUrl = useMemo(() => toCanonicalApiQuery(query, currentYear), [query, currentYear]);

  return (
    <LibraryErrorBoundary resetKey={apiUrl} clearFilters={handleClearFilters}>
      <LibraryBrowseView
        query={query}
        currentYear={currentYear}
        onQueryChange={handleQueryChange}
        onClearFilters={handleClearFilters}
        onPageChange={handlePageChange}
        leadingItem={leadingItem}
        inWatchlist={inWatchlist}
        routeKey={routeKey}
        routePath={routePath}
        integrated={integrated}
        onOpenMovie={onOpenMovie}
      />
    </LibraryErrorBoundary>
  );
}
