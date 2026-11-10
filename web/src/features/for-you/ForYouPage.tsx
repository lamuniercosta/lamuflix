import { useId } from 'react';
import MovieCard from '../../library/MovieCard';
import '../../library/library.css';
import { useForYou } from './forYouApi';
import './ForYouPage.css';

export interface ForYouPageProps {
  onOpenMovie?: (id: number | string) => void;
}

export default function ForYouPage({ onOpenMovie }: ForYouPageProps) {
  const query = useForYou();
  const headingId = useId();
  const rows = query.data ?? [];

  return (
    <section className="for-you-page" aria-labelledby={headingId}>
      <h1 id={headingId} className="lamu-section-heading">
        For You
      </h1>
      {query.isPending ? <p role="status">Loading recommendations</p> : null}
      {query.isError ? (
        <>
          <p role="alert">Could not load recommendations.</p>
          <button type="button" className="lamu-action-quiet" onClick={() => query.refetch()}>
            Retry
          </button>
        </>
      ) : null}
      {query.isSuccess && rows.length === 0 ? (
        <p>No recommendations yet. Mark movies as watched or liked to get some.</p>
      ) : null}
      {rows.length > 0 ? (
        <div className="library-grid" role="list" aria-label="Recommended movies">
          {rows.map((movie) => (
            <div key={String(movie.id)} role="listitem">
              <MovieCard movie={movie} reasons={movie.reasons} onOpenMovie={onOpenMovie} />
            </div>
          ))}
        </div>
      ) : null}
    </section>
  );
}
