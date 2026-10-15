import { useMemo } from 'react';
import MovieDetailsView from './features/movie-details/MovieDetailsView';
import LibraryView from './library/LibraryView';

function parseMovieId(pathname: string): number | null {
  const match = /^\/movies\/(\d+)$/.exec(pathname);
  if (!match) return null;
  const id = Number(match[1]);
  return Number.isInteger(id) && id > 0 ? id : null;
}

export default function App() {
  const movieId = useMemo(() => parseMovieId(window.location.pathname), []);
  if (movieId !== null) {
    return <MovieDetailsView movieId={movieId} />;
  }
  return (
    <div>
      <header>
        <h1>LamuFlix</h1>
        <button type="button">Import folder</button>
      </header>
      <LibraryView />
    </div>
  );
}
