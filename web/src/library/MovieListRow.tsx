import { useState } from 'react';
import { FilmIcon, RetryIcon, StarFilledIcon } from '../ui/icons';
import { MovieFeedbackActions } from './MovieFeedbackActions';
import type { MovieCardProps } from './MovieCard';
import { canRetryEnrichment, displayValue, formatRating } from './movieCardFormat';

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pending',
  NotFound: 'Not found',
  Failed: 'Failed',
};

function runtimeText(value: null | number | string | undefined): string {
  return value === null || value === undefined || value === '' ? '—' : `${String(value)} min`;
}

export default function MovieListRow({
  movie,
  onOpenMovie,
  onRetry,
  retryError,
  retryErrorId,
  feedbackClients,
}: MovieCardProps) {
  const [posterFailed, setPosterFailed] = useState(false);

  const { id, title, enrichmentStatus: status } = movie;
  const showPoster = Boolean(movie.posterUrl) && !posterFailed;
  const ratingText = formatRating(movie.imdbRating);
  const statusLabel = STATUS_LABELS[status];

  return (
    <div className="movie-row" data-status={status}>
      <div className="movie-row-poster">
        {showPoster ? (
          <img
            src={movie.posterUrl ?? undefined}
            alt={title}
            loading="lazy"
            onError={() => setPosterFailed(true)}
          />
        ) : (
          <div className="movie-card-placeholder" role="img" aria-label={title}>
            <FilmIcon size={18} />
          </div>
        )}
      </div>

      <h3 className="movie-row-title">
        <a
          href={`/movies/${id}`}
          onClick={(event) => {
            if (onOpenMovie) {
              event.preventDefault();
              onOpenMovie(id);
            }
          }}
        >
          {title}
        </a>
      </h3>

      <span className="movie-row-cell movie-row-year">{displayValue(movie.releaseYear)}</span>

      <span className="movie-row-cell movie-row-runtime">{runtimeText(movie.runtime)}</span>

      <span className="movie-row-cell movie-row-rating">
        <span className="sr-only">IMDb rating </span>
        <StarFilledIcon size={14} className="movie-card-star" />
        <span>{ratingText ?? '—'}</span>
      </span>

      <div className="movie-row-status">
        {statusLabel ? (
          <span className={`movie-status movie-status-${status.toLowerCase()}`}>{statusLabel}</span>
        ) : null}
        {canRetryEnrichment(status) ? (
          <button
            type="button"
            className="movie-card-retry"
            aria-label={`Retry ${title}`}
            aria-describedby={retryError ? retryErrorId : undefined}
            onClick={() => onRetry?.(id)}
          >
            <RetryIcon size={14} />
            <span>Retry</span>
          </button>
        ) : null}
        {retryError ? (
          <p id={retryErrorId} role="alert" className="movie-row-error">
            {retryError}
          </p>
        ) : null}
      </div>

      <div className="movie-row-watch">
        <MovieFeedbackActions
          movieId={id}
          title={title}
          isWatched={movie.isWatched}
          review={movie.review}
          isInWatchlist={movie.isInWatchlist}
          clients={feedbackClients}
        />
      </div>
    </div>
  );
}
