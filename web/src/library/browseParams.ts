import type { paths } from '../api/types';
import {
  currentYearExtent,
  formatRatingBound,
  isRatingActive,
  isRuntimeActive,
  isYearActive,
  parseFilterQuery,
  RUNTIME_MAX_EXTENT,
  RUNTIME_MIN_EXTENT,
  YEAR_MIN_EXTENT,
} from '../features/library/filterQuery';

type BrowseMoviesQuery = paths['/api/movies']['get']['parameters']['query'];

export type BrowseParams = {
  sort: string;
  direction: string;
  page: number;
  pageSize: number;
  text?: string;
  genreIds?: string[];
  actorIds?: string[];
  runtimeMin?: string;
  runtimeMax?: string;
  runtimeIncludeUnknown?: string;
  yearMin?: string;
  yearMax?: string;
  minRating?: BrowseMoviesQuery['MinRating'];
  maxRating?: BrowseMoviesQuery['MaxRating'];
  statuses?: string[];
  collectionId?: string;
};

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

function toBool(value: unknown): boolean {
  if (typeof value === 'boolean') {
    return value;
  }
  if (value == null) {
    return false;
  }
  return String(value).toLowerCase() === 'true';
}

function formatBound(value: unknown, extent: number): string {
  const number = toNumberOrNull(value);
  if (number === null || number === extent) {
    return '';
  }
  return String(value).trim();
}

export function buildBrowseParams(
  input: unknown,
  currentYear: number = currentYearExtent(),
): BrowseParams {
  const query = parseFilterQuery(input);
  const params: Record<string, unknown> = {};

  if (query.text) {
    params.text = query.text;
  }

  if (query.genreIds.length > 0) {
    params.genreIds = query.genreIds.map((value) => String(value));
  }

  if (query.actorIds.length > 0) {
    params.actorIds = query.actorIds.map((value) => String(value));
  }

  if (isRuntimeActive(query.runtimeMin, query.runtimeMax, query.runtimeIncludeUnknown)) {
    params.runtimeMin = formatBound(query.runtimeMin, RUNTIME_MIN_EXTENT);
    params.runtimeMax = formatBound(query.runtimeMax, RUNTIME_MAX_EXTENT);
    params.runtimeIncludeUnknown = String(toBool(query.runtimeIncludeUnknown));
  }

  if (isYearActive(query.yearMin, query.yearMax, currentYear)) {
    params.yearMin = formatBound(query.yearMin, YEAR_MIN_EXTENT);
    params.yearMax = formatBound(query.yearMax, currentYear);
  }

  if (query.minRating != null || query.maxRating != null) {
    if (isRatingActive(query.minRating, query.maxRating)) {
      params.minRating = formatRatingBound(query.minRating);
      params.maxRating = formatRatingBound(query.maxRating);
    } else {
      if (query.minRating != null) {
        params.minRating = String(query.minRating);
      }
      if (query.maxRating != null) {
        params.maxRating = String(query.maxRating);
      }
    }
  }

  if (query.statuses.length > 0) {
    params.statuses = query.statuses.map((value) => String(value));
  }

  if (query.collectionId != null) {
    params.collectionId = String(query.collectionId);
  }

  params.sort = query.sort || 'Title';
  params.direction = query.direction || 'Ascending';
  params.page = toNumberOrNull(query.page) ?? 1;
  params.pageSize = toNumberOrNull(query.pageSize) ?? 20;

  return params as unknown as BrowseParams;
}
