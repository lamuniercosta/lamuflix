import { useId } from 'react';
import { BookmarkFilledIcon, BookmarkIcon } from '../ui/icons';
import { useWatchlistToggle } from './watchlistStore';

export interface WatchlistToggleButtonProps {
  movieId: number | string;
  isInWatchlist: boolean;
  title?: string;
  className?: string;
  disabled?: boolean;
}

export function WatchlistToggleButton({
  movieId,
  isInWatchlist,
  title,
  className,
  disabled = false,
}: WatchlistToggleButtonProps) {
  const {
    isInWatchlist: current,
    pending,
    error,
    toggle,
  } = useWatchlistToggle(movieId, isInWatchlist);
  const errorId = useId();
  const label = title
    ? `${current ? 'Remove from Watchlist' : 'Add to Watchlist'}, ${title}`
    : current
      ? 'Remove from watchlist'
      : 'Add to watchlist';

  const button = (
    <button
      type="button"
      className={className}
      aria-label={label}
      aria-pressed={current}
      aria-busy={pending}
      aria-describedby={error ? errorId : undefined}
      disabled={disabled}
      onClick={toggle}
    >
      {current ? <BookmarkFilledIcon size={16} /> : <BookmarkIcon size={16} />}
    </button>
  );

  if (!title) {
    return (
      <>
        {button}
        {error ? (
          <p id={errorId} role="alert">
            {error}
          </p>
        ) : null}
      </>
    );
  }

  return (
    <>
      <span className="movie-feedback-action">
        {button}
        <span className="movie-feedback-tooltip" aria-hidden="true">
          {label}
        </span>
      </span>
      {error ? (
        <p id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </>
  );
}
