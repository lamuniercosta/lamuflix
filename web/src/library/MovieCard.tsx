import { useState, type MouseEvent } from 'react';
import type { components } from '../api/types';
import { FilmIcon, PlayFilledIcon, RetryIcon, StarFilledIcon } from '../ui/icons';
import { openMoviePlayer } from '../features/app-navigation/useAppNavigation';
import { canRetryEnrichment, displayValue, formatRating } from './movieCardFormat';
import { MovieFeedbackActions } from './MovieFeedbackActions';
import type { MovieFeedbackClients } from './movieFeedbackStore';

export type MovieSummaryResponse = components['schemas']['MovieSummaryResponse'];

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pending',
  NotFound: 'Not found',
  Failed: 'Failed',
};

export interface MovieCardReason {
  kind: string;
  text: string;
}

export interface OpenMovieOptions {
  playerIntent?: boolean;
}

export interface MovieCardProps {
  movie: MovieSummaryResponse;
  onOpenMovie?: (id: number | string, options?: OpenMovieOptions) => void;
  onRetry?: (id: number | string) => void;
  retryError?: string | null;
  retryErrorId?: string;
  reasons?: ReadonlyArray<string | MovieCardReason>;
  feedbackClients?: MovieFeedbackClients;
  className?: string;
}

export default function MovieCard({
  movie,
  onOpenMovie,
  onRetry,
  retryError,
  retryErrorId,
  reasons,
  feedbackClients,
  className,
}: MovieCardProps) {
  const [posterFailed, setPosterFailed] = useState(false);
  const [thumbnailFailed, setThumbnailFailed] = useState(false);

  const { id, title, enrichmentStatus: status } = movie;
  const showPoster = Boolean(movie.posterUrl) && !posterFailed;
  const showThumbnail = !showPoster && !thumbnailFailed && status === 'Enriched';
  const thumbnailUrl = `/api/movies/${id}/thumbnail`;
  const ratingText = formatRating(movie.imdbRating);
  const statusLabel = STATUS_LABELS[status];

  const handlePlay = () => {
    if (onOpenMovie) {
      onOpenMovie(id, { playerIntent: true });
      return;
    }
    openMoviePlayer(id);
  };

  const handleOpen = (event: MouseEvent<HTMLAnchorElement>) => {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }
    if (onOpenMovie) {
      event.preventDefault();
      onOpenMovie(id);
    }
  };

  return (
    <article className={`movie-card${className ? ` ${className}` : ''}`} data-status={status}>
      <div className="movie-card-poster" style={{ aspectRatio: '2 / 3' }}>
        {showPoster ? (
          <img
            src={movie.posterUrl ?? undefined}
            alt={title}
            loading="lazy"
            onError={() => setPosterFailed(true)}
          />
        ) : showThumbnail ? (
          <img
            src={thumbnailUrl}
            alt={title}
            loading="lazy"
            onError={() => setThumbnailFailed(true)}
          />
        ) : (
          <div className="movie-card-placeholder" role="img" aria-label={title}>
            <FilmIcon size={32} />
          </div>
        )}

        {statusLabel ? (
          <span className={`movie-status movie-status-${status.toLowerCase()}`}>{statusLabel}</span>
        ) : null}

        <button
          type="button"
          className="movie-card-play"
          aria-label={`Play ${title}`}
          onClick={handlePlay}
        >
          <PlayFilledIcon size={26} />
        </button>

        <div className="movie-card-actions">
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

      <h3 className="movie-card-title">
        <a href={`/movies/${id}`} onClick={handleOpen}>
          {title}
        </a>
      </h3>

      <p className="movie-card-meta">
        <span
          className="movie-card-rating"
          aria-label={ratingText ? `IMDb rating ${ratingText}` : 'IMDb rating unavailable'}
        >
          <StarFilledIcon size={14} className="movie-card-star" />
          <span>{ratingText ?? '—'}</span>
        </span>
        <span className="movie-card-sep" aria-hidden="true" />
        <span>{displayValue(movie.releaseYear)}</span>
      </p>

      {reasons?.length ? (
        <div className="related-movie-reasons">
          {reasons.map((reason, index) => {
            const text = typeof reason === 'string' ? reason : reason.text;
            const kind = typeof reason === 'string' ? null : reason.kind;
            return (
              <span
                className={
                  kind === null
                    ? 'related-movie-reason'
                    : `related-movie-reason related-movie-reason-${kind}`
                }
                key={`${kind ?? 'text'}-${text}-${String(index)}`}
              >
                {text}
              </span>
            );
          })}
        </div>
      ) : null}

      {canRetryEnrichment(status) ? (
        <div className="movie-card-status">
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
        </div>
      ) : null}

      {retryError ? (
        <p id={retryErrorId} role="alert" className="movie-card-error">
          {retryError}
        </p>
      ) : null}
    </article>
  );
}
