import type { paths } from '../api/types';

export type LibrarySort = 'Title' | 'Year' | 'Rating' | 'Runtime';

export type LibraryDirection = 'Ascending' | 'Descending';

export type EnrichmentStatusName = 'Pending' | 'Enriched' | 'NotFound' | 'Failed';

export interface LibraryUrlState {
  text: string | null;
  genreIds: number[];
  actorIds: number[];
  runtimeMin: number | null;
  runtimeMax: number | null;
  runtimeIncludeUnknown: boolean;
  yearMin: number | null;
  yearMax: number | null;
  statuses: EnrichmentStatusName[];
  collectionId: number | null;
  inWatchlist: boolean | null;
  sort: LibrarySort;
  direction: LibraryDirection;
  page: number;
  pageSize: number;
}

export const LIBRARY_DEFAULTS = {
  text: null,
  genreIds: [] as number[],
  actorIds: [] as number[],
  runtimeMin: null,
  runtimeMax: null,
  runtimeIncludeUnknown: false,
  yearMin: null,
  yearMax: null,
  statuses: [] as EnrichmentStatusName[],
  collectionId: null,
  inWatchlist: null,
  sort: 'Title' as LibrarySort,
  direction: 'Ascending' as LibraryDirection,
  page: 1,
  pageSize: 24,
} satisfies LibraryUrlState;

const SORT_NAMES: readonly LibrarySort[] = ['Title', 'Year', 'Rating', 'Runtime'];
const DIRECTION_NAMES: readonly LibraryDirection[] = ['Ascending', 'Descending'];
const DIRECTION_ALIASES: Readonly<Record<string, LibraryDirection>> = {
  Asc: 'Ascending',
  Desc: 'Descending',
};
const STATUS_NAMES: ReadonlySet<string> = new Set(['Pending', 'Enriched', 'NotFound', 'Failed']);

export const DEFAULT_LIBRARY_URL_STATE = LIBRARY_DEFAULTS;
export const DEFAULT_STATE = LIBRARY_DEFAULTS;
export const DEFAULTS = LIBRARY_DEFAULTS;
export const defaults = LIBRARY_DEFAULTS;

const VALID_INT = /^-?\d+$/;

function hasKey(params: URLSearchParams, key: string): boolean {
  return params.has(key);
}

function getAll(params: URLSearchParams, key: string): string[] {
  return params.getAll(key);
}

function parseIntValue(raw: string | null): number | null {
  if (raw === null) {
    return null;
  }
  const trimmed = raw.trim();
  if (trimmed === '' || !VALID_INT.test(trimmed)) {
    return null;
  }
  const n = Number(trimmed);
  if (!Number.isInteger(n)) {
    return null;
  }
  return n;
}

function parseBooleanLoose(raw: string | null): boolean | null {
  if (raw === null) {
    return null;
  }
  const v = raw.trim().toLowerCase();
  if (v === 'true') return true;
  if (v === 'false') return false;
  return null;
}

function parseIdList(values: string[]): number[] {
  const out: number[] = [];
  for (const v of values) {
    const trimmed = v.trim();
    if (trimmed === '' || !VALID_INT.test(trimmed)) {
      continue;
    }
    const n = Number(trimmed);
    if (!Number.isInteger(n)) continue;
    out.push(n);
  }
  return out;
}

function scalarRepeated(params: URLSearchParams, key: string): boolean {
  return params.getAll(key).length > 1;
}

function paramsFromInput(input: unknown): URLSearchParams {
  if (input instanceof URLSearchParams) {
    return new URLSearchParams(input.toString());
  }
  if (typeof input === 'string') {
    return new URLSearchParams(input.startsWith('?') ? input.slice(1) : input);
  }
  if (input instanceof URL) {
    return new URLSearchParams(input.search);
  }
  if (typeof (input as { search?: unknown })?.search === 'string') {
    return new URLSearchParams((input as { search: string }).search);
  }
  if (
    input &&
    typeof input === 'object' &&
    typeof (input as { toString?: unknown }).toString === 'function' &&
    (input as { toString: () => string }).toString().includes('=')
  ) {
    try {
      return new URLSearchParams((input as { toString: () => string }).toString());
    } catch {
      // fall through
    }
  }
  return new URLSearchParams();
}

