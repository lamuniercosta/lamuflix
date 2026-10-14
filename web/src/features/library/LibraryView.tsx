import { useEffect, useMemo, useState } from 'react';
import {
  QueryClient,
  QueryClientProvider,
  keepPreviousData,
  useQuery,
} from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';
import FilterPanel from './FilterPanel';
import {
  parseFilterQuery,
  toCanonicalApiQuery,
  currentYearExtent,
  RUNTIME_MIN_EXTENT,
  RUNTIME_MAX_EXTENT,
  YEAR_MIN_EXTENT,
} from './filterQuery';

type FilterQuery = ReturnType<typeof parseFilterQuery>;

function readQuery() {
  return parseFilterQuery(window.location.search);
}

function toBool(value: unknown): boolean {
  if (typeof value === 'boolean') {
    return value;
  }
  if (value == null) {
    return false;
  }
  return String(value).toLowerCase() === 'true';
}

function toNumberOrNull(value: unknown): number | null {
  if (value == null) {
    return null;
  }
  const text = String(value).trim();
  if (text === '') {
    return null;
  }
  const number = Number(text);
  return Number.isFinite(number) ? number : null;
}

function buildMovieParams(query: FilterQuery) {
  const params: Record<string, unknown> = {};

  if (query.text) {
    params.text = query.text;
  }

  if (Array.isArray(query.genreIds) && query.genreIds.length > 0) {
    params.genreIds = query.genreIds.map((value) => String(value));
  }

  if (Array.isArray(query.actorIds) && query.actorIds.length > 0) {
    params.actorIds = query.actorIds.map((value) => String(value));
  }

  const runtimeActive =
    toBool(query.runtimeIncludeUnknown) ||
    (toNumberOrNull(query.runtimeMin) !== null &&
      toNumberOrNull(query.runtimeMin) !== RUNTIME_MIN_EXTENT) ||
    (toNumberOrNull(query.runtimeMax) !== null &&
      toNumberOrNull(query.runtimeMax) !== RUNTIME_MAX_EXTENT);

  if (runtimeActive) {
    const runtimeMinValue = toNumberOrNull(query.runtimeMin);
    const runtimeMaxValue = toNumberOrNull(query.runtimeMax);
    params.runtimeMin =
      runtimeMinValue === null || runtimeMinValue === RUNTIME_MIN_EXTENT
        ? ''
        : String(query.runtimeMin).trim();
    params.runtimeMax =
      runtimeMaxValue === null || runtimeMaxValue === RUNTIME_MAX_EXTENT
        ? ''
        : String(query.runtimeMax).trim();
    params.runtimeIncludeUnknown = String(toBool(query.runtimeIncludeUnknown));
  }

  const currentYear = currentYearExtent();
  const yearActive =
    (toNumberOrNull(query.yearMin) !== null && toNumberOrNull(query.yearMin) !== YEAR_MIN_EXTENT) ||
    (toNumberOrNull(query.yearMax) !== null && toNumberOrNull(query.yearMax) !== currentYear);

  if (yearActive) {
    const yearMinValue = toNumberOrNull(query.yearMin);
    const yearMaxValue = toNumberOrNull(query.yearMax);
    params.yearMin =
      yearMinValue === null || yearMinValue === YEAR_MIN_EXTENT ? '' : String(query.yearMin).trim();
    params.yearMax =
      yearMaxValue === null || yearMaxValue === currentYear ? '' : String(query.yearMax).trim();
  }

  if (Array.isArray(query.statuses) && query.statuses.length > 0) {
    params.statuses = query.statuses.map((value) => String(value));
  }

  params.sort = query.sort || 'Title';
  params.direction = query.direction || 'Ascending';
  params.page = String(
    Number.isFinite(Number(query.page)) && String(query.page).trim() !== '' ? query.page : 1,
  );
  params.pageSize = String(
    Number.isFinite(Number(query.pageSize)) && String(query.pageSize).trim() !== ''
      ? query.pageSize
      : 20,
  );

  if (query.inWatchlist != null && String(query.inWatchlist).trim() !== '') {
    params.inWatchlist = String(query.inWatchlist);
  }

  return params;
}

function GenreDtoList(value: unknown): components['schemas']['GenreDto'][] {
  return Array.isArray(value) ? (value as components['schemas']['GenreDto'][]) : [];
}

function PersonDtoList(value: unknown): components['schemas']['PersonDto'][] {
  return Array.isArray(value) ? (value as components['schemas']['PersonDto'][]) : [];
}

function countLabel(total: unknown): string {
  const value = Number(total);
  const safe = Number.isFinite(value) ? value : 0;
  return safe === 1 ? '1 movie' : `${safe} movies`;
}

function LibraryViewInner() {
  const [query, setQuery] = useState<FilterQuery>(() => readQuery());

  useEffect(() => {
    const handler = () => setQuery(readQuery());
    window.addEventListener('popstate', handler);
    return () => window.removeEventListener('popstate', handler);
  }, []);

  const apiUrl = useMemo(() => toCanonicalApiQuery(query), [query]);

  const moviesQuery = useQuery({
    queryKey: ['movies', apiUrl],
    queryFn: async () => {
      const movieParams = buildMovieParams(query);
      const { data, error } = await client.GET('/api/movies', {
        params: { query: movieParams as never },
      });
      if (error) {
        throw new Error('Movies could not be refreshed.');
      }
      return data;
    },
    placeholderData: keepPreviousData,
  });

  const genresQuery = useQuery({
    queryKey: ['genres'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/genres');
      if (error) {
        throw new Error('Genres could not be loaded.');
      }
      return data;
    },
  });

  const actorsQuery = useQuery({
    queryKey: ['people', 'actor'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/people', {
        params: { query: { Role: 'actor' } as never },
      });
      if (error) {
        throw new Error('Cast could not be loaded.');
      }
      return data;
    },
  });

  const movieTitles = moviesQuery.data?.items ?? [];
  const pendingRefetch = moviesQuery.isFetching && movieTitles.length > 0;

  return (
    <main className="flex min-h-screen flex-col gap-6 bg-slate-950 px-6 py-6 text-slate-50 lg:flex-row">
      <div className="w-full lg:w-80">
        <FilterPanel
          query={query}
          genres={GenreDtoList(genresQuery.data)}
          actors={PersonDtoList(actorsQuery.data)}
          onChange={setQuery as (value: unknown) => void}
        />
      </div>
      <section className="flex-1" aria-label="Results">
        <div aria-live="polite">
          {moviesQuery.error ? (
            <p role="alert">Movies could not be refreshed.</p>
          ) : (
            <p>{countLabel(moviesQuery.data?.totalCount)}</p>
          )}
        </div>
        <div className={pendingRefetch ? 'opacity-60' : ''}>
          {moviesQuery.isPending ? (
            <p>Loading movies...</p>
          ) : movieTitles.length === 0 ? (
            <p>No movies match these filters.</p>
          ) : (
            <ul>
              {movieTitles.map((item) => (
                <li key={String(item.id)}>{item.title}</li>
              ))}
            </ul>
          )}
        </div>
      </section>
    </main>
  );
}

export default function LibraryView() {
  const [queryClient] = useState(() => new QueryClient());
  return (
    <QueryClientProvider client={queryClient}>
      <LibraryViewInner />
    </QueryClientProvider>
  );
}
