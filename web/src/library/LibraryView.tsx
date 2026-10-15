import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { client } from '../api/client';
import type { components } from '../api/types';
import FilterPanel from '../features/library/FilterPanel';
import {
  currentYearExtent,
  parseFilterQuery,
  toCanonicalApiQuery,
  toUrlQuery,
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
}

function LibraryTile({ item, filterActive, onRemoved, onHiddenChange }: LibraryTileProps) {
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
}

function LibraryBrowseView({
  query,
  currentYear,
  onQueryChange,
  onClearFilters,
  onPageChange,
  leadingItem,
  inWatchlist = false,
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

  const [lastGood, setLastGood] = useState<BrowseMoviesResponse | null>(null);
  useEffect(() => {
    if (browse.data) {
      setLastGood(browse.data);
    }
  }, [browse.data]);
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
  const tiles = useMemo(
    () =>
      visibleItems.map((movie) => (
        <div key={String(movie.id)} role="listitem">
          <LibraryTile
            item={movie}
            filterActive={watchlistActive}
            onRemoved={focusGrid}
            onHiddenChange={setTileHidden}
          />
        </div>
      )),
    [visibleItems, watchlistActive, focusGrid, setTileHidden],
  );

  return (
    <div className="library-view">
      <FilterPanel
        query={query}
        genres={GenreDtoList(genresQuery.data)}
        actors={PersonDtoList(actorsQuery.data)}
        onChange={onQueryChange}
        genresError={genresQuery.error}
        actorsError={actorsQuery.error}
      />
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
  );
}

export default function LibraryView({
  leadingItem,
  inWatchlist = false,
}: {
  leadingItem?: MovieSummary | null;
  inWatchlist?: boolean;
}) {
  const [query, setQuery] = useState<FilterQuery>(() => parseFilterQuery(window.location.search));
  const [currentYear] = useState(() => currentYearExtent(new Date()));

  const syncFromUrl = useCallback(() => {
    setQuery(parseFilterQuery(window.location.search));
  }, []);

  useEffect(() => {
    window.addEventListener('popstate', syncFromUrl);
    return () => window.removeEventListener('popstate', syncFromUrl);
  }, [syncFromUrl]);

  const prevWatchlist = useRef(inWatchlist);
  useEffect(() => {
    if (prevWatchlist.current !== inWatchlist) {
      prevWatchlist.current = inWatchlist;
      syncFromUrl();
    }
  }, [inWatchlist, syncFromUrl]);

  const handleQueryChange = useCallback((value: unknown) => {
    setQuery(parseFilterQuery(value));
  }, []);

  const handlePageChange = useCallback(
    (page: number) => {
      const next = parseFilterQuery(query);
      next.page = page;
      replaceQuery(next, currentYear);
      setQuery(next);
    },
    [query, currentYear],
  );

  const handleClearFilters = useCallback(() => {
    const next = parseFilterQuery({});
    next.unknown = { ...query.unknown };
    replaceQuery(next, currentYear);
    setQuery(next);
  }, [query, currentYear]);

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
      />
    </LibraryErrorBoundary>
  );
}
