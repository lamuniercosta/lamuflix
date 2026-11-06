import { useId, useMemo, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { EyeIcon, ThumbsDownIcon, ThumbsUpIcon } from '../ui/icons';
import { normalizeReview, useMovieFeedback, type MovieFeedbackClients } from './movieFeedbackStore';
import type { FeedbackMovieId } from './movieFeedback';
import { WatchlistToggleButton } from './watchlist';
import './MovieFeedbackActions.css';

export interface MovieFeedbackActionsProps {
  movieId: FeedbackMovieId;
  title: string;
  isWatched: boolean;
  review: string;
  isInWatchlist: boolean;
  clients?: MovieFeedbackClients;
}

interface IconActionProps {
  label: string;
  pressed: boolean;
  pending: boolean;
  disabled: boolean;
  onActivate: () => void;
  children: ReactNode;
}

function IconAction({ label, pressed, pending, disabled, onActivate, children }: IconActionProps) {
  return (
    <span className="movie-feedback-action">
      <button
        type="button"
        className="movie-feedback-icon"
        aria-label={label}
        aria-pressed={pressed}
        aria-busy={pending ? true : undefined}
        disabled={disabled}
        onClick={onActivate}
      >
        {children}
      </button>
      <span className="movie-feedback-tooltip" aria-hidden="true">
        {label}
      </span>
    </span>
  );
}

export function MovieFeedbackActions({
  movieId,
  title,
  isWatched,
  review,
  isInWatchlist,
  clients,
}: MovieFeedbackActionsProps) {
  const queryClient = useQueryClient();
  const resolvedClients = useMemo<MovieFeedbackClients>(
    () => clients ?? { libraryClient: queryClient },
    [clients, queryClient],
  );
  const {
    isWatched: watched,
    review: currentReview,
    pending,
    blocked,
    error,
    refreshError,
    markWatched,
    unmarkWatched,
    like,
    dislike,
    clearReview,
    retry,
    retryRefresh,
  } = useMovieFeedback(movieId, { isWatched, review: normalizeReview(review) }, resolvedClients);
  const errorId = useId();
  const refreshErrorId = useId();
  const liked = currentReview === 'liked';
  const disliked = currentReview === 'disliked';

  return (
    <div className="movie-feedback-actions">
      <IconAction
        label={watched ? `Unmark Watched, ${title}` : `Mark Watched, ${title}`}
        pressed={watched}
        pending={pending}
        disabled={blocked}
        onActivate={watched ? unmarkWatched : markWatched}
      >
        <EyeIcon size={18} />
      </IconAction>
      <IconAction
        label={liked ? `Clear liked review, ${title}` : `Like, ${title}`}
        pressed={liked}
        pending={pending}
        disabled={blocked}
        onActivate={liked ? clearReview : like}
      >
        <ThumbsUpIcon size={18} />
      </IconAction>
      <IconAction
        label={disliked ? `Clear disliked review, ${title}` : `Dislike, ${title}`}
        pressed={disliked}
        pending={pending}
        disabled={blocked}
        onActivate={disliked ? clearReview : dislike}
      >
        <ThumbsDownIcon size={18} />
      </IconAction>
      <WatchlistToggleButton
        movieId={movieId}
        isInWatchlist={isInWatchlist}
        title={title}
        className="movie-feedback-icon"
        disabled={blocked}
      />
      <span className="sr-only" role="status">
        {pending ? `Saving feedback for ${title}` : ''}
      </span>
      {error ? (
        <div className="movie-feedback-error">
          <p id={errorId} role="alert">
            {error}
          </p>
          <button
            type="button"
            className="lamu-action-quiet"
            aria-label={`Retry saving feedback, ${title}`}
            aria-describedby={errorId}
            disabled={pending}
            onClick={retry}
          >
            Retry
          </button>
        </div>
      ) : null}
      {refreshError ? (
        <div className="movie-feedback-error">
          <p id={refreshErrorId} role="alert">
            {refreshError}
          </p>
          <button
            type="button"
            className="lamu-action-quiet"
            aria-label={`Retry refresh, ${title}`}
            aria-describedby={refreshErrorId}
            onClick={retryRefresh}
          >
            Retry
          </button>
        </div>
      ) : null}
    </div>
  );
}
