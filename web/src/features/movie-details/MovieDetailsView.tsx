import { useEffect, useRef, useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  formatRating,
  formatRuntime,
  useMovieDetails,
  usePlayMovie,
  type MovieDetails,
  type MovieMetadata,
} from './movieDetailsApi';
import { WatchlistToggleButton } from '../../library/watchlist';
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

export default function MovieDetailsView({ movieId }: MovieDetailsViewProps) {
  const [queryClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
  return (
    <QueryClientProvider client={queryClient}>
      <MovieDetailsViewInner movieId={movieId} />
    </QueryClientProvider>
  );
}

function MovieDetailsViewInner({ movieId }: MovieDetailsViewProps) {
  const query = useMovieDetails(movieId);
  const playMutation = usePlayMovie(movieId);
  const [playState, setPlayState] = useState<'idle' | 'success' | 'notfound' | 'error'>('idle');
  const [dialogOpen, setDialogOpen] = useState(false);
  const playButtonRef = useRef<HTMLButtonElement>(null);
  const closeBtnRef = useRef<HTMLButtonElement>(null);
  const previousFocus = useRef<HTMLElement | null>(null);
  const playInFlightRef = useRef(false);

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