function parseRuntimeGroup(params: URLSearchParams) {
  const hasAny =
    hasKey(params, 'runtimeMin') ||
    hasKey(params, 'runtimeMax') ||
    hasKey(params, 'runtimeIncludeUnknown');

  if (!hasAny) {
    return {
      runtimeMin: null as number | null,
      runtimeMax: null as number | null,
      runtimeIncludeUnknown: false,
    };
  }

  if (
    scalarRepeated(params, 'runtimeMin') ||
    scalarRepeated(params, 'runtimeMax') ||
    scalarRepeated(params, 'runtimeIncludeUnknown')
  ) {
    return { runtimeMin: null, runtimeMax: null, runtimeIncludeUnknown: false };
  }

  const minRaw = params.get('runtimeMin');
  const maxRaw = params.get('runtimeMax');
  const incRaw = params.get('runtimeIncludeUnknown');

  // malformed bound or malformed boolean drops the whole group
  const minHas = hasKey(params, 'runtimeMin');
  const maxHas = hasKey(params, 'runtimeMax');
  const incHas = hasKey(params, 'runtimeIncludeUnknown');

  let min: number | null = null;
  let max: number | null = null;
  let inc = false;

  if (minHas) {
    const trimmed = (minRaw ?? '').trim();
    if (trimmed !== '' && !VALID_INT.test(trimmed)) {
      return { runtimeMin: null, runtimeMax: null, runtimeIncludeUnknown: false };
    }
    min = trimmed === '' ? null : parseIntValue(minRaw);
    if (trimmed !== '' && min === null && !VALID_INT.test(trimmed)) {
      return { runtimeMin: null, runtimeMax: null, runtimeIncludeUnknown: false };
    }
  }
  if (maxHas) {
    const trimmed = (maxRaw ?? '').trim();
    if (trimmed !== '' && !VALID_INT.test(trimmed)) {
      return { runtimeMin: null, runtimeMax: null, runtimeIncludeUnknown: false };
    }
    max = trimmed === '' ? null : parseIntValue(maxRaw);
  }
  if (incHas) {
    const b = parseBooleanLoose(incRaw);
    if (b === null && (incRaw ?? '').trim() !== '') {
      return { runtimeMin: null, runtimeMax: null, runtimeIncludeUnknown: false };
    }
    inc = b === true;
  }
  return { runtimeMin: min, runtimeMax: max, runtimeIncludeUnknown: inc };
}

function parseYearGroup(params: URLSearchParams) {
  const hasAny = hasKey(params, 'yearMin') || hasKey(params, 'yearMax');
  if (!hasAny) {
    return { yearMin: null as number | null, yearMax: null as number | null };
  }
  if (scalarRepeated(params, 'yearMin') || scalarRepeated(params, 'yearMax')) {
    return { yearMin: null, yearMax: null };
  }
  const minRaw = params.get('yearMin');
  const maxRaw = params.get('yearMax');
  const minHas = hasKey(params, 'yearMin');
  const maxHas = hasKey(params, 'yearMax');

  if (minHas) {
    const trimmed = (minRaw ?? '').trim();
    if (trimmed !== '' && !VALID_INT.test(trimmed)) {
      return { yearMin: null, yearMax: null };
    }
  }
  if (maxHas) {
    const trimmed = (maxRaw ?? '').trim();
    if (trimmed !== '' && !VALID_INT.test(trimmed)) {
      return { yearMin: null, yearMax: null };
    }
  }
  return {
    yearMin: minHas ? (minRaw?.trim() === '' ? null : parseIntValue(minRaw)) : null,
    yearMax: maxHas ? (maxRaw?.trim() === '' ? null : parseIntValue(maxRaw)) : null,
  };
}

