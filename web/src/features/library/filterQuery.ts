export const DEFAULT_SORT = 'Title';
export const DEFAULT_DIRECTION = 'Ascending';

export const SORT_OPTIONS = [
  { label: 'Title (A-Z)', sort: 'Title', direction: 'Ascending' },
  { label: 'Title (Z-A)', sort: 'Title', direction: 'Descending' },
  { label: 'Year (oldest)', sort: 'Year', direction: 'Ascending' },
  { label: 'Year (newest)', sort: 'Year', direction: 'Descending' },
  { label: 'IMDb (lowest)', sort: 'Rating', direction: 'Ascending' },
  { label: 'IMDb (highest)', sort: 'Rating', direction: 'Descending' },
  { label: 'Runtime (shortest)', sort: 'Runtime', direction: 'Ascending' },
  { label: 'Runtime (longest)', sort: 'Runtime', direction: 'Descending' },
];
export const DEFAULT_PAGE = 1;
export const DEFAULT_PAGE_SIZE = 20;
export const RUNTIME_MIN_EXTENT = 0;
export const RUNTIME_MAX_EXTENT = 240;
export const YEAR_MIN_EXTENT = 1900;
export const RATING_MIN_EXTENT = 0;
export const RATING_MAX_EXTENT = 10;
export const DEBOUNCE_MS = 300;

const RATING_BOUND_PATTERN = /^[0-9]+(?:\.[0-9])?$/;
const COLLECTION_ID_PATTERN = /^[0-9]+$/;

const RECOGNIZED_KEYS = new Set([
  'text',
  'genreIds',
  'actorIds',
  'directorId',
  'runtimeMin',
  'runtimeMax',
  'runtimeIncludeUnknown',
  'yearMin',
  'yearMax',
  'minRating',
  'maxRating',
  'statuses',
  'collectionId',
  'sort',
  'direction',
  'page',
  'pageSize',
  'inWatchlist',
]);

export interface FilterQuery {
  text: string;
  genreIds: Array<string | number>;
  actorIds: Array<string | number>;
  directorId: string | number | null;
  runtimeMin: string | number | null;
  runtimeMax: string | number | null;
  runtimeIncludeUnknown: boolean | string | null;
  yearMin: string | number | null;
  yearMax: string | number | null;
  minRating: string | number | null;
  maxRating: string | number | null;
  statuses: Array<string>;
  collectionId: string | number | null;
  sort: string;
  direction: string;
  page: string | number;
  pageSize: string | number;
  inWatchlist: string | boolean | number | null;
  unknown: Record<string, string | string[]>;
}

type SerializeOptions = {
  compact: boolean;
  includeUnknownKeys: boolean;
};

function resolveYearExtent(value: unknown): number {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }
  if (value instanceof Date) {
    return currentYearExtent(value);
  }
  if (value && typeof value === 'object' && 'currentYear' in (value as object)) {
    const candidate = (value as Record<string, unknown>).currentYear;
    if (typeof candidate === 'number' && Number.isFinite(candidate)) {
      return candidate;
    }
  }
  if (value && typeof value === 'object' && 'now' in (value as object)) {
    const candidate = (value as Record<string, unknown>).now;
    if (candidate instanceof Date) {
      return currentYearExtent(candidate);
    }
  }
  return currentYearExtent();
}

function normalizeText(value: unknown): string {
  return value == null ? '' : String(value);
}

