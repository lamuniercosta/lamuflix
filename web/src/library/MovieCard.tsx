import { useId, useState, type MouseEvent } from 'react';
import type { components } from '../api/types';
import { FilmIcon, PlayFilledIcon, RetryIcon, StarFilledIcon } from '../ui/icons';
import { usePlayMovieMutation } from './libraryQueries';
import {
  canRetryEnrichment,
  displayValue,
  formatRating,
  playErrorMessage,
} from './movieCardFormat';
import { WatchlistToggleButton } from './watchlist';
import { useWatchlistToggle } from './watchlistStore';

export type MovieSummaryResponse = components['schemas']['MovieSummaryResponse'];

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pending',
  NotFound: 'Not found',
  Failed: 'Failed',
};

export interface MovieCardProps {
  movie: MovieSummaryResponse;
  onOpenMovie?: (id: number | string) => void;
  onRetry?: (id: number | string) => void;
  retryError?: string | null;
  retryErrorId?: string;
}

export default function MovieCard({
  movie,
  onOpenMovie,
  onRetry,
  retryError,
  retryErrorId,
}: MovieCardProps) {
  const [posterFailed, setPosterFailed] = useState(false);
  const [playError, setPlayError] = useState<string | null>(null);
  const playErrorId = useId();
  const playMutation = usePlayMovieMutation();
  const watchlist = useWatchlistToggle(movie.id, movie.isInWatchlist);

  const { id, title, enrichmentStatus: status } = movie;
  const showPoster = Boolean(movie.posterUrl) && !posterFailed;
  const ratingText = formatRating(movie.imdbRating);
  const statusLabel = STATUS_LABELS[status];

  const handlePlay = () => {
    setPlayError(null);
    playMutation.mutate(id, {
      onError: (error) => setPlayError(playErrorMessage(error)),
    });
  };

  const handleOpen = (event: MouseEvent<HTMLAnchorElement>) => {
    if (onOpenMovie) {
      event.preventDefault();
      onOpenMovie(id);
    }
  };

  return (
    <article className="movie-card" data-status={status}>
      <div className="movie-card-poster" style={{ aspectRatio: '2 / 3' }}>
        {showPoster ? (
          <img
            src={movie.posterUrl ?? undefined}
            alt={title}
            loading="lazy"
            onError={() => setPosterFailed(true)}
          />
        ) : (
          <div className="movie-card-placeholder" role="img" aria-label={title}>
            <FilmIcon size={32} />
          </div>
        )}

        {statusLabel ? (
          <span className={`movie-status movie-status-${status.toLowerCase()}`}>{statusLabel}</span>
        ) : null}

        <div
          className="movie-card-watch"
          data-watchlisted={String(watchlist.isInWatchlist)}
          data-error={watchlist.error ? 'true' : 'false'}
        >
          <WatchlistToggleButton movieId={id} isInWatchlist={movie.isInWatchlist} />
        </div>

        <button
          type="button"
          className="movie-card-play"
          aria-label={`Play ${title}`}
          aria-busy={playMutation.isPending}
          aria-describedby={playError ? playErrorId : undefined}
          disabled={playMutation.isPending}
          onClick={handlePlay}
        >
          <PlayFilledIcon size={22} />
        </button>
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
        <span className="movie-card-sep" aria-hidden="true">
          |
        </span>
        <span>{displayValue(movie.releaseYear)}</span>
      </p>

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

      {playError ? (
        <p id={playErrorId} role="alert" className="movie-card-error">
          {playError}
        </p>
      ) : null}
    </article>
  );
}