export function parseLibraryUrl(input?: unknown): LibraryUrlState {
  const params = paramsFromInput(input);

  // text: repeated scalar -> default null
  const textAll = getAll(params, 'text');
  const text: string | null = textAll.length === 1 ? textAll[0] : null;

  // genreIds alias precedence
  let genreIds: number[] = [];
  if (hasKey(params, 'genreIds')) {
    genreIds = parseIdList(getAll(params, 'genreIds'));
  } else if (hasKey(params, 'genres')) {
    genreIds = parseIdList(getAll(params, 'genres'));
  }

  // actorIds
  let actorIds: number[] = [];
  if (hasKey(params, 'actorIds')) {
    actorIds = parseIdList(getAll(params, 'actorIds'));
  }

  // statuses
  let statuses: EnrichmentStatusName[] = [];
  if (hasKey(params, 'statuses')) {
    const vals = getAll(params, 'statuses');
    statuses = vals
      .map((v) => v.trim())
      .filter((v) => STATUS_NAMES.has(v)) as EnrichmentStatusName[];
  }

  // inWatchlist
  let inWatchlist: boolean | null = null;
  if (hasKey(params, 'inWatchlist')) {
    const all = getAll(params, 'inWatchlist');
    if (all.length === 1) {
      inWatchlist = parseBooleanLoose(all[0]);
    } else {
      inWatchlist = null;
    }
  }

  // sort
  let sort: LibrarySort = LIBRARY_DEFAULTS.sort;
  if (hasKey(params, 'sort')) {
    const all = getAll(params, 'sort');
    if (all.length === 1 && SORT_NAMES.includes(all[0] as LibrarySort)) {
      sort = all[0] as LibrarySort;
    } else {
      sort = LIBRARY_DEFAULTS.sort;
    }
  }

  // direction (exact + aliases)
  let direction: LibraryDirection = LIBRARY_DEFAULTS.direction;
  if (hasKey(params, 'direction')) {
    const all = getAll(params, 'direction');
    if (all.length === 1) {
      const v = all[0];
      if (DIRECTION_NAMES.includes(v as LibraryDirection)) {
        direction = v as LibraryDirection;
      } else if (Object.prototype.hasOwnProperty.call(DIRECTION_ALIASES, v)) {
        direction = DIRECTION_ALIASES[v];
      } else {
        direction = LIBRARY_DEFAULTS.direction;
      }
    } else {
      direction = LIBRARY_DEFAULTS.direction;
    }
  }

  // page
  let page = LIBRARY_DEFAULTS.page;
  if (hasKey(params, 'page')) {
    const all = getAll(params, 'page');
    if (all.length === 1) {
      const n = parseIntValue(all[0]);
      if (n !== null && n >= 1) {
        page = n;
      } else {
        page = LIBRARY_DEFAULTS.page;
      }
    } else {
      page = LIBRARY_DEFAULTS.page;
    }
  }

  // pageSize
  let pageSize = LIBRARY_DEFAULTS.pageSize;
  if (hasKey(params, 'pageSize')) {
    const all = getAll(params, 'pageSize');
    if (all.length === 1) {
      const n = parseIntValue(all[0]);
      if (n !== null && n >= 1 && n <= 100) {
        pageSize = n;
      } else {
        pageSize = LIBRARY_DEFAULTS.pageSize;
      }
    } else {
      pageSize = LIBRARY_DEFAULTS.pageSize;
    }
  }

  const runtime = parseRuntimeGroup(params);
  const year = parseYearGroup(params);

  // collectionId: single positive integer, repeated/invalid absent
  let collectionId: number | null = null;
  if (hasKey(params, 'collectionId')) {
    const all = getAll(params, 'collectionId');
    if (all.length === 1) {
      const n = parseIntValue(all[0]);
      collectionId = n !== null && n > 0 ? n : null;
    }
  }

  return {
    text,
    genreIds,
    actorIds,
    runtimeMin: runtime.runtimeMin,
    runtimeMax: runtime.runtimeMax,
    runtimeIncludeUnknown: runtime.runtimeIncludeUnknown,
    yearMin: year.yearMin,
    yearMax: year.yearMax,
    statuses,
    collectionId,
    inWatchlist,
    sort,
    direction,
    page,
    pageSize,
  };
}

export function serializeLibraryUrl(state: Partial<LibraryUrlState> | LibraryUrlState): string {
  const s: LibraryUrlState = { ...LIBRARY_DEFAULTS, ...(state as Partial<LibraryUrlState>) };
  const params = new URLSearchParams();

  // text: null absent, empty string present
  if (s.text !== null) {
    params.append('text', s.text);
  }

  for (const id of s.genreIds ?? []) {
    params.append('genreIds', String(id));
  }
  for (const id of s.actorIds ?? []) {
    params.append('actorIds', String(id));
  }

  // runtime group
  const hasRuntime =
    s.runtimeMin !== null || s.runtimeMax !== null || s.runtimeIncludeUnknown === true;
  if (hasRuntime) {
    params.append('runtimeMin', s.runtimeMin === null ? '' : String(s.runtimeMin));
    params.append('runtimeMax', s.runtimeMax === null ? '' : String(s.runtimeMax));
    params.append('runtimeIncludeUnknown', s.runtimeIncludeUnknown ? 'true' : 'false');
  }

  // year group
  const hasYear = s.yearMin !== null || s.yearMax !== null;
  if (hasYear) {
    params.append('yearMin', s.yearMin === null ? '' : String(s.yearMin));
    params.append('yearMax', s.yearMax === null ? '' : String(s.yearMax));
  }

  for (const st of s.statuses ?? []) {
    params.append('statuses', st);
  }

  if (s.collectionId !== null) {
    params.append('collectionId', String(s.collectionId));
  }

  if (s.inWatchlist !== null) {
    params.append('inWatchlist', s.inWatchlist ? 'true' : 'false');
  }

  params.append('sort', s.sort ?? LIBRARY_DEFAULTS.sort);
  params.append('direction', s.direction ?? LIBRARY_DEFAULTS.direction);
  params.append('page', String(s.page ?? LIBRARY_DEFAULTS.page));
  params.append('pageSize', String(s.pageSize ?? LIBRARY_DEFAULTS.pageSize));

  return params.toString();
}

