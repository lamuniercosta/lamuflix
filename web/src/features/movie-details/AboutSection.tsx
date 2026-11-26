import { Fragment, useId } from 'react';
import type { components } from '../../api/types';
import type { MovieMetadata } from './movieDetailsApi';
import { matchPersonIds } from './movieDetailsLabels';

type PersonDto = components['schemas']['PersonDto'];

export interface AboutSectionProps {
  title: string;
  metadata: MovieMetadata;
  people: readonly PersonDto[];
  selected: string | null;
  onSelect: (name: string) => void;
}

interface AboutRow {
  label: string;
  values: string[];
  selectable?: boolean;
}

function displayed(values: readonly string[]): string[] {
  return values.filter((value) => value.trim().length > 0);
}

export function AboutSection({ title, metadata, people, selected, onSelect }: AboutSectionProps) {
  const headingId = useId();
  const directors = displayed(metadata.directors);
  const actors = displayed(metadata.actors);
  const writers = displayed(metadata.writers);
  const genres = displayed(metadata.genres);
  const collection = metadata.collectionName?.trim() ? metadata.collectionName : null;

  const rows: AboutRow[] = [];
  if (directors.length > 0) {
    rows.push({
      label: directors.length > 1 ? 'Directors' : 'Director',
      values: directors,
    });
  }
  if (actors.length > 0) {
    rows.push({ label: 'Cast', values: actors, selectable: true });
  }
  if (writers.length > 0) {
    rows.push({ label: writers.length > 1 ? 'Writers' : 'Writer', values: writers });
  }
  if (genres.length > 0) {
    rows.push({ label: 'Genres', values: genres });
  }
  if (collection !== null) {
    rows.push({ label: 'Collection', values: [collection] });
  }
  if (rows.length === 0) return null;

  return (
    <section className="details-section details-about" aria-labelledby={headingId}>
      <h2 id={headingId} className="details-section-heading">
        About {title}
      </h2>
      <dl className="details-about-list" aria-labelledby={headingId}>
        {rows.map((row) => (
          <div className="details-about-row" key={row.label}>
            <dt>{row.label}</dt>
            <dd>
              {row.values.map((value, index) => (
                <Fragment key={`${index}-${value}`}>
                  {index > 0 && ', '}
                  {row.selectable && matchPersonIds(value, people).length > 0 ? (
                    <button
                      type="button"
                      className="lamu-chip"
                      aria-pressed={selected === value}
                      onClick={() => onSelect(value)}
                    >
                      {value}
                    </button>
                  ) : (
                    <span>{value}</span>
                  )}
                </Fragment>
              ))}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

export default AboutSection;
