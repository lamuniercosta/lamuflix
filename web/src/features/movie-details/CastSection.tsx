import { useId } from 'react';
import type { components } from '../../api/types';
import { matchPersonIds } from './movieDetailsLabels';

type PersonDto = components['schemas']['PersonDto'];

export interface CastSectionProps {
  actors: string[];
  people: readonly PersonDto[];
  selected: string | null;
  onSelect: (name: string) => void;
}

/** The cast list; selectable names are the ones that resolve to known people. */
export function CastSection({ actors, people, selected, onSelect }: CastSectionProps) {
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

export default CastSection;
