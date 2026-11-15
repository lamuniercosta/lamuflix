import type { ReactNode } from 'react';
import MovieGrid from './MovieGrid';
import Pagination from './Pagination';
import type { LibraryVariant } from './LibraryTile';

export type ResultsMode = 'grid' | 'watchlistEmpty' | 'libraryEmpty';

export interface LibraryResultsBodyProps {
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

export function LibraryResultsBody(props: LibraryResultsBodyProps) {
  if (props.loading) {
    return <MovieGrid isLoading pageSize={props.pageSize} variant={props.variant} />;
  }
  if (props.mode === 'watchlistEmpty') {
    return <p>Your watchlist is empty.</p>;
  }
  if (props.mode === 'libraryEmpty') {
    return <p>No movies yet. Bulk import the library to get started.</p>;
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

export default LibraryResultsBody;
