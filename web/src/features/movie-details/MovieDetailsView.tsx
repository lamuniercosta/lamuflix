import { useEffect, useRef, useState } from 'react';
import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query';
import {
  formatRating,
  formatRuntime,
  useDeleteMovie,
  useMovieDetails,
  usePlayMovie,
  type MovieDetails,
  type MovieMetadata,
} from './movieDetailsApi';
import { WatchlistToggleButton } from '../../library/watchlist';
import { browseMoviesQueryKey } from '../../library/libraryQueries';
import './MovieDetailsView.css';

export interface MovieDetailsViewProps {
  movieId: number | string;
}

function MetadataSection({ movie }: { movie: MovieDetails }) {
  const metadata: MovieMetadata | null = movie.metadata;
  const hasYearOrRuntime =
    Boolean(metadata) && (metadata!.releaseYear != null || metadata!.runtime != null);

  return (
    <div className="movie-details">
      <div className="movie-details-poster">
        {metadata?.posterUrl ? (
          <img src={metadata.posterUrl} alt={`${movie.title} poster`} />
        ) : (
          <div className="movie-details-placeholder" aria-hidden="true" />
        )}
      </div>
      <section>
        <h1>{movie.title}</h1>
        <WatchlistToggleButton movieId={movie.id} isInWatchlist={movie.isInWatchlist} />
        <p className="movie-details-format">{movie.format}</p>
        {hasYearOrRuntime && (
          <p className="movie-details-year-runtime">
            {metadata?.releaseYear != null && <span>{metadata.releaseYear}</span>}
            {metadata?.releaseYear != null && metadata?.runtime != null && <span> × </span>}
            {metadata?.runtime != null && <span>{formatRuntime(metadata.runtime)}</span>}
          </p>
        )}
        {metadata && (
          <div className="movie-details-badges">
            {metadata.imdbRating != null && (
              <span className="movie-details-badge">
                IMDb {formatRating(metadata.imdbRating)}/10
              </span>
            )}
            {metadata.rottenTomatoesRating != null && (
              <span className="movie-details-badge">
                Rotten Tomatoes {metadata.rottenTomatoesRating}
              </span>
            )}
            {metadata.metaScore != null && (
              <span className="movie-details-badge">Metacritic {metadata.metaScore}</span>
            )}
          </div>
        )}
        {metadata && metadata.directors.length > 0 && (
          <p className="movie-details-directors">
            {metadata.directors.length > 1 ? 'Directors' : 'Director'}:{' '}
            {metadata.directors.join(', ')}
          </p>
        )}
        {metadata && metadata.actors.length > 0 && (
          <p className="movie-details-cast">Cast: {metadata.actors.join(', ')}</p>
        )}
        {metadata?.synopsis && <p className="movie-details-synopsis">{metadata.synopsis}</p>}
        {metadata === null && (
          <p className="movie-details-no-metadata">No metadata available yet.</p>
        )}
      </section>
    </div>
  );
}

function navigateToLibrary(): void {
  window.history.pushState({}, '', '/');
  window.dispatchEvent(new PopStateEvent('popstate'));
}

