import { useLayoutEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import MovieCard, { type MovieSummaryResponse } from './MovieCard';
import MovieListRow from './MovieListRow';
import type { LibraryVariant } from './LibraryTile';

export type { MovieSummaryResponse } from './MovieCard';

export type MovieLike = MovieSummaryResponse;

const DEFAULT_PAGE_SIZE = 24;

export interface MovieGridProps {
  items?: MovieSummaryResponse[];

  tiles?: ReactNode;
  variant?: LibraryVariant;
  focusSignal?: number;
  totalCount?: number;
  page?: number;
  pageSize?: number;
  isLoading?: boolean;
  filtersActive?: boolean;
  onClearFilters?: () => void;
  onGoToFirstPage?: () => void;
  onOpenMovie?: (id: number | string) => void;
}

function resolvePageSize(pageSize: number | undefined, itemCount: number): number {
  if (pageSize !== undefined && pageSize > 0) {
    return pageSize;
  }
  return itemCount > 0 ? itemCount : DEFAULT_PAGE_SIZE;
}

function SkeletonGrid({ count, variant }: { count: number; variant: LibraryVariant }) {
  const containerClass = variant === 'list' ? 'library-list' : 'library-grid';
  return (
    <div className={containerClass} aria-busy="true">
      {Array.from({ length: count }).map((_, index) =>
        variant === 'list' ? (
          <div key={index} className="movie-skeleton movie-skeleton-row" aria-hidden="true" />
        ) : (
          <div
            key={index}
            className="movie-skeleton"
            style={{ aspectRatio: '2 / 3' }}
            aria-hidden="true"
          />
        ),
      )}
    </div>
  );
}

interface EmptyStateProps {
  totalCount: number;
  page: number;
  totalPages: number;
  filtersActive: boolean;
  onClearFilters?: () => void;
  onGoToFirstPage?: () => void;
}

function EmptyState({
  totalCount,
  page,
  totalPages,
  filtersActive,
  onClearFilters,
  onGoToFirstPage,
}: EmptyStateProps) {
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

  if (page > 1 && page > totalPages) {
    return (
      <section className="movie-grid-empty">
        <p>This page is past the end of the results.</p>
        <button type="button" onClick={onGoToFirstPage}>
          Go to page 1
        </button>
        {totalCount === 0 || filtersActive ? (
          <button type="button" onClick={onClearFilters}>
            Clear filters
          </button>
        ) : null}
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

export default function MovieGrid({
  items = [],
  tiles,
  variant = 'grid',
  focusSignal = 0,
  totalCount,
  page = 1,
  pageSize,
  isLoading = false,
  filtersActive = false,
  onClearFilters,
  onGoToFirstPage,
  onOpenMovie,
}: MovieGridProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    if (focusSignal !== 0) {
      containerRef.current?.focus();
    }
  }, [focusSignal]);

  const resolvedPageSize = resolvePageSize(pageSize, items.length);
  const resolvedTotal = totalCount ?? items.length;
  const totalPages = Math.max(1, Math.ceil(resolvedTotal / resolvedPageSize));
  const hasTiles = tiles !== undefined;

  if (isLoading) {
    return <SkeletonGrid count={resolvedPageSize} variant={variant} />;
  }

  if (!hasTiles && items.length === 0) {
    return (
      <EmptyState
        totalCount={resolvedTotal}
        page={page}
        totalPages={totalPages}
        filtersActive={filtersActive}
        onClearFilters={onClearFilters}
        onGoToFirstPage={onGoToFirstPage}
      />
    );
  }

  const containerClass = variant === 'list' ? 'library-list' : 'library-grid';

  return (
    <div
      className={containerClass}
      role="list"
      aria-label="Movies"
      aria-live="polite"
      tabIndex={-1}
      ref={containerRef}
    >
      {hasTiles
        ? tiles
        : items.map((movie) => (
            <div key={String(movie.id)} role="listitem">
              {variant === 'list' ? (
                <MovieListRow movie={movie} onOpenMovie={onOpenMovie} />
              ) : (
                <MovieCard movie={movie} onOpenMovie={onOpenMovie} />
              )}
            </div>
          ))}
    </div>
  );
}
