import { useQueryClient } from '@tanstack/react-query';
import { useMemo, useRef, useState } from 'react';
import type { components } from './api/types';
import MovieDetailsView from './features/movie-details/MovieDetailsView';
import ImportDialog from './library/ImportDialog';
import { browseMoviesQueryKey, pendingRow, prependPendingRow } from './library/libraryQueries';
import LibraryView from './library/LibraryView';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];

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
  const queryClient = useQueryClient();
  const movieId = useMemo(() => parseMovieId(window.location.pathname), []);

  function closeImport() {
    setImportOpen(false);
    importFolderButtonRef.current?.focus();
  }

  function prependImportedMovie(id: string, title: string) {
    const row = pendingRow(id, title);
    setImportedRow(row);
    queryClient.setQueryData<BrowseMoviesResponse>(browseMoviesQueryKey, (current) =>
      prependPendingRow(current, row),
    );
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
      </header>
      <LibraryView leadingItem={importedRow} />
      <ImportDialog open={importOpen} onClose={closeImport} onImported={prependImportedMovie} />
    </div>
  );
}