export default function MovieDetailsView({ movieId }: MovieDetailsViewProps) {
  const libraryClient = useQueryClient();
  const [detailsClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
  return (
    <QueryClientProvider client={detailsClient}>
      <MovieDetailsViewInner
        movieId={movieId}
        detailsClient={detailsClient}
        libraryClient={libraryClient}
      />
    </QueryClientProvider>
  );
}

interface MovieDetailsViewInnerProps extends MovieDetailsViewProps {
  detailsClient: QueryClient;
  libraryClient: QueryClient;
}

function MovieDetailsViewInner({
  movieId,
  detailsClient,
  libraryClient,
}: MovieDetailsViewInnerProps) {
  const query = useMovieDetails(movieId);
  const playMutation = usePlayMovie(movieId);
  const [playState, setPlayState] = useState<'idle' | 'success' | 'notfound' | 'error'>('idle');
  const [dialogOpen, setDialogOpen] = useState(false);
  const playButtonRef = useRef<HTMLButtonElement>(null);
  const closeBtnRef = useRef<HTMLButtonElement>(null);
  const previousFocus = useRef<HTMLElement | null>(null);
  const playInFlightRef = useRef(false);
  const deleteMutation = useDeleteMovie(movieId);
  const [removeDialogOpen, setRemoveDialogOpen] = useState(false);
  const [removeState, setRemoveState] = useState<'idle' | 'error'>('idle');
  const removeInFlightRef = useRef(false);
  const removeButtonRef = useRef<HTMLButtonElement>(null);
  const removeConfirmRef = useRef<HTMLButtonElement>(null);
  const removePreviousFocus = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (removeDialogOpen && removeConfirmRef.current) {
      removePreviousFocus.current =
        removeButtonRef.current ?? (document.activeElement as HTMLElement | null);
      removeConfirmRef.current.focus();
    } else if (!removeDialogOpen && removePreviousFocus.current) {
      removePreviousFocus.current.focus();
      removePreviousFocus.current = null;
    }
  }, [removeDialogOpen]);

  useEffect(() => {
    if (!removeDialogOpen) return;
    const handler = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setRemoveDialogOpen(false);
        setRemoveState('idle');
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [removeDialogOpen]);

  useEffect(() => {
    if (dialogOpen && closeBtnRef.current) {
      previousFocus.current =
        playButtonRef.current ?? (document.activeElement as HTMLElement | null);
      closeBtnRef.current.focus();
    } else if (!dialogOpen && previousFocus.current) {
      previousFocus.current.focus();
      previousFocus.current = null;
    }
  }, [dialogOpen]);

  useEffect(() => {
    if (!dialogOpen) return;
    const handler = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setDialogOpen(false);
        return;
      }
      if (event.key === 'Tab') {
        event.preventDefault();
        closeBtnRef.current?.focus();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [dialogOpen]);

  const openRemoveDialog = () => {
    setRemoveState('idle');
    setRemoveDialogOpen(true);
  };

  const cancelRemove = () => {
    setRemoveDialogOpen(false);
    setRemoveState('idle');
  };

  const confirmRemove = () => {
    if (removeInFlightRef.current) return;
    removeInFlightRef.current = true;
    setRemoveState('idle');
    deleteMutation.mutate(undefined, {
      onSuccess: async () => {
        await detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] });
        await libraryClient.invalidateQueries({ queryKey: browseMoviesQueryKey });
        navigateToLibrary();
      },
      onError: () => {
        setRemoveState('error');
      },
      onSettled: () => {
        removeInFlightRef.current = false;
      },
    });
  };

  const handlePlay = () => {
    if (playInFlightRef.current) return;
    playInFlightRef.current = true;
    setPlayState('idle');
    playMutation.mutate(undefined, {
      onSuccess: () => {
        setPlayState('success');
      },
      onError: (error) => {
        const status = (error as { status?: number }).status;
        if (status === 403) {
          setDialogOpen(true);
        } else if (status === 404) {
          setPlayState('notfound');
        } else {
          setPlayState('error');
        }
      },
      onSettled: () => {
        playInFlightRef.current = false;
      },
    });
  };

  if (query.isPending) {
    return <p role="status">Loading movie…</p>;
  }
  if (query.isError) {
    const status = (query.error as { status?: number }).status;
    if (status === 404) {
      return <p role="alert">Movie not found.</p>;
    }
    return <p role="alert">Couldn&apos;t load this movie. Try again later.</p>;
  }

  return (
    <>
      <MetadataSection movie={query.data as MovieDetails} />
      <button
        ref={playButtonRef}
        type="button"
        disabled={playMutation.isPending}
        onClick={handlePlay}
      >
        {playMutation.isPending ? 'Starting...' : 'Play'}
      </button>
      {playState === 'success' && <span role="status">Playback started.</span>}
      {playState === 'notfound' && (
        <span role="alert">This movie is no longer in the library.</span>
      )}
      {playState === 'error' && (
        <span role="alert">Couldn&apos;t start playback. Try again later.</span>
      )}
      <button
        ref={removeButtonRef}
        type="button"
        disabled={deleteMutation.isPending}
        onClick={openRemoveDialog}
      >
        Remove from library
      </button>
      {removeDialogOpen && (
        <div role="dialog" aria-modal="true" aria-labelledby="remove-movie-title">
          <h2 id="remove-movie-title">Remove this movie from the library?</h2>
          <p>The movie leaves the library. The file on disk stays.</p>
          <button
            ref={removeConfirmRef}
            type="button"
            disabled={deleteMutation.isPending}
            onClick={confirmRemove}
          >
            Remove
          </button>
          <button type="button" onClick={cancelRemove}>
            Cancel
          </button>
          {removeState === 'error' && (
            <>
              <p role="alert">Could not remove this movie.</p>
              <button type="button" onClick={confirmRemove}>
                Try again
              </button>
            </>
          )}
        </div>
      )}
      {dialogOpen && (
        <div role="dialog" aria-modal="true" aria-labelledby="playback-unavailable-title">
          <h2 id="playback-unavailable-title">Playback unavailable</h2>
          <p>Local playback is disabled on this server.</p>
          <button ref={closeBtnRef} type="button" onClick={() => setDialogOpen(false)}>
            Close
          </button>
        </div>
      )}
    </>
  );
}
