import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { client } from '../api/client';
import type { components } from '../api/types';
import FilterPanel from '../features/library/FilterPanel';
import {
  currentYearExtent,
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
import {
  GENERIC_RETRY_ERROR,
  RetryRowError,
  useBrowseMoviesQuery,
  useRetryEnrichmentMutation,
} from './libraryQueries';
import type { BrowseMoviesQueryError } from './libraryQueries';
import MovieCard from './MovieCard';
import MovieGrid from './MovieGrid';
import type { MovieLike } from './MovieGrid';
import Pagination from './Pagination';
import { WatchlistToggleButton } from './watchlist';
import { useWatchlistToggle } from './watchlistStore';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];

const lastGoodQueryKey = ['libraryLastGoodBrowse'];

function GenreDtoList(value: unknown): components['schemas']['GenreDto'][] {
  return Array.isArray(value) ? (value as components['schemas']['GenreDto'][]) : [];
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

function canRetry(status: MovieSummary['enrichmentStatus']): boolean {
  return status === 'NotFound' || status === 'Failed';
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

interface LibraryTileProps {
  item: MovieLike;
  filterActive: boolean;
  onRemoved: () => void;
  onHiddenChange: (id: string, hidden: boolean) => void;
  detailsLink?: ReactNode;
}

function LibraryTile({
  item,
  filterActive,
  onRemoved,
  onHiddenChange,
  detailsLink,
}: LibraryTileProps) {
  const errorId = useId();
  const [tileError, setTileError] = useState<string | null>(null);
  const retryEnrichment = useRetryEnrichmentMutation();
  const { isInWatchlist, pending } = useWatchlistToggle(item.id, item.isInWatchlist);
  const removed = filterActive && !pending && !isInWatchlist;
  const wasRemoved = useRef(removed);

  useEffect(() => {
    if (removed && !wasRemoved.current) {
      onRemoved();
    }
    wasRemoved.current = removed;
  }, [removed, onRemoved]);

  useEffect(() => {
    onHiddenChange(String(item.id), removed);
  }, [removed, item.id, onHiddenChange]);

  async function onRetry() {
    setTileError(null);
    try {
      await retryEnrichment.mutateAsync(item.id);
    } catch (error) {
      setTileError(error instanceof RetryRowError ? error.message : GENERIC_RETRY_ERROR);
    }
  }

  if (removed) {
    return null;
  }

  const title = String(item.title ?? 'Untitled');

  return (
    <MovieCard
      movie={item}
      actions={
        <>
          {detailsLink}
          {canRetry(item.enrichmentStatus) ? (
            <button
              type="button"
              aria-label={`Retry ${title}`}
              aria-describedby={tileError ? errorId : undefined}
              onClick={() => void onRetry()}
            >
              Retry
            </button>
          ) : null}
          <WatchlistToggleButton movieId={item.id} isInWatchlist={item.isInWatchlist} />
          {tileError ? (
            <p id={errorId} role="alert">
              {tileError}
            </p>
          ) : null}
        </>
      }
    />
  );
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

interface ResultsBodyProps {
  loading: boolean;
  mode: ResultsMode;
  hasItems: boolean;
  tiles: ReactNode;
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
    return <MovieGrid isLoading pageSize={props.pageSize} />;
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
      visibleItems.map((movie) => {
        const title = String(movie.title ?? 'Untitled');
        return (
          <div key={String(movie.id)} role="listitem">
            <LibraryTile
              item={movie}
              filterActive={watchlistActive}
              onRemoved={focusGrid}
              onHiddenChange={setTileHidden}
              detailsLink={
                openMovie ? (
                  <a
                    href={`/movies/${String(movie.id)}`}
                    aria-label={`Open details for ${title}`}
                    onClick={(event) => {
                      event.preventDefault();
                      openMovie(movie.id as string | number);
                    }}
                  >
                    Details
                  </a>
                ) : (
                  <a href={`/movies/${String(movie.id)}`}>Details</a>
                )
              }
            />
          </div>
        );
      }),
    [visibleItems, watchlistActive, focusGrid, setTileHidden, openMovie],
  );

  const removeValue = useCallback(
    (mutate: (next: FilterQuery) => void) => {
      const next = parseFilterQuery(query);
      mutate(next);
      next.page = 1;
      onQueryChange(next);
    },
    [query, onQueryChange],
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
      pushChip(
        list,
        `genre-${value}-${index}`,
        `Genre: ${value}`,
        `Remove genre filter ${value}`,
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
      pushChip(
        list,
        `actor-${value}-${index}`,
        `Cast: ${value}`,
        `Remove cast filter ${value}`,
        () =>
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
  }, [query, removeValue, currentYear]);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [isDesktop, setIsDesktop] = useState(
    () =>
      typeof window.matchMedia !== 'function' || window.matchMedia('(min-width: 48rem)').matches,
  );
  const filterOpenRef = useRef<HTMLButtonElement>(null);
  const drawerRef = useRef<HTMLDivElement>(null);

  const closeDrawer = useCallback(() => {
    setDrawerOpen(false);
    filterOpenRef.current?.focus();
  }, []);

  useEffect(() => {
    if (!drawerOpen) {
      return;
    }
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const node = drawerRef.current;
    const focusables = node?.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    );
    focusables?.[0]?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        closeDrawer();
        return;
      }
      if (event.key !== 'Tab' || !node) {
        return;
      }
      const items = Array.from(
        node.querySelectorAll<HTMLElement>(
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
        ),
      ).filter((item) => !item.hasAttribute('disabled'));
      if (items.length === 0) {
        return;
      }
      const first = items[0];
      const last = items[items.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [drawerOpen, closeDrawer]);

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') {
      return;
    }
    const media = window.matchMedia('(min-width: 48rem)');
    const onChange = (event: MediaQueryListEvent) => {
      setIsDesktop(event.matches);
      setDrawerOpen(false);
      document.body.style.overflow = '';
    };
    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);

  const prevRoutePathRef = useRef(routePath);
  useEffect(() => {
    if (routePath == null || prevRoutePathRef.current === routePath) {
      prevRoutePathRef.current = routePath;
      return;
    }
    prevRoutePathRef.current = routePath;
    setDrawerOpen(false);
    document.body.style.overflow = '';
  }, [routePath]);

  useEffect(
    () => () => {
      document.body.style.overflow = '';
    },
    [],
  );

  const sortValue = sortValueOf(query.sort, query.direction);
  const handleToolbarSort = useCallback(
    (value: string) => {
      const [sort, direction] = value.split('|');
      const next = parseFilterQuery(query);
      next.sort = sort;
      next.direction = direction;
      next.page = 1;
      onQueryChange(next);
    },
    [query, onQueryChange],
  );

  const filterPanel = (
    <FilterPanel
      query={query}
      genres={GenreDtoList(genresQuery.data)}
      actors={PersonDtoList(actorsQuery.data)}
      onChange={onQueryChange}
      genresError={genresQuery.error}
      actorsError={actorsQuery.error}
      integrated={integrated}
      showTitleSearch={!integrated}
      showSort={!integrated}
      routeKey={routeKey}
    />
  );

  const backgroundInert = !isDesktop && drawerOpen;

  return (
    <>
      <div className="lamu-browse" inert={backgroundInert}>
        {isDesktop ? (
          !sidebarCollapsed ? (
            <div className="lamu-filter-sidebar">
              <button type="button" onClick={() => setSidebarCollapsed(true)}>
                Collapse filters
              </button>
              {filterPanel}
            </div>
          ) : (
            <div>
              <button type="button" onClick={() => setSidebarCollapsed(false)}>
                Show filters
              </button>
            </div>
          )
        ) : null}
        <div className="library-view">
          <div className="lamu-results-head">
            {!isDesktop ? (
              <button
                ref={filterOpenRef}
                type="button"
                className="lamu-filter-open-button"
                aria-haspopup="dialog"
                onClick={() => setDrawerOpen(true)}
              >
                Filters
              </button>
            ) : null}
            <p role="status">
              {totalCount} {totalCount === 1 ? 'movie' : 'movies'}
            </p>
            {integrated ? (
              <label>
                Sort:{' '}
                <select
                  aria-label="Sort movies"
                  value={sortValue}
                  onChange={(event) => handleToolbarSort(event.target.value)}
                >
                  {Object.entries(SORT_LABELS).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
            <span aria-live="polite">Sort: {sortLabel(query.sort, query.direction)}</span>
          </div>
          {chips.length > 0 ? (
            <ul className="lamu-chip-list" aria-label="Active filters">
              {chips.map((chip) => (
                <li key={chip.key}>
                  <span className="lamu-chip">
                    {chip.label}
                    <button type="button" aria-label={chip.removeLabel} onClick={chip.onRemove}>
                      ×
                    </button>
                  </span>
                </li>
              ))}
            </ul>
          ) : null}
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
              focusSignal={focusSignal}
              onClearFilters={onClearFilters}
              onFirstPage={() => onPageChange(1)}
              onPageChange={onPageChange}
            />
          </section>
        </div>
      </div>
      {!isDesktop && drawerOpen ? (
        <button
          type="button"
          className="lamu-filter-drawer-backdrop"
          aria-label="Close filters"
          onClick={closeDrawer}
        />
      ) : null}
      {!isDesktop && drawerOpen ? (
        <div
          ref={drawerRef}
          className="lamu-filter-drawer"
          role="dialog"
          aria-modal="true"
          aria-label="Filters"
        >
          <button type="button" onClick={closeDrawer}>
            Done
          </button>
          {filterPanel}
        </div>
      ) : null}
    </>
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
