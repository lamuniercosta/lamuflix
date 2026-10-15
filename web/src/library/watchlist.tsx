import { useId } from 'react';
import { useWatchlistToggle } from './watchlistStore';

function BookmarkIcon({ filled }: { filled: boolean }) {
  return (
    <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true" focusable="false">
      <path
        d="M6 3h12a1 1 0 0 1 1 1v17l-7-4-7 4V4a1 1 0 0 1 1-1z"
        fill={filled ? 'currentColor' : 'none'}
        stroke="currentColor"
        strokeWidth="2"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export interface WatchlistToggleButtonProps {
  movieId: number | string;
  isInWatchlist: boolean;
}

export function WatchlistToggleButton({ movieId, isInWatchlist }: WatchlistToggleButtonProps) {
  const {
    isInWatchlist: current,
    pending,
    error,
    toggle,
  } = useWatchlistToggle(movieId, isInWatchlist);
  const errorId = useId();

  return (
    <>
      <button
        type="button"
        aria-label={current ? 'Remove from watchlist' : 'Add to watchlist'}
        aria-pressed={current}
        aria-busy={pending}
        aria-describedby={error ? errorId : undefined}
        onClick={toggle}
      >
        <BookmarkIcon filled={current} />
      </button>
      {error ? (
        <p id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </>
  );
}
