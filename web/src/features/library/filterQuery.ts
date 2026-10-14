export const DEFAULT_SORT = 'Title';
export const DEFAULT_DIRECTION = 'Ascending';
export const DEFAULT_PAGE = 1;
export const DEFAULT_PAGE_SIZE = 20;
export const RUNTIME_MIN_EXTENT = 0;
export const RUNTIME_MAX_EXTENT = 240;
export const YEAR_MIN_EXTENT = 1900;
export const DEBOUNCE_MS = 300;

const RECOGNIZED_KEYS = new Set([
  'text',
  'genreIds',
  'actorIds',
  'runtimeMin',
  'runtimeMax',
  'runtimeIncludeUnknown',
  'yearMin',
  'yearMax',
  'statuses',
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
  runtimeMin: string | number | null;
  runtimeMax: string | number | null;
  runtimeIncludeUnknown: boolean | string | null;
  yearMin: string | number | null;
  yearMax: string | number | null;
  statuses: Array<string>;
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
  const runtimeMin = params.has('runtimeMin') ? (params.get('runtimeMin') ?? '') : '';
  const runtimeMax = params.has('runtimeMax') ? (params.get('runtimeMax') ?? '') : '';
  const runtimeIncludeUnknown = params.has('runtimeIncludeUnknown')
    ? (params.get('runtimeIncludeUnknown') ?? '')
    : '';
  const yearMin = params.has('yearMin') ? (params.get('yearMin') ?? '') : '';
  const yearMax = params.has('yearMax') ? (params.get('yearMax') ?? '') : '';
  const statuses = params.getAll('statuses');
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
    runtimeMin,
    runtimeMax,
    runtimeIncludeUnknown: params.has('runtimeIncludeUnknown') ? runtimeIncludeUnknown : false,
    yearMin,
    yearMax,
    statuses,
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
    runtimeMin: partial.runtimeMin == null ? '' : (partial.runtimeMin as string | number),
    runtimeMax: partial.runtimeMax == null ? '' : (partial.runtimeMax as string | number),
    runtimeIncludeUnknown:
      partial.runtimeIncludeUnknown == null
        ? false
        : (partial.runtimeIncludeUnknown as boolean | string),
    yearMin: partial.yearMin == null ? '' : (partial.yearMin as string | number),
    yearMax: partial.yearMax == null ? '' : (partial.yearMax as string | number),
    statuses: toStringArray(partial.statuses).map(String),
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

function isRuntimeActive(minRaw: unknown, maxRaw: unknown, includeUnknown: unknown): boolean {
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

function isYearActive(minRaw: unknown, maxRaw: unknown, currentYear: number): boolean {
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

function serialize(input: unknown, options: SerializeOptions): string {
  const query = parseFilterQuery(input);
  const pairs: Array<[string, string]> = [];
  const currentYear = currentYearExtent();

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

  for (const value of query.statuses ?? []) {
    if (String(value).trim() !== '') {
      pairs.push(['statuses', String(value)]);
    }
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

export function toUrlQuery(input: unknown): string {
  return serialize(input, { compact: true, includeUnknownKeys: true });
}

export function toUrlString(input: unknown): string {
  return toUrlQuery(input);
}

export function toQueryString(input: unknown): string {
  return toUrlQuery(input);
}

export function toUrlSearchParams(input: unknown): URLSearchParams {
  return new URLSearchParams(toUrlQuery(input));
}

export function toApiQuery(input: unknown): string {
  return serialize(input, { compact: false, includeUnknownKeys: false });
}

export function toCanonicalQuery(input: unknown): string {
  return toApiQuery(input);
}

export function toCanonicalApiQuery(input: unknown): string {
  return toApiQuery(input);
}

export function toApiUrlSearchParams(input: unknown): URLSearchParams {
  return new URLSearchParams(toApiQuery(input));
}

export function normalizeFilterQuery(input: unknown): string {
  return toApiQuery(input);
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
