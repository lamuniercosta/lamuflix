import { useMemo, useRef, useState } from 'react';
import type { components } from './api/types';
import MovieDetailsView from './features/movie-details/MovieDetailsView';
import ImportDialog from './library/ImportDialog';
import LibraryErrorBoundary from './library/LibraryErrorBoundary';
import { armImportPlaceholder, pendingRow } from './library/libraryQueries';
import LibraryView from './library/LibraryView';
import { useWatchlistFilter } from './library/useWatchlistFilter';

type MovieSummary = components['schemas']['MovieSummaryResponse'];

function parseMovieId(pathname: string): number | null {
  const match = /^\/movies\/(\d+)$/.exec(pathname);
  if (!match) return null;
  const id = Number(match[1]);
  return Number.isInteger(id) && id > 0 ? id : null;
}

export default function App() {
  const [importOpen, setImportOpen] = useState(false);
  const [importedRow, setImportedRow] = useState<MovieSummary | null>(null);
  const importFolderButtonRef = useRef<HTMLButtonElement>(null);
  const movieId = useMemo(() => parseMovieId(window.location.pathname), []);
  const { active: watchlistActive, toggle: toggleWatchlist } = useWatchlistFilter();

  function closeImport() {
    setImportOpen(false);
    importFolderButtonRef.current?.focus();
  }

  function prependImportedMovie(id: string, title: string) {
    setImportedRow(pendingRow(id, title));
    armImportPlaceholder(() => setImportedRow(null));
  }

  if (movieId !== null) {
    return <MovieDetailsView movieId={movieId} />;
  }

  return (
    <div>
      <header>
        <h1>LamuFlix</h1>
        <button ref={importFolderButtonRef} type="button" onClick={() => setImportOpen(true)}>
          Import folder
        </button>
        <button type="button" aria-pressed={watchlistActive} onClick={toggleWatchlist}>
          Watchlist
        </button>
      </header>
      <LibraryErrorBoundary>
        <LibraryView leadingItem={importedRow} inWatchlist={watchlistActive} />
      </LibraryErrorBoundary>
      <ImportDialog open={importOpen} onClose={closeImport} onImported={prependImportedMovie} />
    </div>
  );
}
