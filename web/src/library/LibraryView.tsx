import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { client } from '../api/client';
import { subscribeToRouteChange } from '../features/app-navigation/appNavigation';
import {
  currentYearExtent,
  parseFilterQuery,
  toCanonicalApiQuery,
  toUrlQuery,
} from '../features/library/filterQuery';
import type { FilterQuery } from '../features/library/filterQuery';
import type { components } from '../api/types';
import { actorPeopleParams } from './peopleQuery';
import LibraryBrowseView from './LibraryBrowseView';
import LibraryErrorBoundary from './LibraryErrorBoundary';
import './library.css';
import './browse.css';

type MovieSummary = components['schemas']['MovieSummaryResponse'];
type GenreDto = components['schemas']['GenreDto'];
type CollectionDto = components['schemas']['CollectionDto'];
type PersonDto = components['schemas']['PersonDto'];
type DirectorDto = components['schemas']['DirectorDto'];

function GenreDtoList(value: unknown): GenreDto[] {
  return Array.isArray(value) ? (value as GenreDto[]) : [];
}

function CollectionDtoList(value: unknown): CollectionDto[] {
  return Array.isArray(value) ? (value as CollectionDto[]) : [];
}

function PersonDtoList(value: unknown): PersonDto[] {
  return Array.isArray(value) ? (value as PersonDto[]) : [];
}

function DirectorDtoList(value: unknown): DirectorDto[] {
  return Array.isArray(value) ? (value as DirectorDto[]) : [];
}

function replaceQuery(next: FilterQuery, currentYear: number): void {
  try {
    const search = toUrlQuery(next, currentYear);
    const path = window.location.pathname || '/';
    window.history.replaceState(null, '', search ? `${path}?${search}` : path);
  } catch {
    /* history is unavailable in this environment */
  }
}

export interface LibraryViewProps {
  leadingItem?: MovieSummary | null;
  inWatchlist?: boolean;
  query?: FilterQuery;
  routeKey?: number;
  routePath?: string;
  onQueryChange?: (next: FilterQuery, options?: { replace?: boolean }) => void;
  onOpenMovie?: (id: string | number) => void;
}

export default function LibraryView({
  leadingItem,
  inWatchlist = false,
  query: controlledQuery,
  routeKey,
  routePath,
  onQueryChange,
  onOpenMovie,
}: LibraryViewProps) {
  const [internalQuery, setInternalQuery] = useState<FilterQuery>(() =>
    parseFilterQuery(window.location.search),
  );
  const [currentYear] = useState(() => currentYearExtent(new Date()));
  const integrated = controlledQuery != null;

  const syncFromUrl = useCallback(() => {
    if (!integrated) {
      setInternalQuery(parseFilterQuery(window.location.search));
    }
  }, [integrated]);

  useEffect(() => {
    if (integrated) {
      return;
    }
    return subscribeToRouteChange(syncFromUrl);
  }, [syncFromUrl, integrated]);

  const prevWatchlist = useRef(inWatchlist);
  useEffect(() => {
    if (prevWatchlist.current !== inWatchlist) {
      prevWatchlist.current = inWatchlist;
      syncFromUrl();
    }
  }, [inWatchlist, syncFromUrl]);

  const query = controlledQuery ?? internalQuery;

  const handleQueryChange = useCallback(
    (value: unknown) => {
      const next = parseFilterQuery(value);
      if (integrated) {
        onQueryChange?.(next);
      } else {
        replaceQuery(next, currentYear);
        setInternalQuery(next);
      }
    },
    [currentYear, integrated, onQueryChange],
  );

  const handlePageChange = useCallback(
    (page: number) => {
      const next = parseFilterQuery(query);
      next.page = page;
      if (integrated) {
        onQueryChange?.(next);
      } else {
        replaceQuery(next, currentYear);
        setInternalQuery(next);
      }
      try {
        window.scrollTo(0, 0);
      } catch {
        /* scroll unavailable in this environment */
      }
    },
    [query, currentYear, integrated, onQueryChange],
  );

  const handleClearFilters = useCallback(() => {
    const next = parseFilterQuery({});
    next.unknown = { ...query.unknown };
    if (integrated) {
      onQueryChange?.(next);
    } else {
      replaceQuery(next, currentYear);
      setInternalQuery(next);
    }
  }, [query, currentYear, integrated, onQueryChange]);

  const apiUrl = useMemo(() => toCanonicalApiQuery(query, currentYear), [query, currentYear]);

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

  const collectionsQuery = useQuery({
    queryKey: ['collections'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/collections');
      if (error) {
        throw new Error('Collections could not be loaded.');
      }
      return data;
    },
  });

  const directorsQuery = useQuery({
    queryKey: ['directors'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/directors');
      if (error) {
        throw new Error('Directors could not be loaded.');
      }
      return data;
    },
  });

  const actorsQuery = useQuery({
    queryKey: ['people', 'actor'],
    queryFn: async () => {
      const { data, error } = await client.GET('/api/people', {
        params: actorPeopleParams(),
      });
      if (error) {
        throw new Error('Cast could not be loaded.');
      }
      return data;
    },
  });

  return (
    <LibraryErrorBoundary resetKey={apiUrl} clearFilters={handleClearFilters}>
      <LibraryBrowseView
        query={query}
        currentYear={currentYear}
        onQueryChange={handleQueryChange}
        onClearFilters={handleClearFilters}
        onPageChange={handlePageChange}
        leadingItem={leadingItem}
        inWatchlist={inWatchlist}
        routeKey={routeKey}
        routePath={routePath}
        integrated={integrated}
        onOpenMovie={onOpenMovie}
        genres={GenreDtoList(genresQuery.data)}
        collections={CollectionDtoList(collectionsQuery.data)}
        actors={PersonDtoList(actorsQuery.data)}
        directors={DirectorDtoList(directorsQuery.data)}
        genresError={genresQuery.error}
        collectionsError={collectionsQuery.error}
        actorsError={actorsQuery.error}
        directorsError={directorsQuery.error}
        directorsLoading={directorsQuery.isLoading}
        onRetryDirectors={() => void directorsQuery.refetch()}
      />
    </LibraryErrorBoundary>
  );
}
