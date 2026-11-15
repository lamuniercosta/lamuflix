import { useId } from 'react';
import { ArrowRightIcon } from '../../ui/icons';
import { libraryHrefForActors } from './movieDetailsLabels';
import { useMoreWithActors } from './movieDetailsQueries';
import MorePosterCard from './MorePosterCard';

export interface MoreWithPanelProps {
  actor: string;
  actorIds: Array<number | string>;
  movieId: number | string;
  /** Navigation callbacks supplied by the shell; nothing here writes history. */
  onOpenMovie: (id: number | string) => void;
  onNavigate: (href: string) => void;
}

/** Related presentation for the selected actor, plus the library jump. */
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
            <MorePosterCard key={String(item.id)} item={item} onOpen={onOpenMovie} />
          ))}
        </ul>
      )}
    </section>
  );
}

export default MoreWithPanel;
