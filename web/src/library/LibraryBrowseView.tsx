import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { components } from '../api/types';
import FilterPanel from '../features/library/FilterPanel';
import {
  isRatingActive,
  isRuntimeActive,
  isYearActive,
  parseFilterQuery,
  parseSortSelection,
  toUrlQuery,
} from '../features/library/filterQuery';
import type { FilterQuery } from '../features/library/filterQuery';
import { buildBrowseParams } from './browseParams';
import { useBrowseMoviesQuery } from './libraryQueries';
import type { BrowseMoviesQueryError } from './libraryQueries';
import LibraryTile from './LibraryTile';
import type { LibraryVariant } from './LibraryTile';
import { buildFilterChips, hasDirector, LibraryFilterSummary } from './LibraryFilterSummary';
import LibraryResultsBody from './LibraryResultsBody';
import LibraryResultsToolbar from './LibraryResultsToolbar';
import { useAmbientPosters } from '../ui/ambient';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type GenreDto = components['schemas']['GenreDto'];
type CollectionDto = components['schemas']['CollectionDto'];
type PersonDto = components['schemas']['PersonDto'];
type DirectorDto = components['schemas']['DirectorDto'];

const lastGoodQueryKey = ['libraryLastGoodBrowse'];

function isWatchlistFlag(value: unknown): boolean {
  if (typeof value === 'boolean') {
    return value;
  }
  if (value == null) {
    return false;
  }
  return String(value).toLowerCase() === 'true';
}

function activeFilterGroups(query: FilterQuery, currentYear: number): number {
  const groups = [
    (query.genreIds ?? []).length > 0,
    (query.actorIds ?? []).length > 0,
    hasDirector(query),
    isRuntimeActive(query.runtimeMin, query.runtimeMax, query.runtimeIncludeUnknown),
    isYearActive(query.yearMin, query.yearMax, currentYear),
    isRatingActive(query.minRating, query.maxRating),
    (query.statuses ?? []).length > 0,
    query.collectionId != null,
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

export interface LibraryBrowseViewProps {
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
  genres: GenreDto[];
  collections: CollectionDto[];
  actors: PersonDto[];
  directors: DirectorDto[];
  genresError?: unknown;
  collectionsError?: unknown;
  actorsError?: unknown;
  directorsError?: unknown;
  directorsLoading?: boolean;
  onRetryDirectors?: () => void;
}

export function LibraryBrowseView({
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
  genres,
  collections,
  actors,
  directors,
  genresError,
  collectionsError,
  actorsError,
  directorsError,
  directorsLoading,
  onRetryDirectors,
}: LibraryBrowseViewProps) {
  const filters = useMemo(() => buildBrowseParams(query, currentYear), [query, currentYear]);
  const watchlistActive = inWatchlist || isWatchlistFlag(query.inWatchlist);
  const browse = useBrowseMoviesQuery(watchlistActive, filters);

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
  const [panelOpen, setPanelOpen] = useState(
    () =>
      typeof window.matchMedia === 'function' && window.matchMedia('(min-width: 64rem)').matches,
  );
  const panelId = useId();
  const toggleRef = useRef<HTMLButtonElement>(null);

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
  const mode =
    visibleItems.length > 0 || pastEnd || activeFilters
      ? 'grid'
      : watchlistActive
        ? 'watchlistEmpty'
        : 'libraryEmpty';
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

  const handleSortChange = useCallback(
    (value: string) => {
      const selection = parseSortSelection(value);
      if (!selection) {
        return;
      }
      const next = parseFilterQuery(query);
      next.sort = selection.sort;
      next.direction = selection.direction;
      next.page = 1;
      onQueryChange(next);
    },
    [query, onQueryChange],
  );

  const genreNameById = useMemo(
    () => new Map(genres.map((genre) => [String(genre.id), genre.name])),
    [genres],
  );
  const actorNameById = useMemo(
    () => new Map(actors.map((actor) => [String(actor.id), String(actor.name ?? '')])),
    [actors],
  );
  const directorNameById = useMemo(
    () => new Map(directors.map((director) => [String(director.id), String(director.name ?? '')])),
    [directors],
  );
  const collectionNameById = useMemo(
    () => new Map(collections.map((item) => [String(item.id), String(item.name ?? '')])),
    [collections],
  );

  const chips = useMemo(
    () =>
      buildFilterChips(query, currentYear, {
        genreNameById,
        actorNameById,
        directorNameById,
        collectionNameById,
      }),
    [query, currentYear, genreNameById, actorNameById, directorNameById, collectionNameById],
  );

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
  const headingText = libraryTitle(query, genres, watchlistActive);

  return (
    <div className="library-view">
      <h2 className="library-title sr-only">{headingText}</h2>

      <LibraryFilterSummary
        chips={chips}
        onRemove={(chip) => removeValue(chip.clear)}
        onClearAll={onClearFilters}
      />

      <div className="library-body" data-rail={panelOpen ? 'open' : 'closed'}>
        <aside
          id={panelId}
          className="filter-collapse"
          data-open={String(panelOpen)}
          inert={!panelOpen}
        >
          <FilterPanel
            query={query}
            genres={genres}
            collections={collections}
            actors={actors}
            directors={directors}
            onChange={onQueryChange}
            genresError={genresError}
            collectionsError={collectionsError}
            actorsError={actorsError}
            directorError={directorsError}
            directorLoading={directorsLoading}
            onRetryDirectors={onRetryDirectors}
            integrated={integrated}
            showTitleSearch={!integrated}
            showSort={false}
            showFooter={false}
            matchCount={totalCount}
            routeKey={routeKey}
          />
        </aside>

        <div className="library-main">
          <LibraryResultsToolbar
            toggleRef={toggleRef}
            panelOpen={panelOpen}
            panelId={panelId}
            groupCount={groupCount}
            countText={countText}
            sort={query.sort}
            direction={query.direction}
            variant={variant}
            onTogglePanel={() => setPanelOpen((open) => !open)}
            onSortChange={handleSortChange}
            onVariantChange={setVariant}
          />

          <section aria-label="Results">
            {refreshFailed ? <p role="alert">Movies could not be refreshed.</p> : null}
            <LibraryResultsBody
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
      </div>
    </div>
  );
}

export default LibraryBrowseView;
