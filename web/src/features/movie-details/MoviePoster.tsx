import { useState } from 'react';
import { FilmIcon } from '../../ui/icons';

export interface MoviePosterProps {
  title: string;
  posterUrl: string | null;
}

export function MoviePoster({ title, posterUrl }: MoviePosterProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  if (posterUrl && posterUrl !== failedUrl) {
    return (
      <img
        className="details-poster"
        src={posterUrl}
        alt={`${title} poster`}
        onError={() => setFailedUrl(posterUrl)}
      />
    );
  }
  return (
    <div
      className="details-poster details-poster-placeholder"
      role="img"
      aria-label={`${title} poster`}
    >
      <FilmIcon size={48} />
    </div>
  );
}

export default MoviePoster;
