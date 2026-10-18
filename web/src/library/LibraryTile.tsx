import { useEffect, useId, useRef, useState } from 'react';
import MovieCard, { type MovieSummaryResponse } from './MovieCard';
import MovieListRow from './MovieListRow';
import { GENERIC_RETRY_ERROR, RetryRowError, useRetryEnrichmentMutation } from './libraryQueries';
import { useWatchlistToggle } from './watchlistStore';

export type LibraryVariant = 'grid' | 'list';

export interface LibraryTileProps {
  item: MovieSummaryResponse;
  variant?: LibraryVariant;
  filterActive: boolean;
  onRemoved: () => void;
  onHiddenChange: (id: string, hidden: boolean) => void;
  onOpenMovie?: (id: number | string) => void;
}

export default function LibraryTile({
  item,
  variant = 'grid',
  filterActive,
  onRemoved,
  onHiddenChange,
  onOpenMovie,
}: LibraryTileProps) {
  const errorId = useId();
  const [tileError, setTileError] = useState<string | null>(null);
  const retryEnrichment = useRetryEnrichmentMutation();
  const { isInWatchlist, pending } = useWatchlistToggle(item.id, item.isInWatchlist);
  const removed = filterActive && !pending && !isInWatchlist;
  const wasRemoved = useRef(removed);

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
    setTileError(null);
    try {
      await retryEnrichment.mutateAsync(item.id);
    } catch (error) {
      setTileError(error instanceof RetryRowError ? error.message : GENERIC_RETRY_ERROR);
    }
  }

  if (removed) {
    return null;
  }

  const retryError = tileError;
  const sharedProps = {
    onOpenMovie,
    onRetry: () => void onRetry(),
    retryError,
    retryErrorId: errorId,
  };

  return (
    <div role="listitem">
      {variant === 'list' ? (
        <MovieListRow movie={item} {...sharedProps} />
      ) : (
        <MovieCard movie={item} {...sharedProps} />
      )}
    </div>
  );
}
