import { useCallback, useEffect, useId, useRef, useState, type MouseEvent } from 'react';
import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query';
import type { components } from '../../api/types';
import { WatchlistToggleButton } from '../../library/watchlist';
import { useAmbientPosters } from '../../ui/ambient';
import {
  ArrowRightIcon,
  CopyIcon,
  ExternalLinkIcon,
  FilmIcon,
  LoaderIcon,
  PlayFilledIcon,
  StarFilledIcon,
} from '../../ui/icons';
import { readReturnUrl } from '../app-navigation/useAppNavigation';
import { formatRating } from '../../library/movieCardFormat';
import {
  formatRuntime,
  useDeleteMovie,
  useMovieDetails,
  usePlayMovie,
  type MovieDetails,
  type MovieMetadata,
} from './movieDetailsApi';
import { libraryHrefForActors, matchPersonIds } from './movieDetailsLabels';
import {
  EnrichmentRequestError,
  useLibraryEnrichmentStatus,
  useMoreWithActors,
  usePeopleLookup,
  useRequestEnrichment,
  type EnrichmentStatus,
} from './movieDetailsQueries';
import './MovieDetailsView.css';

type PersonDto = components['schemas']['PersonDto'];
type MovieSummary = components['schemas']['MovieSummaryResponse'];
type ActorId = number | string;
type PlayState = 'idle' | 'success' | 'notfound' | 'unavailable' | 'error';
type CopyState = 'idle' | 'copied' | 'failed';

export interface MovieDetailsViewProps {
  movieId: number | string;
}

function navigateTo(href: string, state: unknown = {}): void {
  window.history.pushState(state, '', href);
  window.dispatchEvent(new PopStateEvent('popstate'));
}

function openDetails(id: ActorId): void {
  const returnUrl = readReturnUrl(window.history.state);
  navigateTo(`/movies/${String(id)}`, returnUrl ? { libraryReturnUrl: returnUrl } : {});
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

function Poster({ title, posterUrl }: { title: string; posterUrl: string | null }) {
  if (posterUrl) {
    return <img className="details-poster" src={posterUrl} alt={`${title} poster`} />;
  }
  return (
    <div className="details-poster details-poster-placeholder lamu-glass" aria-hidden="true">
      <FilmIcon size={48} />
    </div>
  );
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
      {requestError && (
        <p role={requestError.status === 409 ? 'status' : 'alert'} className="details-status">
          {enrichmentRequestMessage(requestError)}
        </p>
      )}
    </div>
  );
}

