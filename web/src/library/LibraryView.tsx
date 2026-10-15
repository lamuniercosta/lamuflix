import { useCallback, useEffect, useId, useRef, useState } from 'react';
import type { components } from '../api/types';
import {
  GENERIC_RETRY_ERROR,
  RetryRowError,
  useBrowseMoviesQuery,
  useRetryEnrichmentMutation,
} from './libraryQueries';
import { WatchlistToggleButton } from './watchlist';
import { useWatchlistToggle } from './watchlistStore';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type EnrichmentStatus = components['schemas']['EnrichmentStatus'];

function statusBadgeText(status: EnrichmentStatus): string {
  return status === 'NotFound' ? 'Not found' : status;
}

function canRetry(status: EnrichmentStatus): boolean {
  return status === 'NotFound' || status === 'Failed';
}

interface MovieRowProps {
  item: MovieSummary;
  filterActive: boolean;
  onRemoved: () => void;
}

function MovieRow({ item, filterActive, onRemoved }: MovieRowProps) {
  const errorId = useId();
  const [rowError, setRowError] = useState<string | null>(null);
  const retryEnrichment = useRetryEnrichmentMutation();
  const { isInWatchlist, pending } = useWatchlistToggle(item.id, item.isInWatchlist);
  const removed = filterActive && !pending && !isInWatchlist;
  const wasRemoved = useRef(removed);
  const badge = statusBadgeText(item.enrichmentStatus);

  useEffect(() => {
    if (removed && !wasRemoved.current) {
      onRemoved();
    }
    wasRemoved.current = removed;
  }, [removed, onRemoved]);

  async function onRetry() {
    setRowError(null);
    try {
      await retryEnrichment.mutateAsync(item.id);
    } catch (error) {
      setRowError(error instanceof RetryRowError ? error.message : GENERIC_RETRY_ERROR);
    }
  }

  if (removed) {
    return null;
  }

  return (
    <li>
      <span>{item.title}</span> <span aria-label={badge}>{badge}</span>
      {canRetry(item.enrichmentStatus) ? (
        <button
          type="button"
          aria-label={`Retry ${item.title}`}
          aria-describedby={rowError ? errorId : undefined}
          onClick={() => void onRetry()}
        >
          Retry
        </button>
      ) : null}
      <WatchlistToggleButton movieId={item.id} isInWatchlist={item.isInWatchlist} />
      {rowError ? (
        <p id={errorId} role="alert">
          {rowError}
        </p>
      ) : null}
    </li>
  );
}

export default function LibraryView({
  leadingItem,
  inWatchlist = false,
}: {
  leadingItem?: MovieSummary | null;
  inWatchlist?: boolean;
}) {
  const query = useBrowseMoviesQuery(leadingItem != null, inWatchlist);
  const fetched = query.data?.items ?? [];
  const items =
    leadingItem && !fetched.some((item) => String(item.id) === String(leadingItem.id))
      ? [leadingItem, ...fetched]
      : fetched;
  const listRef = useRef<HTMLUListElement>(null);
  const focusList = useCallback(() => {
    listRef.current?.focus();
  }, []);

  if (!query.data && !leadingItem) {
    return null;
  }

  if (items.length === 0) {
    return inWatchlist ? (
      <p>Your watchlist is empty.</p>
    ) : (
      <p>No movies yet. Import a folder to get started.</p>
    );
  }

  return (
    <ul ref={listRef} tabIndex={-1} aria-live="polite">
      {items.map((item) => (
        <MovieRow
          key={String(item.id)}
          item={item}
          filterActive={inWatchlist}
          onRemoved={focusList}
        />
      ))}
    </ul>
  );
}
