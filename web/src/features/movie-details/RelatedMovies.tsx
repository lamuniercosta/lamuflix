import { useState, type MouseEvent } from 'react';
import { FilmIcon } from '../../ui/icons';
import { MovieCarousel } from '../../ui/MovieCarousel';
import { readReturnUrl } from '../app-navigation/useAppNavigation';
import {
  toRecommendationMovieId,
  useRelatedMovies,
  type RecommendedMovie,
  type RecommendationMovieId,
} from './relatedMoviesApi';
import './RelatedMovies.css';

const SECTION_LABEL = 'Related movies';
const LOADING_LABEL = 'Loading related movies…';
const EMPTY_LABEL = 'No related movies yet.';
const ERROR_LABEL = 'Could not load related movies.';

function navigateTo(href: string, state: unknown = {}): void {
  window.history.pushState(state, '', href);
  window.dispatchEvent(new PopStateEvent('popstate'));
}

function openRecommendedMovie(id: RecommendedMovie['id']): void {
  const returnUrl = readReturnUrl(window.history.state);
  navigateTo(`/movies/${String(id)}`, returnUrl ? { libraryReturnUrl: returnUrl } : {});
}

function RelatedMovieCard({ movie }: { movie: RecommendedMovie }) {
  const [posterFailed, setPosterFailed] = useState(false);
  const posterUrl = movie.posterUrl;
  const hasYear = movie.releaseYear !== null && String(movie.releaseYear).trim() !== '';
  const open = (event: MouseEvent<HTMLAnchorElement>) => {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }
    event.preventDefault();
    openRecommendedMovie(movie.id);
  };
  return (
    <article className="related-movie-card">
      <a className="related-movie-link" href={`/movies/${String(movie.id)}`} onClick={open}>
        <span className="related-movie-poster">
          {posterUrl !== null && !posterFailed ? (
            <img src={posterUrl} alt="" loading="lazy" onError={() => setPosterFailed(true)} />
          ) : (
            <span className="related-movie-placeholder lamu-glass" aria-hidden="true">
              <FilmIcon size={28} />
            </span>
          )}
        </span>
        <span className="related-movie-title">{movie.title}</span>
        {hasYear && <span className="related-movie-year">{String(movie.releaseYear)}</span>}
        {movie.reasons.length > 0 && (
          <span className="related-movie-reasons">
            {movie.reasons.map((reason, index) => (
              <span className="related-movie-reason" key={`${index}-${reason}`}>
                {reason}
              </span>
            ))}
          </span>
        )}
      </a>
    </article>
  );
}

export interface RelatedMoviesProps {
  movieId: RecommendationMovieId | null | undefined;
}

export default function RelatedMovies({ movieId }: RelatedMoviesProps) {
  const id = toRecommendationMovieId(movieId);
  const query = useRelatedMovies(id);
  const rows = query.data ?? [];

  if (id === null) {
    return null;
  }

  return (
    <section className="details-section related-movies">
      <h2 className="details-section-heading">{SECTION_LABEL}</h2>
      {query.isPending && (
        <p role="status" className="details-status">
          {LOADING_LABEL}
        </p>
      )}
      {query.isError && (
        <div className="related-movies-error">
          <p role="alert" className="details-status">
            {ERROR_LABEL}
          </p>
          <button
            type="button"
            className="lamu-pill-button"
            disabled={query.isFetching}
            onClick={() => void query.refetch()}
          >
            Retry
          </button>
        </div>
      )}
      {query.isSuccess && rows.length === 0 && <p className="details-status">{EMPTY_LABEL}</p>}
      {rows.length > 0 && (
        <MovieCarousel label={SECTION_LABEL}>
          {rows.map((movie) => (
            <RelatedMovieCard key={String(movie.id)} movie={movie} />
          ))}
        </MovieCarousel>
      )}
    </section>
  );
}