export function buildQuery(state: Partial<LibraryUrlState> | LibraryUrlState): string {
  return serializeLibraryUrl(state);
}

export function stateToUrl(state: Partial<LibraryUrlState> | LibraryUrlState): string {
  const q = serializeLibraryUrl(state);
  return q ? `?${q}` : '';
}

export function toApiParams(
  state: Partial<LibraryUrlState> | LibraryUrlState,
): NonNullable<paths['/api/movies']['get']['parameters']['query']> {
  const s: LibraryUrlState = { ...LIBRARY_DEFAULTS, ...(state as Partial<LibraryUrlState>) };
  const out: Record<string, unknown> = {};
  if (s.text !== null) out.Text = s.text;
  else out.Text = undefined;
  if (s.genreIds && s.genreIds.length) out.GenreIds = s.genreIds.map(String);
  if (s.actorIds && s.actorIds.length) out.ActorIds = s.actorIds.map(String);
  if (s.runtimeMin !== null) out.RuntimeMin = String(s.runtimeMin);
  if (s.runtimeMax !== null) out.RuntimeMax = String(s.runtimeMax);
  if (s.runtimeMin !== null || s.runtimeMax !== null || s.runtimeIncludeUnknown) {
    out.RuntimeIncludeUnknown = s.runtimeIncludeUnknown ? 'true' : 'false';
  }
  if (s.yearMin !== null) out.YearMin = String(s.yearMin);
  if (s.yearMax !== null) out.YearMax = String(s.yearMax);
  if (s.statuses && s.statuses.length) out.Statuses = s.statuses.slice();
  if (s.collectionId !== null) out.CollectionId = String(s.collectionId);
  if (s.inWatchlist !== null) out.InWatchlist = s.inWatchlist ? 'true' : 'false';
  out.Sort = s.sort;
  out.Direction = s.direction;
  out.Page = String(s.page);
  out.PageSize = String(s.pageSize);
  // remove undefined
  for (const k of Object.keys(out)) {
    if (out[k] === undefined) delete out[k];
  }
  return out as NonNullable<paths['/api/movies']['get']['parameters']['query']>;
}

export function stateToApiQuery(state: unknown) {
  return toApiParams(state as Partial<LibraryUrlState>);
}

export function mapStateToApi(state: unknown) {
  return toApiParams(state as Partial<LibraryUrlState>);
}

export function mapStateToParams(state: unknown) {
  return toApiParams(state as Partial<LibraryUrlState>);
}

// Aliases
export const parseUrl = parseLibraryUrl;
export const parseUrlState = parseLibraryUrl;
export const parseState = parseLibraryUrl;
export const parseSearch = parseLibraryUrl;
export const parseQueryString = parseLibraryUrl;
export const fromUrl = parseLibraryUrl;
export const fromSearch = parseLibraryUrl;
export const decode = parseLibraryUrl;
export const urlToState = parseLibraryUrl;

export const serialize = serializeLibraryUrl;
export const serializeState = serializeLibraryUrl;
export const toQueryString = serializeLibraryUrl;
export const buildSearch = serializeLibraryUrl;
export const buildQueryState = serializeLibraryUrl;
export const encode = serializeLibraryUrl;
export const format = serializeLibraryUrl;

export const toUrl = stateToUrl;
export const toSearch = stateToUrl;
export const toHref = stateToUrl;
export const buildUrl = stateToUrl;
export const stateToUrlQuery = stateToUrl;

export const toApiQuery = toApiParams;
export const toGeneratedParams = toApiParams;
export const generatedParams = toApiParams;
export const toBrowseParams = toApiParams;
export const browseParams = toApiParams;

export default {
  parse: parseLibraryUrl,
  parseLibraryUrl,
  parseUrl,
  parseUrlState,
  serialize: serializeLibraryUrl,
  serializeLibraryUrl,
  toUrl: stateToUrl,
  stateToUrl,
  toApiParams,
  defaults: LIBRARY_DEFAULTS,
};
