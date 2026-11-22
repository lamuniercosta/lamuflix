import type { ForYouRecommendation } from './forYouApi';

export const MAX_SEED_ROWS = 4;
export const MAX_THEMED_ROW_CARDS = 6;

export const SERIES_ROW_LABEL = 'Next in a series you started';
export const WATCHLIST_ROW_LABEL = 'Close to your watchlist';
export const MORE_FOR_YOU_LABEL = 'More for you';

export type ForYouRowKind = 'series' | 'liked' | 'watched' | 'watchlist' | 'more';

export interface ForYouRow {
  key: string;
  kind: ForYouRowKind;
  label: string;
  movies: ForYouRecommendation[];
}

type SeedGroup = ForYouRow & { rank: number };

const SEED_LABEL_PREFIX: Record<'liked' | 'watched', string> = {
  liked: 'Because you liked ',
  watched: 'Because you watched ',
};

function movieId(movie: ForYouRecommendation): number {
  return Number(movie.id);
}

function uniqueMovies(items: ReadonlyArray<ForYouRecommendation>): ForYouRecommendation[] {
  const seen = new Set<number>();
  const unique: ForYouRecommendation[] = [];
  for (const movie of items) {
    const id = movieId(movie);
    if (seen.has(id)) {
      continue;
    }
    seen.add(id);
    unique.push(movie);
  }
  return unique;
}

function knownYear(movie: ForYouRecommendation): number | null {
  if (movie.releaseYear === null || movie.releaseYear === undefined) {
    return null;
  }
  return Number(movie.releaseYear);
}

function compareByYearThenRank(
  rank: ReadonlyMap<number, number>,
): (left: ForYouRecommendation, right: ForYouRecommendation) => number {
  return (left, right) => {
    const leftYear = knownYear(left);
    const rightYear = knownYear(right);
    if (leftYear !== rightYear) {
      if (leftYear === null) {
        return 1;
      }
      if (rightYear === null) {
        return -1;
      }
      return leftYear - rightYear;
    }
    return (rank.get(movieId(left)) ?? 0) - (rank.get(movieId(right)) ?? 0);
  };
}

export function groupForYou(items: ReadonlyArray<ForYouRecommendation>): ForYouRow[] {
  const ranked = uniqueMovies(items);
  const rank = new Map(ranked.map((movie, index) => [movieId(movie), index]));
  const compare = compareByYearThenRank(rank);

  const collections = new Map<number, ForYouRecommendation[]>();
  for (const movie of ranked) {
    if (movie.startedCollectionId === null || movie.startedCollectionId === undefined) {
      continue;
    }
    const collectionId = Number(movie.startedCollectionId);
    const members = collections.get(collectionId);
    if (members) {
      members.push(movie);
    } else {
      collections.set(collectionId, [movie]);
    }
  }

  const seriesSelection = Array.from(collections.values())
    .map((members) => [...members].sort(compare)[0])
    .sort(compare);
  const seriesIds = new Set(seriesSelection.map(movieId));

  const seedGroups = new Map<string, SeedGroup>();
  const watchlist: ForYouRecommendation[] = [];
  for (const movie of ranked) {
    if (seriesIds.has(movieId(movie))) {
      continue;
    }
    const seed = movie.seed;
    if (!seed) {
      continue;
    }
    if (seed.signal === 'watchlist') {
      watchlist.push(movie);
      continue;
    }
    const key = `${String(seed.movieId)}:${seed.signal}`;
    const group = seedGroups.get(key);
    if (group) {
      group.movies.push(movie);
      continue;
    }
    seedGroups.set(key, {
      key: `seed-${key}`,
      kind: seed.signal,
      label: `${SEED_LABEL_PREFIX[seed.signal]}${seed.title}`,
      movies: [movie],
      rank: rank.get(movieId(movie)) ?? 0,
    });
  }

  const chosenSeedRows = Array.from(seedGroups.values())
    .filter((group) => group.movies.length >= 2)
    .sort((left, right) => right.movies.length - left.movies.length || left.rank - right.rank)
    .slice(0, MAX_SEED_ROWS);

  const placed = new Set<number>();
  const rows: ForYouRow[] = [];

  const placeRow = (row: ForYouRow, minimum: number): void => {
    if (row.movies.length < minimum) {
      return;
    }
    const movies = row.movies.slice(0, MAX_THEMED_ROW_CARDS);
    if (movies.length < minimum) {
      return;
    }
    for (const movie of movies) {
      placed.add(movieId(movie));
    }
    rows.push({ ...row, movies });
  };

  placeRow({ key: 'series', kind: 'series', label: SERIES_ROW_LABEL, movies: seriesSelection }, 2);
  for (const group of chosenSeedRows) {
    placeRow(group, 2);
  }
  placeRow(
    { key: 'watchlist', kind: 'watchlist', label: WATCHLIST_ROW_LABEL, movies: watchlist },
    2,
  );

  const more = ranked.filter((movie) => !placed.has(movieId(movie)));
  if (more.length > 0) {
    rows.push({ key: 'more', kind: 'more', label: MORE_FOR_YOU_LABEL, movies: more });
  }

  return rows;
}
