import { useId, useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { FeedbackMovieId } from './movieFeedback';
import { useMovieFeedback, type MovieFeedbackClients } from './movieFeedbackStore';
import './MovieFeedbackActions.css';

export interface MovieFeedbackActionsProps {
  movieId: FeedbackMovieId;
  title: string;
  isWatched: boolean;
  isNotInterested: boolean;
  clients?: MovieFeedbackClients;
}

export function MovieFeedbackActions({
  movieId,
  title,
  isWatched,
  isNotInterested,
  clients,
}: MovieFeedbackActionsProps) {
  const queryClient = useQueryClient();
  const resolvedClients = useMemo<MovieFeedbackClients>(
    () => clients ?? { libraryClient: queryClient },
    [clients, queryClient],
  );
  const {
    isWatched: watched,
    isNotInterested: notInterested,
    pending,
    blocked,
    error,
    refreshError,
    markWatched,
    markNotInterested,
    retry,
    retryRefresh,
  } = useMovieFeedback(movieId, { isWatched, isNotInterested }, resolvedClients);
  const errorId = useId();
  const refreshErrorId = useId();

  return (
    <div className="movie-feedback-actions">
      <button
        type="button"
        className="lamu-action-quiet"
        data-marked={watched ? 'true' : undefined}
        aria-label={`${watched ? 'Watched' : 'Mark Watched'}, ${title}`}
        aria-busy={pending ? true : undefined}
        disabled={blocked || watched}
        onClick={markWatched}
      >
        {watched ? 'Watched' : 'Mark Watched'}
      </button>
      <button
        type="button"
        className="lamu-action-quiet"
        data-marked={notInterested ? 'true' : undefined}
        aria-label={`${notInterested ? 'Not Interested (marked)' : 'Not Interested'}, ${title}`}
        aria-busy={pending ? true : undefined}
        disabled={blocked || notInterested}
        onClick={markNotInterested}
      >
        {notInterested ? 'Not Interested (marked)' : 'Not Interested'}
      </button>
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
