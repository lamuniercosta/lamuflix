import { useId, useState } from 'react';
import type { components } from '../api/types';
import {
  GENERIC_RETRY_ERROR,
  RetryRowError,
  useBrowseMoviesQuery,
  useRetryEnrichmentMutation,
} from './libraryQueries';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type EnrichmentStatus = components['schemas']['EnrichmentStatus'];

function statusBadgeText(status: EnrichmentStatus): string {
  return status === 'NotFound' ? 'Not found' : status;
}

function canRetry(status: EnrichmentStatus): boolean {
  return status === 'NotFound' || status === 'Failed';
}

function MovieRow({ item }: { item: MovieSummary }) {
  const errorId = useId();
  const [rowError, setRowError] = useState<string | null>(null);
  const retryEnrichment = useRetryEnrichmentMutation();
  const badge = statusBadgeText(item.enrichmentStatus);

  async function onRetry() {
    setRowError(null);
    try {
      await retryEnrichment.mutateAsync(item.id);
    } catch (error) {
      setRowError(error instanceof RetryRowError ? error.message : GENERIC_RETRY_ERROR);
    }
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
      {rowError ? (
        <p id={errorId} role="alert">
          {rowError}
        </p>
      ) : null}
    </li>
  );
}

export default function LibraryView({ leadingItem }: { leadingItem?: MovieSummary | null }) {
  const query = useBrowseMoviesQuery(leadingItem != null);
  const fetched = query.data?.items ?? [];
  const items =
    leadingItem && !fetched.some((item) => String(item.id) === String(leadingItem.id))
      ? [leadingItem, ...fetched]
      : fetched;

  if (!query.data && !leadingItem) {
    return null;
  }

  if (items.length === 0) {
    return <p>No movies yet. Import a folder to get started.</p>;
  }

  return (
    <ul aria-live="polite">
      {items.map((item) => (
        <MovieRow key={String(item.id)} item={item} />
      ))}
    </ul>
  );
}
