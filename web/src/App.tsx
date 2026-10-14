import { useMemo } from 'react';
import LibraryView from './features/library/LibraryView';
import MovieDetailsView from './features/movie-details/MovieDetailsView';

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
  return <LibraryView />;
}
