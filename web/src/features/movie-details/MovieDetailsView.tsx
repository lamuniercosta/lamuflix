import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react';
import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query';
import type { components } from '../../api/types';
import { MovieFeedbackActions } from '../../library/MovieFeedbackActions';
import { useAmbientPosters } from '../../ui/ambient';
import { PlayFilledIcon, StarFilledIcon } from '../../ui/icons';
import { navigate, openMovie } from '../app-navigation/appNavigation';
import { readPlayerIntent, readReturnUrl } from '../app-navigation/useAppNavigation';
import BrowserPlayer from '../playback/BrowserPlayer';
import { OPEN_LOCAL_COPY, openLocalLabel, playLabel } from '../playback/playerCopy';
import { movieStreamUrl } from '../playback/streamSource';
import { usePlaybackCapabilities } from '../playback/usePlaybackCapabilities';
import { formatRating } from '../../library/movieCardFormat';
import {
  LAUNCH_UNAVAILABLE_TITLE,
  localLaunchOutcome,
  type LocalLaunchOutcome,
} from './movieDetailsLaunch';
import {
  formatRuntime,
  MovieDetailsError,
  useDeleteMovie,
  useMovieDetails,
  usePlayMovie,
  type MovieDetails,
  type MovieMetadata,
} from './movieDetailsApi';
import { matchPersonIds } from './movieDetailsLabels';
import {
  EnrichmentRequestError,
  useDetailsEnrichmentBridge,
  useLibraryEnrichmentStatus,
  usePeopleLookup,
  useRequestEnrichment,
  type EnrichmentStatus,
} from './movieDetailsQueries';
import CastSection from './CastSection';
import FileSection from './FileSection';
import ManualMatchControl from './ManualMatchControl';
import MoreWithPanel from './MoreWithPanel';
import MoviePoster from './MoviePoster';
import RelatedMovies from './RelatedMovies';
import TrailerSection from './TrailerSection';
import type { EnrichmentStatusSubscription } from '../../library/useEnrichmentStatus';
import './MovieDetailsView.css';

type PersonDto = components['schemas']['PersonDto'];
type ActorId = number | string;

export interface MovieDetailsViewProps {
  movieId: number | string;
  enrichmentStatus?: EnrichmentStatusSubscription;
}

function factsLine(metadata: MovieMetadata | null): string {
  if (!metadata) return '';
  const parts = [
    metadata.releaseYear != null ? String(metadata.releaseYear) : '',
    metadata.runtime != null ? formatRuntime(metadata.runtime) : '',
  ];
  return parts.filter(Boolean).join(' · ');
}

function enrichmentNote(status: EnrichmentStatus | null): string | null {
  if (status === 'Pending') return 'A metadata lookup is in progress.';
  if (status === 'NotFound') return 'The last lookup found no match for this title.';
  if (status === 'Failed') return 'The last metadata lookup failed.';
  return null;
}

function enrichmentRequestMessage(error: EnrichmentRequestError): string {
  if (error.status === 409) return 'A metadata lookup is already running for this movie.';
  if (error.status === 404) return 'This movie is no longer in the library.';
  return "Couldn't request metadata. Try again later.";
}

function NoMetadata({
  movieId,
  libraryClient,
  status,
}: {
  movieId: ActorId;
  libraryClient: QueryClient;
  status: EnrichmentStatus | null;
}) {
  const request = useRequestEnrichment(movieId, libraryClient);
  const note = enrichmentNote(status);
  const requestError = request.error;
  const canMatch = status === 'NotFound' || status === 'Failed';
  return (
    <div className="details-no-metadata">
      <p>No metadata available yet.</p>
      {note && <p className="details-status">{note}</p>}
      {status !== 'Pending' && (
        <button
          type="button"
          className="lamu-pill-button"
          disabled={request.isPending}
          onClick={() => request.mutate()}
        >
          {request.isPending ? 'Requesting...' : 'Fetch metadata'}
        </button>
      )}
      {canMatch && (
        <ManualMatchControl
          movieId={movieId}
          libraryClient={libraryClient}
          className="lamu-pill-button"
        />
      )}
      {request.isSuccess && status !== 'Pending' && (
        <p role="status" className="details-status">
          Metadata lookup requested. Use Match manually if it still finds nothing.
        </p>
      )}
      {requestError && (
        <p role={requestError.status === 409 ? 'status' : 'alert'} className="details-status">
          {enrichmentRequestMessage(requestError)}
        </p>
      )}
    </div>
  );
}