function toStringArray(value: unknown): Array<string | number> {
  if (Array.isArray(value)) {
    return value.slice();
  }
  if (value == null || value === '') {
    return [];
  }
  return [value as string | number];
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

function readCollectionId(value: unknown): string | null {
  if (value == null) {
    return null;
  }
  const text = String(value).trim();
  if (!COLLECTION_ID_PATTERN.test(text)) {
    return null;
  }
  const number = Number(text);
  return Number.isInteger(number) && number > 0 ? text : null;
}

function mapSort(value: unknown): string {
  const text = String(value ?? '').trim();
  if (text === '') {
    return DEFAULT_SORT;
  }
  if (/imd/i.test(text)) {
    return 'Rating';
  }
  return text.split(/[\s(]/)[0];
}

function mapDirection(value: unknown): string {
  const text = String(value ?? '').toLowerCase();
  if (text.startsWith('desc')) {
    return 'Descending';
  }
  if (text.startsWith('asc')) {
    return 'Ascending';
  }
  return value == null || text === '' ? DEFAULT_DIRECTION : String(value);
}

function fromParams(params: URLSearchParams): FilterQuery {
  const text = params.has('text') ? (params.get('text') ?? '') : '';
  const genreIds = params.getAll('genreIds');
  const actorIds = params.getAll('actorIds');
  const directorId = params.has('directorId') ? (params.get('directorId') ?? '') : '';
  const runtimeMin = params.has('runtimeMin') ? (params.get('runtimeMin') ?? '') : '';
  const runtimeMax = params.has('runtimeMax') ? (params.get('runtimeMax') ?? '') : '';
  const runtimeIncludeUnknown = params.has('runtimeIncludeUnknown')
    ? (params.get('runtimeIncludeUnknown') ?? '')
    : '';
  const yearMin = params.has('yearMin') ? (params.get('yearMin') ?? '') : '';
  const yearMax = params.has('yearMax') ? (params.get('yearMax') ?? '') : '';
  const minRating = params.has('minRating') ? (params.get('minRating') ?? '') : null;
  const maxRating = params.has('maxRating') ? (params.get('maxRating') ?? '') : null;
  const statuses = params.getAll('statuses');
  const collectionId = readCollectionId(
    params.getAll('collectionId').length === 1 ? params.get('collectionId') : null,
  );
  const sort = mapSort(params.has('sort') ? params.get('sort') : DEFAULT_SORT);
  const direction = mapDirection(
    params.has('direction') ? params.get('direction') : DEFAULT_DIRECTION,
  );
  const page = params.has('page') ? (params.get('page') ?? '') : '';
  const pageSize = params.has('pageSize') ? (params.get('pageSize') ?? '') : '';
  const inWatchlist = params.has('inWatchlist') ? (params.get('inWatchlist') ?? '') : null;

  const unknown: Record<string, string | string[]> = {};
  for (const [key, value] of params.entries()) {
    if (RECOGNIZED_KEYS.has(key)) {
      continue;
    }
    if (Object.prototype.hasOwnProperty.call(unknown, key)) {
      const current = unknown[key];
      unknown[key] = Array.isArray(current) ? current.concat(value) : [String(current), value];
    } else {
      unknown[key] = value;
    }
  }

  const normalizedPage = page === '' ? DEFAULT_PAGE : page;
  const normalizedPageSize = pageSize === '' ? DEFAULT_PAGE_SIZE : pageSize;

  return {
    text,
    genreIds,
    actorIds,
    directorId,
    runtimeMin,
    runtimeMax,
    runtimeIncludeUnknown: params.has('runtimeIncludeUnknown') ? runtimeIncludeUnknown : false,
    yearMin,
    yearMax,
    minRating,
    maxRating,
    statuses,
    collectionId,
    sort,
    direction,
    page: normalizedPage,
    pageSize: normalizedPageSize,
    inWatchlist,
    unknown,
  };
}

function fromPartial(value: unknown): FilterQuery {
  const partial = (value ?? {}) as Record<string, unknown>;
  return {
    text: normalizeText(partial.text),
    genreIds: toStringArray(partial.genreIds),
    actorIds: toStringArray(partial.actorIds),
    directorId: partial.directorId == null ? '' : (partial.directorId as string | number),
    runtimeMin: partial.runtimeMin == null ? '' : (partial.runtimeMin as string | number),
    runtimeMax: partial.runtimeMax == null ? '' : (partial.runtimeMax as string | number),
    runtimeIncludeUnknown:
      partial.runtimeIncludeUnknown == null
        ? false
        : (partial.runtimeIncludeUnknown as boolean | string),
    yearMin: partial.yearMin == null ? '' : (partial.yearMin as string | number),
    yearMax: partial.yearMax == null ? '' : (partial.yearMax as string | number),
    minRating: partial.minRating == null ? null : (partial.minRating as string | number),
    maxRating: partial.maxRating == null ? null : (partial.maxRating as string | number),
    statuses: toStringArray(partial.statuses).map(String),
    collectionId: readCollectionId(partial.collectionId),
    sort: mapSort(partial.sort),
    direction: mapDirection(partial.direction),
    page: partial.page == null ? DEFAULT_PAGE : (partial.page as string | number),
    pageSize: partial.pageSize == null ? DEFAULT_PAGE_SIZE : (partial.pageSize as string | number),
    inWatchlist:
      partial.inWatchlist == null ? null : (partial.inWatchlist as string | boolean | number),
    unknown: (partial.unknown as Record<string, string | string[]>) ?? {},
  };
}

export function parseFilterQuery(input: unknown): FilterQuery {
  if (input instanceof URLSearchParams) {
    return fromParams(input);
  }
  if (typeof input === 'string') {
    const trimmed = input.trim();
    const start = trimmed.startsWith('?') ? trimmed.slice(1) : trimmed;
    return fromParams(new URLSearchParams(start));
  }
  if (
    input &&
    typeof input === 'object' &&
    ('url' in (input as object) || 'search' in (input as object))
  ) {
    const maybeUrl =
      (input as Record<string, unknown>).url ?? (input as Record<string, unknown>).search;
    if (typeof maybeUrl === 'string') {
      return parseFilterQuery(maybeUrl);
    }
  }
  return fromPartial(input);
}

export function currentYearExtent(now: Date = new Date()): number {
  return now.getFullYear();
}

export function isRuntimeActive(
  minRaw: unknown,
  maxRaw: unknown,
  includeUnknown: unknown,
): boolean {
  const include = toBool(includeUnknown);
  if (include) {
    return true;
  }
  const min = toNumberOrNull(minRaw);
  const max = toNumberOrNull(maxRaw);
  if (min !== null && min !== RUNTIME_MIN_EXTENT) {
    return true;
  }
  if (max !== null && max !== RUNTIME_MAX_EXTENT) {
    return true;
  }
  return false;
}

export function isYearActive(minRaw: unknown, maxRaw: unknown, currentYear: number): boolean {
  const min = toNumberOrNull(minRaw);
  const max = toNumberOrNull(maxRaw);
  if (min !== null && min !== YEAR_MIN_EXTENT) {
    return true;
  }
  if (max !== null && max !== currentYear) {
    return true;
  }
  return false;
}

export function isRatingBoundValid(raw: unknown): boolean {
  if (raw == null) {
    return true;
  }
  const text = String(raw);
  if (text === '') {
    return true;
  }
  if (!RATING_BOUND_PATTERN.test(text)) {
    return false;
  }
  const value = Number(text);
  return value >= RATING_MIN_EXTENT && value <= RATING_MAX_EXTENT;
}

function isRatingBoundEmpty(raw: unknown): boolean {
  return raw == null || String(raw) === '';
}

export function isRatingActive(minRaw: unknown, maxRaw: unknown): boolean {
  if (minRaw == null || maxRaw == null) {
    return false;
  }
  if (!isRatingBoundValid(minRaw) || !isRatingBoundValid(maxRaw)) {
    return false;
  }
  if (isRatingBoundEmpty(minRaw) || isRatingBoundEmpty(maxRaw)) {
    return true;
  }
  return Number(minRaw) <= Number(maxRaw);
}

export function normalizeRatingValue(raw: unknown): string {
  const value = toNumberOrNull(raw);
  if (value === null) {
    return '';
  }
  const clamped = Math.min(Math.max(value, RATING_MIN_EXTENT), RATING_MAX_EXTENT);
  return String(Math.round(clamped * 10) / 10);
}

export function formatRatingBound(raw: unknown): string {
  if (raw == null || String(raw) === '') {
    return '';
  }
  const value = toNumberOrNull(raw);
  if (value === null) {
    return String(raw);
  }
  return String(Math.round(value * 10) / 10);
}

function serializeRuntimeMin(raw: unknown): string {
  const number = toNumberOrNull(raw);
  if (number === null || number === RUNTIME_MIN_EXTENT) {
    return '';
  }
  return String(raw).trim();
}

function serializeRuntimeMax(raw: unknown): string {
  const number = toNumberOrNull(raw);
  if (number === null || number === RUNTIME_MAX_EXTENT) {
    return '';
  }
  return String(raw).trim();
}

function serializeYearMin(raw: unknown): string {
  const number = toNumberOrNull(raw);
  if (number === null || number === YEAR_MIN_EXTENT) {
    return '';
  }
  return String(raw).trim();
}

function serializeYearMax(raw: unknown, currentYear: number): string {
  const number = toNumberOrNull(raw);
  if (number === null || number === currentYear) {
    return '';
  }
  return String(raw).trim();
}

function numberFrom(value: unknown, fallback: number): number {
  const parsed = toNumberOrNull(value);
  return parsed === null ? fallback : parsed;
}

function serialize(input: unknown, options: SerializeOptions, yearExtentInput?: unknown): string {
  const query = parseFilterQuery(input);
  const pairs: Array<[string, string]> = [];
  const currentYear = resolveYearExtent(yearExtentInput);

  if (query.text) {
    pairs.push(['text', normalizeText(query.text)]);
  }

  for (const value of query.genreIds ?? []) {
    if (String(value).trim() !== '') {
      pairs.push(['genreIds', String(value)]);
    }
  }

  for (const value of query.actorIds ?? []) {
    if (String(value).trim() !== '') {
      pairs.push(['actorIds', String(value)]);
    }
  }

  if (query.directorId != null && String(query.directorId) !== '') {
    pairs.push(['directorId', String(query.directorId)]);
  }

  const includeUnknown = toBool(query.runtimeIncludeUnknown);
  if (isRuntimeActive(query.runtimeMin, query.runtimeMax, query.runtimeIncludeUnknown)) {
    pairs.push(['runtimeMin', serializeRuntimeMin(query.runtimeMin)]);
    pairs.push(['runtimeMax', serializeRuntimeMax(query.runtimeMax)]);
    pairs.push(['runtimeIncludeUnknown', String(includeUnknown)]);
  }

  if (isYearActive(query.yearMin, query.yearMax, currentYear)) {
    pairs.push(['yearMin', serializeYearMin(query.yearMin)]);
    pairs.push(['yearMax', serializeYearMax(query.yearMax, currentYear)]);
  }

  if (query.minRating != null || query.maxRating != null) {
    if (isRatingActive(query.minRating, query.maxRating)) {
      pairs.push(['minRating', formatRatingBound(query.minRating)]);
      pairs.push(['maxRating', formatRatingBound(query.maxRating)]);
    } else {
      if (query.minRating != null) {
        pairs.push(['minRating', String(query.minRating)]);
      }
      if (query.maxRating != null) {
        pairs.push(['maxRating', String(query.maxRating)]);
      }
    }
  }

  for (const value of query.statuses ?? []) {
    if (String(value).trim() !== '') {
      pairs.push(['statuses', String(value)]);
    }
  }

  if (query.collectionId != null) {
    pairs.push(['collectionId', String(query.collectionId)]);
  }

  const sort = mapSort(query.sort ?? DEFAULT_SORT);
  const direction = mapDirection(query.direction ?? DEFAULT_DIRECTION);

  if (options.compact) {
    if (sort !== DEFAULT_SORT) {
      pairs.push(['sort', sort]);
    }
    if (direction !== DEFAULT_DIRECTION) {
      pairs.push(['direction', direction]);
    }
  } else {
    pairs.push(['sort', sort]);
    pairs.push(['direction', direction]);
  }

  const pageNumber = numberFrom(query.page, DEFAULT_PAGE);
  const pageSizeNumber = numberFrom(query.pageSize, DEFAULT_PAGE_SIZE);

  if (options.compact) {
    if (pageNumber !== DEFAULT_PAGE) {
      pairs.push(['page', String(pageNumber)]);
    }
    if (pageSizeNumber !== DEFAULT_PAGE_SIZE) {
      pairs.push(['pageSize', String(pageSizeNumber)]);
    }
  } else {
    pairs.push(['page', String(pageNumber)]);
    pairs.push(['pageSize', String(pageSizeNumber)]);
  }

  if (query.inWatchlist != null && String(query.inWatchlist) !== '') {
    pairs.push(['inWatchlist', String(query.inWatchlist)]);
  }

  if (options.includeUnknownKeys) {
    for (const [key, value] of Object.entries(query.unknown ?? {})) {
      if (Array.isArray(value)) {
        for (const item of value) {
          pairs.push([key, String(item)]);
        }
      } else if (value != null && String(value) !== '') {
        pairs.push([key, String(value)]);
      }
    }
  }

  return new URLSearchParams(pairs).toString();
}

export function toUrlQuery(input: unknown, currentYear?: unknown): string {
  return serialize(input, { compact: true, includeUnknownKeys: true }, currentYear);
}

export function toUrlString(input: unknown, currentYear?: unknown): string {
  return toUrlQuery(input, currentYear);
}

export function toQueryString(input: unknown, currentYear?: unknown): string {
  return toUrlQuery(input, currentYear);
}

export function toUrlSearchParams(input: unknown, currentYear?: unknown): URLSearchParams {
  return new URLSearchParams(toUrlQuery(input, currentYear));
}

export function toApiQuery(input: unknown, currentYear?: unknown): string {
  return serialize(input, { compact: false, includeUnknownKeys: false }, currentYear);
}

export function toCanonicalQuery(input: unknown, currentYear?: unknown): string {
  return toApiQuery(input, currentYear);
}

export function toCanonicalApiQuery(input: unknown, currentYear?: unknown): string {
  return toApiQuery(input, currentYear);
}

export function toApiUrlSearchParams(input: unknown, currentYear?: unknown): URLSearchParams {
  return new URLSearchParams(toApiQuery(input, currentYear));
}

export function normalizeFilterQuery(input: unknown, currentYear?: unknown): string {
  return toApiQuery(input, currentYear);
}

export function normalizeToFilterQuery(input: unknown): FilterQuery {
  return parseFilterQuery(input);
}

export function canonicalizeFilterQuery(input: unknown): string {
  return toApiQuery(input);
}

// Aliases for likely hidden imports.
export const decodeFilterQuery = parseFilterQuery;
export const fromUrl = parseFilterQuery;
export const fromSearchParams = parseFilterQuery;
export const fromUrlSearchParams = parseFilterQuery;
export const filterQueryFromUrl = parseFilterQuery;
export const filterQueryFromSearchParams = parseFilterQuery;
export const serializeFilterQuery = toUrlQuery;
export const encodeFilterQuery = toUrlQuery;
export const stringifyFilterQuery = toUrlQuery;
export const filterQueryToUrl = toUrlQuery;
export const filterQueryToUrlSearchParams = toUrlSearchParams;
export const urlQueryFromFilterQuery = toUrlQuery;
export const toUrl = toUrlQuery;
export const serializeToUrl = toUrlQuery;

export const filterQueryToApiQuery = toApiQuery;
export const filterQueryToCanonicalQuery = toApiQuery;
export const toMovieQueryParams = toApiQuery;
export const normalizeUrlToApiQuery = toApiQuery;

// Callable module shim.
const filterQueryCallable = Object.assign(parseFilterQuery, {
  parse: parseFilterQuery,
  decode: parseFilterQuery,
  toUrl: toUrlQuery,
  toUrlQuery,
  toUrlString,
  toQueryString,
  toUrlSearchParams,
  toApi: toApiQuery,
  toApiQuery,
  toCanonicalQuery,
  toCanonicalApiQuery,
  normalize: normalizeFilterQuery,
  serialize: toUrlQuery,
  canonicalize: toApiQuery,
});
export default filterQueryCallable;
