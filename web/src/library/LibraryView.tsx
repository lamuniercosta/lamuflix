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
  onHiddenChange: (id: string, hidden: boolean) => void;
}

function MovieRow({ item, filterActive, onRemoved, onHiddenChange }: MovieRowProps) {
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

  useEffect(() => {
    onHiddenChange(String(item.id), removed);
  }, [removed, item.id, onHiddenChange]);

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
  const [hiddenIds, setHiddenIds] = useState<ReadonlySet<string>>(() => new Set());
  const listRef = useRef<HTMLUListElement>(null);
  const emptyRef = useRef<HTMLParagraphElement>(null);
  const focusList = useCallback(() => {
    listRef.current?.focus();
  }, []);
  const setRowHidden = useCallback((id: string, hidden: boolean) => {
    setHiddenIds((prev) => {
      if (prev.has(id) === hidden) {
        return prev;
      }
      const next = new Set(prev);
      if (hidden) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }, []);

  const visibleItems = items.filter((item) => !hiddenIds.has(String(item.id)));
  const hasVisibleItems = visibleItems.length > 0;
  const hadVisibleItems = useRef(hasVisibleItems);

  useEffect(() => {
    if (hadVisibleItems.current && !hasVisibleItems) {
      emptyRef.current?.focus();
    }
    hadVisibleItems.current = hasVisibleItems;
  }, [hasVisibleItems]);

  if (!query.data && !leadingItem) {
    return null;
  }

  if (!hasVisibleItems) {
    return (
      <p ref={emptyRef} tabIndex={-1}>
        {inWatchlist
          ? 'Your watchlist is empty.'
          : 'No movies yet. Import a folder to get started.'}
      </p>
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
          onHiddenChange={setRowHidden}
        />
      ))}
    </ul>
  );
}