function CastSection({
  actors,
  people,
  selected,
  onSelect,
}: {
  actors: string[];
  people: readonly PersonDto[];
  selected: string | null;
  onSelect: (name: string) => void;
}) {
  const headingId = useId();
  return (
    <section className="details-section" aria-labelledby={headingId}>
      <h2 id={headingId} className="details-section-heading">
        Cast
      </h2>
      <ul className="details-cast-list">
        {actors.map((name, index) => {
          const key = `${index}-${name}`;
          if (matchPersonIds(name, people).length === 0) {
            return (
              <li key={key} className="details-cast-plain">
                {name}
              </li>
            );
          }
          return (
            <li key={key}>
              <button
                type="button"
                className="lamu-chip"
                aria-pressed={selected === name}
                onClick={() => onSelect(name)}
              >
                {name}
              </button>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function MorePosterCard({ item }: { item: MovieSummary }) {
  const [posterFailed, setPosterFailed] = useState(false);
  const href = `/movies/${String(item.id)}`;
  const open = (event: MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    openDetails(item.id);
  };
  const rating = item.imdbRating != null ? formatRating(item.imdbRating) : null;
  return (
    <li className="details-more-card">
      <a
        className="details-more-poster"
        href={href}
        tabIndex={-1}
        aria-hidden="true"
        onClick={open}
      >
        {item.posterUrl && !posterFailed ? (
          <img src={item.posterUrl} alt="" loading="lazy" onError={() => setPosterFailed(true)} />
        ) : (
          <span className="details-more-placeholder lamu-glass">
            <FilmIcon size={28} />
          </span>
        )}
      </a>
      <a className="details-more-title" href={href} onClick={open}>
        {item.title}
      </a>
      <p className="details-more-meta">
        {item.enrichmentStatus === 'Enriched' ? (
          <>
            {rating ? (
              <>
                <StarFilledIcon size={12} className="details-more-star" />
                <span className="sr-only">IMDb </span>
                {rating}
              </>
            ) : (
              '—'
            )}
            <span aria-hidden="true"> | </span>
            {item.releaseYear ?? '—'}
          </>
        ) : (
          'No metadata yet'
        )}
      </p>
    </li>
  );
}

function MoreWithPanel({
  actor,
  actorIds,
  movieId,
}: {
  actor: string;
  actorIds: ActorId[];
  movieId: ActorId;
}) {
  const headingId = useId();
  const more = useMoreWithActors(actorIds, actorIds.length > 0);
  const libraryHref = libraryHrefForActors(actorIds);
  const rows = (more.data ?? []).filter((item) => String(item.id) !== String(movieId));
  return (
    <section className="details-section details-more" aria-labelledby={headingId}>
      <div className="details-section-head">
        <h2 id={headingId} className="details-section-heading">
          {`More with ${actor} in your library`}
        </h2>
        <a
          className="lamu-link"
          href={libraryHref}
          onClick={(event) => {
            event.preventDefault();
            navigateTo(libraryHref);
          }}
        >
          {`See all with ${actor} in Library`}
          <ArrowRightIcon size={14} />
        </a>
      </div>
      {more.isPending && (
        <p role="status" className="details-status">
          Loading movies…
        </p>
      )}
      {more.isError && (
        <p role="alert" className="details-status">
          Couldn&apos;t load more movies. Try again later.
        </p>
      )}
      {more.isSuccess && rows.length === 0 && (
        <p className="details-status">{`No other movies with ${actor} in your library yet.`}</p>
      )}
      {rows.length > 0 && (
        <ul className="details-more-row">
          {rows.map((item) => (
            <MorePosterCard key={String(item.id)} item={item} />
          ))}
        </ul>
      )}
    </section>
  );
}

function FileSection({ path, imdbId }: { path: string; imdbId: string | null }) {
  const headingId = useId();
  const [copyState, setCopyState] = useState<CopyState>('idle');
  const copyPath = async () => {
    try {
      await navigator.clipboard.writeText(path);
      setCopyState('copied');
    } catch {
      setCopyState('failed');
    }
  };
  return (
    <section className="details-section details-file" aria-labelledby={headingId}>
      <h2 id={headingId} className="details-section-heading">
        File
      </h2>
      <p className="details-path">{path}</p>
      <div className="details-file-actions">
        <button type="button" className="lamu-link" onClick={() => void copyPath()}>
          <CopyIcon size={16} />
          Copy path
        </button>
        {copyState === 'copied' && <span role="status">Copied</span>}
        {copyState === 'failed' && <span role="alert">Couldn&apos;t copy the path.</span>}
        {imdbId && (
          <a
            className="lamu-link"
            href={`https://www.imdb.com/title/${imdbId}/`}
            target="_blank"
            rel="noopener"
          >
            View on IMDb
            <ExternalLinkIcon size={16} />
          </a>
        )}
      </div>
    </section>
  );
}

export default function MovieDetailsView({ movieId }: MovieDetailsViewProps) {
  const libraryClient = useQueryClient();
  const [detailsClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  );
  return (
    <QueryClientProvider client={detailsClient}>
      <MovieDetailsViewInner
        key={String(movieId)}
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
  const movie: MovieDetails | undefined = query.data;
  const metadata = movie?.metadata ?? null;
  const posterUrl = metadata?.posterUrl ?? null;
  useAmbientPosters([posterUrl]);

  const libraryStatus = useLibraryEnrichmentStatus(libraryClient, movieId);
  const hasMetadata = metadata !== null;
  useEffect(() => {
    if (libraryStatus === 'Enriched' && !hasMetadata) {
      void detailsClient.invalidateQueries({ queryKey: ['movieDetails', movieId] });
    }
  }, [libraryStatus, hasMetadata, detailsClient, movieId]);

  const people = usePeopleLookup();
  const peopleList: readonly PersonDto[] = people.data ?? [];
  const playMutation = usePlayMovie(movieId);
  const deleteMutation = useDeleteMovie(movieId);
  const [playState, setPlayState] = useState<PlayState>('idle');
  const playInFlightRef = useRef(false);
  const [removeOpen, setRemoveOpen] = useState(false);
  const [removeFailed, setRemoveFailed] = useState(false);
  const removeInFlightRef = useRef(false);
  const removeTriggerRef = useRef<HTMLButtonElement>(null);
  const removeConfirmRef = useRef<HTMLButtonElement>(null);
  const stripPromptId = useId();
  const [selectedActor, setSelectedActor] = useState<string | null>(null);

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
        navigateTo('/');
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
    if (playInFlightRef.current) return;
    playInFlightRef.current = true;
    setPlayState('idle');
    playMutation.mutate(undefined, {
      onSuccess: () => {
        setPlayState('success');
      },
      onError: (error) => {
        if (error.status === 403) {
          setPlayState('unavailable');
        } else if (error.status === 404) {
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

  const toggleActor = (name: string) => {
    setSelectedActor((current) => (current === name ? null : name));
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
        <Poster title={movie.title} posterUrl={posterUrl} />
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
            <NoMetadata movieId={movieId} libraryClient={libraryClient} status={libraryStatus} />
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
              disabled={playMutation.isPending}
              onClick={handlePlay}
            >
              {playMutation.isPending ? <LoaderIcon size={18} /> : <PlayFilledIcon size={18} />}
              {playMutation.isPending ? 'Starting...' : 'Play'}
            </button>
            <div className="details-watchlist">
              <WatchlistToggleButton movieId={movie.id} isInWatchlist={movie.isInWatchlist} />
            </div>
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

          {playState === 'success' && (
            <p role="status" className="details-status">
              Playback started.
            </p>
          )}
          {playState === 'notfound' && (
            <p role="alert" className="details-status">
              This movie is no longer in the library.
            </p>
          )}
          {playState === 'error' && (
            <p role="alert" className="details-status">
              Couldn&apos;t start playback. Try again later.
            </p>
          )}
          {playState === 'unavailable' && (
            <div role="alert" className="details-inline-alert">
              <p className="details-inline-alert-title">Playback unavailable</p>
              <p>Local playback is disabled on this server.</p>
            </div>
          )}

          {removeOpen && (
            <div className="details-strip" role="group" aria-labelledby={stripPromptId}>
              <p id={stripPromptId}>The movie leaves the library. The file on disk stays.</p>
              <div className="details-strip-actions">
                <button
                  ref={removeConfirmRef}
                  type="button"
                  className="lamu-action-quiet"
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
        <MoreWithPanel actor={selectedActor} actorIds={selectedIds} movieId={movieId} />
      )}
      <FileSection path={movie.path} imdbId={metadata?.imdbId ?? null} />
    </article>
  );
}
