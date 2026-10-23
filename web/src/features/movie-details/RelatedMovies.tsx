import { useEffect, useMemo, useRef, type FocusEvent } from 'react';
import { useQueryClient, type QueryClient } from '@tanstack/react-query';
import MovieCard from '../../library/MovieCard';
import { MovieCarousel } from '../../ui/MovieCarousel';
import { readReturnUrl } from '../app-navigation/useAppNavigation';
import {
  toRecommendationMovieId,
  useRelatedMovies,
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

function openRecommendedMovie(id: number | string): void {
  const returnUrl = readReturnUrl(window.history.state);
  navigateTo(`/movies/${String(id)}`, returnUrl ? { libraryReturnUrl: returnUrl } : {});
}

export interface RelatedMoviesProps {
  movieId: RecommendationMovieId | null | undefined;
  libraryClient?: QueryClient;
}

export default function RelatedMovies({ movieId, libraryClient }: RelatedMoviesProps) {
  const id = toRecommendationMovieId(movieId);
  const detailsClient = useQueryClient();
  const feedbackClients = useMemo(
    () => ({ libraryClient: libraryClient ?? detailsClient, detailsClient }),
    [libraryClient, detailsClient],
  );
  const query = useRelatedMovies(id);
  const rows = useMemo(() => query.data ?? [], [query.data]);
  const sectionRef = useRef<HTMLElement>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const lastFocusedRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const lastFocused = lastFocusedRef.current;
    if (lastFocused && !document.contains(lastFocused)) {
      lastFocusedRef.current = null;
      headingRef.current?.focus();
    }
  }, [rows]);

  const trackFocus = (event: FocusEvent<HTMLElement>) => {
    if (event.target instanceof HTMLElement && event.target !== headingRef.current) {
      lastFocusedRef.current = event.target;
    }
  };

  const clearFocusOutside = (event: FocusEvent<HTMLElement>) => {
    const next = event.relatedTarget;
    if (next instanceof Node && !sectionRef.current?.contains(next)) {
      lastFocusedRef.current = null;
    }
  };

  if (id === null) {
    return null;
  }

  return (
    <section
      ref={sectionRef}
      className="details-section related-movies"
      onFocus={trackFocus}
      onBlur={clearFocusOutside}
    >
      <h2 ref={headingRef} tabIndex={-1} className="details-section-heading">
        {SECTION_LABEL}
      </h2>
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
            <MovieCard
              key={String(movie.id)}
              movie={movie}
              reasons={movie.reasons}
              feedbackClients={feedbackClients}
              onOpenMovie={openRecommendedMovie}
              className="related-movie-card"
            />
          ))}
        </MovieCarousel>
      )}
    </section>
  );
}
