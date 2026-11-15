import { useId, type ReactNode } from 'react';
import { ArrowRightIcon } from '../../ui/icons';
import { libraryHrefForActors } from './movieDetailsLabels';
import { useMoreWithActors } from './movieDetailsQueries';
import MorePosterCard from './MorePosterCard';

export interface MoreWithPanelProps {
  actor: string;
  actorIds: Array<number | string>;
  movieId: number | string;
  onOpenMovie: (id: number | string) => void;
  onNavigate: (href: string) => void;
}

function moreStatus(
  pending: boolean,
  isError: boolean,
  isSuccess: boolean,
  rowCount: number,
  actor: string,
): ReactNode {
  if (pending) {
    return (
      <p role="status" className="details-status">
        Loading movies…
      </p>
    );
  }
  if (isError) {
    return (
      <p role="alert" className="details-status">
        Couldn&apos;t load more movies. Try again later.
      </p>
    );
  }
  if (isSuccess && rowCount === 0) {
    return <p className="details-status">{`No other movies with ${actor} in your library yet.`}</p>;
  }
  return null;
}

export function MoreWithPanel({
  actor,
  actorIds,
  movieId,
  onOpenMovie,
  onNavigate,
}: MoreWithPanelProps) {
  const headingId = useId();
  const more = useMoreWithActors(actorIds, actorIds.length > 0);
  const libraryHref = libraryHrefForActors(actorIds);
  const rows = (more.data ?? []).filter((item) => String(item.id) !== String(movieId));
  const status = moreStatus(more.isPending, more.isError, more.isSuccess, rows.length, actor);
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
            onNavigate(libraryHref);
          }}
        >
          {`See all with ${actor} in Library`}
          <ArrowRightIcon size={14} />
        </a>
      </div>
      {status}
      {rows.length > 0 && (
        <ul className="details-more-row">
          {rows.map((item) => (
            <MorePosterCard key={String(item.id)} item={item} onOpen={onOpenMovie} />
          ))}
        </ul>
      )}
    </section>
  );
}

export default MoreWithPanel;
