import { useState, type MouseEvent } from 'react';
import type { components } from '../../api/types';
import { formatRating } from '../../library/movieCardFormat';
import { FilmIcon, StarFilledIcon } from '../../ui/icons';

type MovieSummary = components['schemas']['MovieSummaryResponse'];

export interface MorePosterCardProps {
  item: MovieSummary;
  /** Navigation callback supplied by the shell; this card never writes history. */
  onOpen: (id: number | string) => void;
}

function PosterArt({
  item,
  failed,
  onFail,
}: {
  item: MovieSummary;
  failed: boolean;
  onFail: () => void;
}) {
  if (item.posterUrl && !failed) {
    return <img src={item.posterUrl} alt="" loading="lazy" onError={onFail} />;
  }
  return (
    <span className="details-more-placeholder">
      <FilmIcon size={28} />
    </span>
  );
}

function posterMeta(item: MovieSummary, rating: string | null) {
  if (item.enrichmentStatus !== 'Enriched') {
    return 'No metadata yet';
  }
  return (
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
  );
}

/** One related-poster card, owning its own image-failure state. */
export function MorePosterCard({ item, onOpen }: MorePosterCardProps) {
  const [posterFailed, setPosterFailed] = useState(false);
  const href = `/movies/${String(item.id)}`;
  const open = (event: MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    onOpen(item.id);
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
        <PosterArt item={item} failed={posterFailed} onFail={() => setPosterFailed(true)} />
      </a>
      <a className="details-more-title" href={href} onClick={open}>
        {item.title}
      </a>
      <p className="details-more-meta">{posterMeta(item, rating)}</p>
    </li>
  );
}

export default MorePosterCard;
