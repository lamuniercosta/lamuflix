import type { components } from '../api/types';
import { useBrowseMoviesQuery } from './libraryQueries';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type EnrichmentStatus = components['schemas']['EnrichmentStatus'];

function statusBadgeText(status: EnrichmentStatus): string {
  return status === 'NotFound' ? 'Not found' : status;
}

function canRetry(status: EnrichmentStatus): boolean {
  return status === 'NotFound' || status === 'Failed';
}

function MovieRow({ item }: { item: MovieSummary }) {
  const badge = statusBadgeText(item.enrichmentStatus);
  return (
    <li>
      <span>{item.title}</span> <span aria-label={badge}>{badge}</span>
      {canRetry(item.enrichmentStatus) ? (
        <button type="button" aria-label={`Retry ${item.title}`}>
          Retry
        </button>
      ) : null}
    </li>
  );
}

export default function LibraryView() {
  const query = useBrowseMoviesQuery();
  const items = query.data?.items ?? [];

  if (!query.data) {
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
