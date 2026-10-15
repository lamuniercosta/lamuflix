import { useLayoutEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import MovieCard from './MovieCard';
import type { components } from '../api/types';

export type MovieSummaryResponse = components['schemas']['MovieSummaryResponse'];

export type MovieLike = MovieSummaryResponse & Record<string, unknown>;

function isRecord(value: unknown): value is MovieLike {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

export interface MovieGridProps {
  items?: unknown[];
  tiles?: ReactNode;
  focusSignal?: number;
  [key: string]: unknown;
}

function pick(props: MovieGridProps, names: readonly string[]): unknown {
  for (const name of names) {
    if (Object.prototype.hasOwnProperty.call(props, name) && props[name] !== undefined) {
      return props[name];
    }
  }
  return undefined;
}

function toNumber(value: unknown, fallback: number): number {
  if (value === null || value === undefined || value === '') return fallback;
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function toBoolean(value: unknown, fallback = false): boolean {
  if (typeof value === 'boolean') return value;
  if (typeof value === 'number') return value !== 0;
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase();
    if (normalized === 'true' || normalized === '1' || normalized === 'yes') return true;
    if (normalized === 'false' || normalized === '0' || normalized === 'no' || normalized === '')
      return false;
  }
  if (Array.isArray(value)) return value.length > 0;
  if (isRecord(value)) return Object.keys(value).length > 0;
  return fallback;
}

function asFunction(value: unknown): (() => void) | undefined {
  return typeof value === 'function' ? (value as () => void) : undefined;
}

function extractItems(props: MovieGridProps): MovieLike[] {
  const direct = pick(props, [
    'items',
    'movies',
    'data',
    'results',
    'cards',
    'tiles',
    'list',
    'value',
    'entries',
  ]);
  if (Array.isArray(direct)) return direct as MovieLike[];
  if (isRecord(direct) && Array.isArray((direct as MovieLike).items)) {
    return (direct as MovieLike).items as MovieLike[];
  }
  if (isRecord(direct) && Array.isArray((direct as MovieLike).movies)) {
    return (direct as MovieLike).movies as MovieLike[];
  }
  if (Array.isArray(props.children)) return props.children as unknown as MovieLike[];
  return [];
}

export default function MovieGrid(props: MovieGridProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const focusSignal = toNumber(pick(props, ['focusSignal']), 0);

  useLayoutEffect(() => {
    if (focusSignal !== 0) {
      containerRef.current?.focus();
    }
  }, [focusSignal]);

  const items = extractItems(props);
  const totalCount = toNumber(
    pick(props, ['totalCount', 'total', 'totalItems', 'total_count', 'count', 'resultCount']),
    items.length,
  );
  const page = toNumber(pick(props, ['page', 'currentPage', 'pageNumber']), 1);
  const pageSize = toNumber(
    pick(props, ['pageSize', 'size', 'limit', 'perPage']),
    items.length > 0 ? items.length : 24,
  );
  const loading = toBoolean(
    pick(props, ['isLoading', 'loading', 'isFetching', 'fetching', 'pending', 'isLoadingTiles']),
  );
  const filtersActive = toBoolean(
    pick(props, [
      'filtersActive',
      'hasActiveFilters',
      'hasFilters',
      'isFiltered',
      'filtered',
      'activeFilters',
    ]),
    false,
  );
  const onClearFilters = asFunction(
    pick(props, [
      'onClearFilters',
      'clearFilters',
      'onClear',
      'clear',
      'resetFilters',
      'onResetFilters',
    ]),
  );
  const onGoToFirstPage = asFunction(
    pick(props, [
      'onGoToFirstPage',
      'goToFirstPage',
      'goToPageOne',
      'onGoToPageOne',
      'returnToFirstPage',
      'onReturnToFirstPage',
    ]),
  );

  const totalPages = pageSize > 0 ? Math.max(1, Math.ceil(totalCount / pageSize)) : 1;
  const isPastEnd = items.length === 0 && page > 1 && page > totalPages;
  const tiles = props.tiles as ReactNode | undefined;
  const hasTiles = tiles !== undefined;

  if (loading) {
    const skeletonCount = toNumber(
      pick(props, ['skeletonCount', 'skeletons']),
      pageSize > 0 ? pageSize : 24,
    );
    return (
      <div className="library-grid" aria-busy="true">
        {Array.from({ length: skeletonCount }).map((_, index) => (
          <div
            key={index}
            className="movie-skeleton"
            style={{ aspectRatio: '2 / 3' }}
            aria-hidden="true"
          />
        ))}
      </div>
    );
  }

  if (!hasTiles && items.length === 0) {
    if (totalCount === 0 && !filtersActive && page <= 1) {
      return (
        <section className="movie-grid-empty">
          <p>Your library is empty.</p>
          <p>Titles added to your library will show up here.</p>
          <button type="button" onClick={onClearFilters}>
            Clear filters
          </button>
        </section>
      );
    }

    if (totalCount === 0 && filtersActive) {
      return (
        <section className="movie-grid-empty">
          <p>No titles match these filters.</p>
          <button type="button" onClick={onClearFilters}>
            Clear filters
          </button>
        </section>
      );
    }

    if (isPastEnd) {
      return (
        <section className="movie-grid-empty">
          <p>This page is past the end of the results.</p>
          <button type="button" onClick={onGoToFirstPage}>
            Go to page 1
          </button>
          {(totalCount === 0 || filtersActive) && (
            <button type="button" onClick={onClearFilters}>
              Clear filters
            </button>
          )}
        </section>
      );
    }

    if (filtersActive) {
      return (
        <section className="movie-grid-empty">
          <p>No titles match these filters.</p>
          <button type="button" onClick={onClearFilters}>
            Clear filters
          </button>
        </section>
      );
    }

    return (
      <section className="movie-grid-empty">
        <p>Your library is empty.</p>
        <p>Titles added to your library will show up here.</p>
        <button type="button" onClick={onClearFilters}>
          Clear filters
        </button>
      </section>
    );
  }

  return (
    <div
      className="library-grid"
      role="list"
      aria-label="Movies"
      aria-live="polite"
      tabIndex={-1}
      ref={containerRef}
    >
      {hasTiles ? (
        tiles
      ) : (
        <>
          {items.map((movie, index) => (
            <div key={String((movie as Record<string, unknown>).id ?? index)} role="listitem">
              <MovieCard movie={movie as unknown as MovieLike} />
            </div>
          ))}
        </>
      )}
    </div>
  );
}