export default function MovieDetailsView({ movieId, enrichmentStatus }: MovieDetailsViewProps) {
  const libraryClient = useQueryClient();
  const [detailsClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
  return (
    <QueryClientProvider client={detailsClient}>
      <MovieDetailsViewInner
        key={String(movieId)}
        movieId={movieId}
        enrichmentStatus={enrichmentStatus}
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

/**
 * Orchestration shell: owns route/loading/refresh, the details client, local-play
 * request/result/in-flight state, the selected actor, and the removal dialog and
 * its focus. Cohesive presentation lives in the sibling modules.
 */
function MovieDetailsViewInner({
  movieId,
  enrichmentStatus,
  detailsClient,
  libraryClient,
}: MovieDetailsViewInnerProps) {
  const query = useMovieDetails(movieId);
  const movie: MovieDetails | undefined = query.data;
  const metadata = movie?.metadata ?? null;
  const posterUrl = metadata?.posterUrl ?? null;
  useAmbientPosters([posterUrl]);

  useDetailsEnrichmentBridge(enrichmentStatus, detailsClient, movieId);

  const libraryStatus = useLibraryEnrichmentStatus(libraryClient, movieId);
  // The browse cache is absent on direct navigation, so the details response is the fallback.
  const currentStatus = libraryStatus ?? movie?.enrichmentStatus ?? null;
  const hasMetadata = metadata !== null;
  const showMatchManually = hasMetadata && currentStatus !== 'Pending';
  useEffect(() => {
    if (libraryStatus === 'Enriched' && !hasMetadata) {
      void detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] });
    }
  }, [libraryStatus, hasMetadata, detailsClient, movieId]);

  const people = usePeopleLookup();
  const peopleList: readonly PersonDto[] = people.data ?? [];
  const deleteMutation = useDeleteMovie(movieId);
  const feedbackClients = useMemo(
    () => ({ libraryClient, detailsClient }),
    [libraryClient, detailsClient],
  );
  const [playerRequested, setPlayerRequested] = useState(() =>
    readPlayerIntent(window.history.state),
  );
  const capability = usePlaybackCapabilities();
  const localLaunch = usePlayMovie(movieId);
  const launchInFlightRef = useRef(false);
  const [launchOutcome, setLaunchOutcome] = useState<LocalLaunchOutcome | null>(null);
  const [removeOpen, setRemoveOpen] = useState(false);
  const [removeFailed, setRemoveFailed] = useState(false);
  const removeInFlightRef = useRef(false);
  const removeTriggerRef = useRef<HTMLButtonElement>(null);
  const removeConfirmRef = useRef<HTMLButtonElement>(null);
  const stripPromptId = useId();
  const [selectedActor, setSelectedActor] = useState<string | null>(null);

  // Navigation callbacks handed to the sibling modules; they never touch history.
  const openDetails = useCallback((id: ActorId) => {
    openMovie(id, { returnUrl: readReturnUrl(window.history.state) });
  }, []);

  const closeRemove = useCallback(() => {
    setRemoveOpen(false);
    setRemoveFailed(false);
    removeTriggerRef.current?.focus();
  }, []);

  useEffect(() => {
    if (removeOpen) {
      removeConfirmRef.current?.focus();
    }
  }, [removeOpen]);

  useEffect(() => {
    if (!removeOpen) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeRemove();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [removeOpen, closeRemove]);

  const openRemove = () => {
    setRemoveFailed(false);
    setRemoveOpen(true);
  };

  const confirmRemove = () => {
    if (removeInFlightRef.current) return;
    removeInFlightRef.current = true;
    setRemoveFailed(false);
    deleteMutation.mutate(undefined, {
      onSuccess: async () => {
        await detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] });
        await libraryClient.invalidateQueries({ queryKey: ['browseMovies'] });
        navigate('/');
      },
      onError: () => {
        setRemoveFailed(true);
      },
      onSettled: () => {
        removeInFlightRef.current = false;
      },
    });
  };

  const handlePlay = () => {
    setPlayerRequested(true);
  };

  const handleOpenLocal = useCallback(() => {
    if (launchInFlightRef.current) return;
    launchInFlightRef.current = true;
    setLaunchOutcome(null);
    localLaunch.mutate(undefined, {
      onSuccess: (status) => setLaunchOutcome(localLaunchOutcome(status)),
      onError: (error) =>
        setLaunchOutcome(
          localLaunchOutcome(error instanceof MovieDetailsError ? error.status : undefined),
        ),
      onSettled: () => {
        launchInFlightRef.current = false;
      },
    });
  }, [localLaunch]);

  const toggleActor = (name: string) => {
    setSelectedActor((current) => (current === name ? null : name));
  };

  if (query.isPending) {
    return <p role="status">Loading movie…</p>;
  }
  if (query.isError) {
    const status = (query.error as { status?: number }).status;
    if (status === 404) {
      return (
        <div>
          <p role="alert">Movie not found.</p>
          <a
            className="lamu-link"
            href="/"
            onClick={(event) => {
              event.preventDefault();
              navigate('/');
            }}
          >
            Back to library
          </a>
        </div>
      );
    }
    return <p role="alert">Couldn&apos;t load this movie. Try again later.</p>;
  }
  if (!movie) {
    return null;
  }

  const actors = metadata?.actors ?? [];
  const directors = metadata?.directors ?? [];
  const facts = factsLine(metadata);
  const hasRatings =
    metadata !== null &&
    [metadata.imdbRating, metadata.rottenTomatoesRating, metadata.metaScore].some(
      (value) => value != null,
    );
  const selectedIds = selectedActor ? matchPersonIds(selectedActor, peopleList) : [];

  return (
    <article className="details">
      <div className="details-hero">
        <MoviePoster title={movie.title} posterUrl={posterUrl} />
        <div className="details-summary">
          <h1 className="details-title">{movie.title}</h1>
          <p className="details-format">{movie.format}</p>
          {facts && <p className="details-facts">{facts}</p>}
          {metadata && hasRatings && (
            <ul className="details-ratings" aria-label="Ratings">
              {metadata.imdbRating != null && (
                <li className="details-rating">
                  <StarFilledIcon size={14} />
                  IMDb {formatRating(metadata.imdbRating)}/10
                </li>
              )}
              {metadata.rottenTomatoesRating != null && (
                <li className="details-rating">Rotten Tomatoes {metadata.rottenTomatoesRating}</li>
              )}
              {metadata.metaScore != null && (
                <li className="details-rating">Metacritic {metadata.metaScore}</li>
              )}
            </ul>
          )}
          {directors.length > 0 && (
            <p className="details-directors">
              {`${directors.length > 1 ? 'Directors' : 'Director'}: ${directors.join(', ')}`}
            </p>
          )}
          {metadata === null && (
            <NoMetadata movieId={movieId} libraryClient={libraryClient} status={currentStatus} />
          )}
          {actors.length > 0 && (
            <CastSection
              actors={actors}
              people={peopleList}
              selected={selectedActor}
              onSelect={toggleActor}
            />
          )}
          {metadata?.synopsis && <p className="details-synopsis">{metadata.synopsis}</p>}

          <div className="details-actions">
            <button
              type="button"
              className="lamu-action-primary"
              aria-label={playLabel(movie.title)}
              onClick={handlePlay}
            >
              <PlayFilledIcon size={18} />
              Play
            </button>
            {capability === 'enabled' && (
              <button
                type="button"
                className="lamu-action-quiet"
                aria-label={openLocalLabel(movie.title)}
                disabled={localLaunch.isPending}
                onClick={handleOpenLocal}
              >
                {OPEN_LOCAL_COPY}
              </button>
            )}
            <MovieFeedbackActions
              movieId={movie.id}
              title={movie.title}
              isWatched={movie.isWatched}
              review={movie.review}
              isInWatchlist={movie.isInWatchlist}
              clients={feedbackClients}
            />
            <button
              ref={removeTriggerRef}
              type="button"
              className="lamu-action-quiet"
              aria-expanded={removeOpen}
              disabled={deleteMutation.isPending}
              onClick={openRemove}
            >
              Remove from library
            </button>
          </div>

          {playerRequested && (
            <BrowserPlayer
              title={movie.title}
              movieId={movie.id}
              streamUrl={movieStreamUrl(movie.id)}
              localPlayEnabled={capability === 'enabled'}
              onOpenLocal={handleOpenLocal}
            />
          )}

          {launchOutcome?.kind === 'unavailable' && (
            <div role="alert" className="details-inline-alert">
              <p className="details-inline-alert-title">{LAUNCH_UNAVAILABLE_TITLE}</p>
              <p>{launchOutcome.message}</p>
            </div>
          )}
          {launchOutcome && launchOutcome.kind !== 'unavailable' && (
            <p
              role={launchOutcome.kind === 'started' ? 'status' : 'alert'}
              className="details-status"
            >
              {launchOutcome.message}
            </p>
          )}

          {removeOpen && (
            <div className="details-strip" role="group" aria-labelledby={stripPromptId}>
              <p id={stripPromptId}>The movie leaves the library. The file on disk stays.</p>
              <div className="details-strip-actions">
                <button
                  ref={removeConfirmRef}
                  type="button"
                  className="lamu-action-primary"
                  disabled={deleteMutation.isPending}
                  onClick={confirmRemove}
                >
                  Remove
                </button>
                <button type="button" className="lamu-action-quiet" onClick={closeRemove}>
                  Cancel
                </button>
              </div>
              {removeFailed && (
                <div className="details-strip-error">
                  <p role="alert">Could not remove this movie.</p>
                  <button type="button" className="lamu-action-quiet" onClick={confirmRemove}>
                    Try again
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {selectedActor && selectedIds.length > 0 && (
        <MoreWithPanel
          actor={selectedActor}
          actorIds={selectedIds}
          movieId={movieId}
          onOpenMovie={openDetails}
          onNavigate={navigate}
        />
      )}
      <TrailerSection title={movie.title} metadata={metadata} />
      <RelatedMovies movieId={movieId} libraryClient={libraryClient} />
      <FileSection
        path={movie.path}
        imdbId={metadata?.imdbId ?? null}
        movieId={movieId}
        libraryClient={libraryClient}
        showMatch={showMatchManually}
      />
    </article>
  );
}
