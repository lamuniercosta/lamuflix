import { useId } from 'react';
import { useOutsideLibraryRecommendations, type OutsideLibraryCard } from './outsideLibraryApi';
import { tmdbMoviePageUrl, tmdbPosterUrl } from './outsideLibraryUrls';
import './OutsideLibrarySection.css';

export const OUTSIDE_LIBRARY_HEADING = 'Not in your library yet';
export const TMDB_TERMS_URL = 'https://www.themoviedb.org/api-terms-of-use';

function OutsideLibraryPoster({ card }: { card: OutsideLibraryCard }) {
  const posterUrl = tmdbPosterUrl(card.posterPath);
  return posterUrl === null ? (
    <div className="outside-library-poster outside-library-poster-fallback">Poster unavailable</div>
  ) : (
    <img className="outside-library-poster" src={posterUrl} alt={`${card.title} poster`} />
  );
}

function OutsideLibraryCardView({ card }: { card: OutsideLibraryCard }) {
  const movieUrl = tmdbMoviePageUrl(card.tmdbId);
  return (
    <li className="outside-library-card">
      <OutsideLibraryPoster card={card} />
      <div className="outside-library-card-body">
        <h3 className="outside-library-card-title">{card.title}</h3>
        {card.releaseYear === null ? null : (
          <p className="outside-library-card-year">{card.releaseYear}</p>
        )}
        <p className="outside-library-card-reason">{card.reason}</p>
        {movieUrl === null ? null : (
          <a
            className="lamu-link"
            href={movieUrl}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={`View ${card.title} on TMDB (opens in a new tab)`}
          >
            View on TMDB
          </a>
        )}
      </div>
    </li>
  );
}

export default function OutsideLibrarySection() {
  const headingId = useId();
  const { data, isPending, isError } = useOutsideLibraryRecommendations();
  if (isPending || isError || data === undefined || data.length === 0) {
    return null;
  }

  return (
    <section className="outside-library-section" aria-labelledby={headingId}>
      <div className="outside-library-header">
        <h2 id={headingId} className="outside-library-heading">
          {OUTSIDE_LIBRARY_HEADING}
        </h2>
        <p className="outside-library-credit">
          This product uses{' '}
          <a href={TMDB_TERMS_URL} aria-label="TMDB API terms of use">
            TMDB
          </a>{' '}
          and the TMDB APIs but is not endorsed, certified, or otherwise approved by TMDB.
        </p>
      </div>
      <ul className="outside-library-cards">
        {data.map((card) => (
          <OutsideLibraryCardView key={card.tmdbId} card={card} />
        ))}
      </ul>
    </section>
  );
}
