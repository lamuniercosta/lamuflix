import { useState } from 'react';

type MovieLike = Record<string, unknown>;

export interface MovieCardProps {
  movie?: MovieLike;
  item?: MovieLike;
  data?: MovieLike;
  [key: string]: unknown;
}

function isRecord(value: unknown): value is MovieLike {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function extractMovie(props: MovieCardProps): MovieLike {
  const source = props.movie ?? props.item ?? props.data ?? props;
  const base = isRecord(source) ? { ...source } : {};
  const keys = [
    'id',
    'title',
    'posterUrl',
    'poster_url',
    'poster',
    'releaseYear',
    'release_year',
    'year',
    'runtime',
    'runtimeMinutes',
    'imdbRating',
    'imdb_rating',
    'rating',
    'enrichmentStatus',
    'enrichment_status',
    'status',
  ];
  for (const key of keys) {
    if (props[key] !== undefined) {
      base[key] = props[key];
    }
  }
  return base;
}

function toNullableNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === '') return null;
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

export default function MovieCard(props: MovieCardProps) {
  const movie = extractMovie(props);
  const title = String(movie.title ?? movie.name ?? 'Untitled');
  const poster = movie.posterUrl ?? movie.poster_url ?? movie.poster ?? null;
  const year = toNullableNumber(movie.releaseYear ?? movie.release_year ?? movie.year);
  const runtime = toNullableNumber(movie.runtime ?? movie.runtimeMinutes ?? movie.runtime_minutes);
  const rating = toNullableNumber(movie.imdbRating ?? movie.imdb_rating ?? movie.rating);
  const status = String(
    movie.enrichmentStatus ?? movie.enrichment_status ?? movie.status ?? 'Pending',
  );

  const [broken, setBroken] = useState(false);

  const showImage = typeof poster === 'string' && poster !== '' && !broken;

  return (
    <article className="movie-card">
      <div className="movie-card-media" style={{ aspectRatio: '2 / 3' }}>
        {showImage ? (
          <img src={String(poster)} alt={title} onError={() => setBroken(true)} />
        ) : (
          <div className="movie-card-placeholder" role="img" aria-label={title}>
            <span>{title}</span>
          </div>
        )}
      </div>
      <h3 className="movie-card-title">{title}</h3>
      <p className="movie-card-meta">
        <span>{year === null ? '—' : year}</span>
        <span>{runtime === null ? '—' : `${runtime} min`}</span>
        <span
          className="movie-card-rating"
          aria-label={`IMDb rating ${rating === null ? '—' : rating.toFixed(1)}`}
        >
          {rating === null ? '—' : rating.toFixed(1)}
        </span>
      </p>
      <div className="movie-card-status">
        <span className="movie-status-chip">{status}</span>
      </div>
    </article>
  );
}
