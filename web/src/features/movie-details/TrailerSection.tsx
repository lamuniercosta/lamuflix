import { useId } from 'react';
import type { MovieMetadata } from './movieDetailsApi';

const TRAILER_KEY_PATTERN = /^[A-Za-z0-9_-]{11}$/;
const EMBED_BASE = 'https://www.youtube-nocookie.com/embed/';
const EMBED_PARAMS = '?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR';
const SECTION_LABEL = 'Trailer';
const NOTE = 'Requires internet access. Portuguese captions depend on availability.';

function isValidTrailerKey(key: string | null | undefined): key is string {
  return typeof key === 'string' && TRAILER_KEY_PATTERN.test(key);
}

export interface TrailerSectionProps {
  title: string;
  metadata?: Pick<MovieMetadata, 'trailerKey'> | null;
}

export default function TrailerSection({ title, metadata }: TrailerSectionProps) {
  const headingId = useId();
  const key = metadata?.trailerKey;
  if (!isValidTrailerKey(key)) {
    return null;
  }
  return (
    <section className="details-section details-trailer" aria-labelledby={headingId}>
      <h2 id={headingId} className="details-section-heading">
        {SECTION_LABEL}
      </h2>
      <div className="details-trailer-frame">
        <iframe
          src={`${EMBED_BASE}${key}${EMBED_PARAMS}`}
          title={`Trailer for ${title}`}
          loading="lazy"
          allow="encrypted-media; picture-in-picture; fullscreen"
          referrerPolicy="strict-origin-when-cross-origin"
          allowFullScreen
        />
      </div>
      <p className="details-status">{NOTE}</p>
    </section>
  );
}
