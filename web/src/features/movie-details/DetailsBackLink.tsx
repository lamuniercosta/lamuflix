import type { MouseEvent } from 'react';
import { ArrowLeftIcon } from '../../ui/icons';
import { backLinkLabel } from './movieDetailsLabels';
import { useGenresLookup, usePeopleLookup } from './movieDetailsQueries';

type DetailsBackLinkProps = {
  href: string;
  onClick: (event: MouseEvent<HTMLAnchorElement>) => void;
};

export default function DetailsBackLink({ href, onClick }: DetailsBackLinkProps) {
  const genres = useGenresLookup();
  const people = usePeopleLookup();
  const label = backLinkLabel(href, {
    genres: genres.data ?? [],
    people: people.data ?? [],
  });

  return (
    <p className="details-back">
      <a className="lamu-link details-back-link" href={href} onClick={onClick}>
        <ArrowLeftIcon size={16} />
        {label}
      </a>
    </p>
  );
}
