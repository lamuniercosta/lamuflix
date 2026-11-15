import { useId, useState } from 'react';
import { CopyIcon, ExternalLinkIcon } from '../../ui/icons';
import ManualMatchControl from './ManualMatchControl';

type CopyState = 'idle' | 'copied' | 'failed';

export interface FileSectionProps {
  path: string;
  imdbId: string | null;
  movieId: number | string;
  libraryClient: import('@tanstack/react-query').QueryClient;
  showMatch: boolean;
}

/** File path, copy feedback and the external/match affordances. */
export function FileSection({ path, imdbId, movieId, libraryClient, showMatch }: FileSectionProps) {
  const headingId = useId();
  const [copyState, setCopyState] = useState<CopyState>('idle');
  const copyPath = async () => {
    try {
      await navigator.clipboard.writeText(path);
      setCopyState('copied');
    } catch {
      setCopyState('failed');
    }
  };
  return (
    <section className="details-section details-file" aria-labelledby={headingId}>
      <h2 id={headingId} className="details-section-heading">
        File
      </h2>
      <p className="details-path">{path}</p>
      <div className="details-file-actions">
        <button type="button" className="lamu-link" onClick={() => void copyPath()}>
          <CopyIcon size={16} />
          Copy path
        </button>
        {copyState === 'copied' && <span role="status">Copied</span>}
        {copyState === 'failed' && <span role="alert">Couldn&apos;t copy the path.</span>}
        {imdbId && (
          <a
            className="lamu-link"
            href={`https://www.imdb.com/title/${imdbId}/`}
            target="_blank"
            rel="noopener"
          >
            View on IMDb
            <ExternalLinkIcon size={16} />
          </a>
        )}
        {showMatch && (
          <ManualMatchControl
            movieId={movieId}
            libraryClient={libraryClient}
            className="lamu-link"
          />
        )}
      </div>
    </section>
  );
}

export default FileSection;
